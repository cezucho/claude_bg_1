using System.Text.Json;

namespace Augury.Sim.Content;

/// <summary>
/// Loads champion JSON into immutable definitions and enforces the schema's load-time
/// invariants (Champion &amp; Ability Schema rule 9). A breach is a hard failure.
/// </summary>
public static class ContentLoader
{
    /// <summary>Stat permille bought per point of initiative below 10 (schema F4, ⚠ C).</summary>
    public const int StatTradeRate = 150;

    /// <summary>Loads every <c>*.json</c> champion file in a directory, sorted by file name.</summary>
    public static ContentDb LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new ContentException($"Content directory not found: {directory}");
        }

        var champions = Directory.GetFiles(directory, "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => ParseChampion(File.ReadAllText(f), Path.GetFileName(f)))
            .ToList();
        string spells = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar))!, "spells.json");
        return new ContentDb(champions, LoadSpells(spells));
    }

    /// <summary>
    /// Finds <c>assets/data</c> (the directory holding <c>rules_config.json</c>) by walking up
    /// from the working directory and the executable's directory.
    /// </summary>
    public static string FindDataDirectory(string? start = null)
    {
        string[] roots = start is not null
            ? [start]
            : [Directory.GetCurrentDirectory(), AppContext.BaseDirectory];
        foreach (string root in roots)
        {
            for (var dir = new DirectoryInfo(root); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "assets", "data");
                if (File.Exists(Path.Combine(candidate, "rules_config.json"))) return candidate;
            }
        }

        throw new ContentException("Could not locate assets/data/rules_config.json above " + string.Join(" or ", roots));
    }

    /// <summary>The shipped roster's directory, <c>assets/data/champions</c>.</summary>
    public static string FindChampionDirectory(string? start = null) =>
        Path.Combine(FindDataDirectory(start), "champions");

    /// <summary>Parses and validates one champion document.</summary>
    public static ChampionDef ParseChampion(string json, string source = "<inline>")
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        string where = source;

        try
        {
            string id = Str(root, "id");
            where = $"{source} ({id})";
            Role role = Enum<Role>(Str(root, "role"));

            JsonElement stats = root.GetProperty("stats");
            int[] baseStats =
            [
                Int(stats, "vit"), Int(stats, "pow"), Int(stats, "arm"), Int(stats, "rch"), Int(stats, "spd")
            ];

            var abilities = root.GetProperty("abilities").EnumerateArray()
                .Select(ParseAbility).ToList();
            bool v2 = root.TryGetProperty("signature", out JsonElement sig);
            if (abilities.Count != (v2 ? 3 : 4))
            {
                throw new ContentException(v2
                    ? $"{where}: a champion with a signature opening has exactly 3 abilities (the 4th slot is its summoner spell), found {abilities.Count}."
                    : $"{where}: exactly 4 active abilities required, found {abilities.Count}.");
            }

            SignatureOpening? signature = null;
            if (v2)
            {
                ValidateKitV2(abilities, baseStats, where);
                var ops = sig.GetProperty("opening").EnumerateArray().Select(i => ParseInstruction(i.GetString()!)).ToList();
                if (ops.Count != 3) throw new ContentException($"{where}: the signature opening needs exactly 3 instructions.");
                signature = new SignatureOpening(Str(sig, "name"), ops);
                ValidateCasts(abilities, signature, where);
            }
            else
            {
                ValidateKit(abilities, baseStats, where);
                int sum = abilities.Sum(a => a.Initiative);
                Stat trade = Enum<Stat>(Str(root, "tradeStat"));
                baseStats[(int)trade] += (10 - sum) * StatTradeRate;
            }

            if (baseStats[(int)Stat.Rch] > 3000)
            {
                throw new ContentException($"{where}: base RCH above 3000 — reach caps at 3 hexes.");
            }

            JsonElement p = root.GetProperty("passive");
            var passive = new PassiveDef(
                Str(p, "name"),
                Enum<PassiveTrigger>(Str(p, "trigger")),
                Enum<PassiveEffect>(Str(p, "effect")),
                Int(p, "amount"),
                p.TryGetProperty("stat", out JsonElement ps) ? Enum<Stat>(ps.GetString()!) : Stat.Pow);

            string glyph = Str(root, "glyph");
            return new ChampionDef
            {
                Id = id,
                Name = Str(root, "name"),
                Role = role,
                Glyph = glyph.Length == 1 ? glyph[0] : throw new ContentException($"{where}: glyph must be one character."),
                BaseStats = baseStats,
                Abilities = abilities,
                Passive = passive,
                Signature = signature,
                Line = root.TryGetProperty("line", out JsonElement line) ? line.GetString() ?? "" : "",
            };
        }
        catch (ContentException)
        {
            throw;
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ContentException($"{where}: {e.Message}");
        }
    }

    private static AbilityDef ParseAbility(JsonElement a)
    {
        var pattern = a.TryGetProperty("pattern", out JsonElement pe)
            ? pe.EnumerateArray().Select(c => new HexCoord(c[0].GetInt32(), c[1].GetInt32())).ToList()
            : [];

        var effects = a.GetProperty("effects").EnumerateArray().Select(e => new EffectDef(
            Enum<EffectKind>(Str(e, "kind")),
            e.TryGetProperty("power", out JsonElement pw) ? pw.GetInt32() : 0,
            e.TryGetProperty("amount", out JsonElement am) ? am.GetInt32() : 0,
            e.TryGetProperty("rounds", out JsonElement rd) ? rd.GetInt32() : 0,
            e.TryGetProperty("bonusVs", out JsonElement bv) ? Enum<StatusKind>(bv.GetString()!) : StatusKind.None,
            e.TryGetProperty("bonusFlat", out JsonElement bf) ? bf.GetInt32() : 0,
            e.TryGetProperty("bonusDouble", out JsonElement bd) && bd.GetBoolean())).ToList();

        bool spell = !a.TryGetProperty("moldUp", out _);
        JsonElement up = spell ? default : a.GetProperty("moldUp");
        JsonElement down = spell ? default : a.GetProperty("moldDown");

        return new AbilityDef
        {
            Name = Str(a, "name"),
            Initiative = Int(a, "initiative"),
            Cooldown = Int(a, "cooldown"),
            Target = a.TryGetProperty("target", out JsonElement t) ? Enum<TargetRule>(t.GetString()!) : TargetRule.Enemy,
            RangeBonus = a.TryGetProperty("rangeBonus", out JsonElement rb) ? rb.GetInt32() : 0,
            Pattern = pattern,
            Effects = effects,
            ScalesFrom = spell ? Stat.Pow : Enum<Stat>(Str(a, "scalesFrom")),
            MoldUp = spell ? Stat.Pow : Enum<Stat>(up[0].GetString()!),
            MoldUpDelta = spell ? 0 : up[1].GetInt32(),
            MoldDown = spell ? Stat.Arm : Enum<Stat>(down[0].GetString()!),
            MoldDownDelta = spell ? 0 : down[1].GetInt32(),
            PrintedSigil = a.TryGetProperty("printedSigil", out JsonElement ps) ? ps.GetInt32() : -1,
            SlotSigil = a.TryGetProperty("slotSigil", out JsonElement ss) ? ss.GetInt32() : -1,
            Opening = a.TryGetProperty("opening", out JsonElement op)
                ? op.EnumerateArray().Select(i => ParseInstruction(i.GetString()!)).ToList()
                : [],
            FixedRange = a.TryGetProperty("range", out JsonElement fr) ? fr.GetInt32() : 0,
            IsSpell = spell,
        };
    }

    /// <summary>
    /// Parses <c>"move &lt;role&gt; &lt;direction&gt;"</c>, <c>"beacon &lt;role&gt; &lt;sigil&gt;"</c>
    /// or (v2) <c>"cast &lt;role&gt; &lt;q|w|e&gt;"</c>.
    /// </summary>
    public static OpeningInstruction ParseInstruction(string text)
    {
        string[] parts = text.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new ContentException($"Bad opening instruction '{text}'.");
        }

        Role role = Enum<Role>(parts[1]);
        return parts[0].ToLowerInvariant() switch
        {
            "move" => new OpeningInstruction(InstructionKind.Move, role, Directions.Parse(parts[2]), -1),
            "beacon" => new OpeningInstruction(InstructionKind.PlaceBeacon, role, 0, int.Parse(parts[2])),
            "cast" => new OpeningInstruction(InstructionKind.Cast, role, 0, -1, parts[2].ToLowerInvariant() switch
            {
                "q" => 0,
                "w" => 1,
                "e" => 2,
                _ => throw new ContentException($"Bad cast slot in '{text}': use q, w or e."),
            }),
            _ => throw new ContentException($"Bad opening instruction kind in '{text}'."),
        };
    }

    private static void ValidateKit(List<AbilityDef> kit, int[] baseStats, string where)
    {
        for (int i = 0; i < kit.Count; i++)
        {
            AbilityDef a = kit[i];
            string at = $"{where} ability '{a.Name}'";
            if (a.Initiative is < 1 or > 4) throw new ContentException($"{at}: initiative must be 1–4.");
            if (a.Cooldown is < 0 or > 4) throw new ContentException($"{at}: cooldown must be 0–4.");
            if (i > 0 && a.Initiative < kit[i - 1].Initiative) throw new ContentException($"{at}: initiatives must be non-decreasing Q→R.");
            if (a.IsFree && a.Pattern.Count > 0) throw new ContentException($"{at}: initiative 1–2 abilities are free-targeting and declare no pattern.");
            if (!a.IsFree && a.Pattern.Count == 0) throw new ContentException($"{at}: initiative 3–4 abilities must declare a pattern.");
            if (a.Initiative == 4 && a.Pattern.Count is < 4 or > 6) throw new ContentException($"{at}: tier-4 patterns are 4–6 hexes.");
            if (a.Effects.Count is < 1 or > 2) throw new ContentException($"{at}: one or two effects required.");
            if (a.MoldUp == a.MoldDown) throw new ContentException($"{at}: MoldUp and MoldDown must differ.");
            if (a.MoldUp == a.ScalesFrom || a.MoldDown == a.ScalesFrom) throw new ContentException($"{at}: cross rule — never mold the stat you scale from.");
            if (a.MoldUpDelta <= 0 || a.MoldDownDelta <= 0) throw new ContentException($"{at}: mold deltas must be positive.");
            if (a.PrintedSigil is < -1 or > 2 || a.SlotSigil is < -1 or > 2) throw new ContentException($"{at}: sigils are 0–2 or −1.");
            if (a.Opening.Count != 3) throw new ContentException($"{at}: exactly 3 opening instructions required.");
            if (a.Pattern.Contains(default)) throw new ContentException($"{at}: a pattern may not include the caster's own hex.");

            bool dash = a.Effects.Any(e => e.Kind == EffectKind.Dash);
            if (dash && (a.Effects.Count != 1 || !a.IsFree)) throw new ContentException($"{at}: Dash must be the only effect of a free-targeting ability.");
            if (a.Target == TargetRule.EmptyHex && !dash) throw new ContentException($"{at}: EmptyHex targeting is only for Dash.");

            int range = Math.Clamp(baseStats[(int)Stat.Rch] / 1000 + a.RangeBonus, 1, 3);
            if (a.Initiative == 1 && range == 1 && !dash)
            {
                throw new ContentException($"{at}: tier-1 abilities are ranged (schema rule 5); range 1 needs a Dash.");
            }
        }

        int sum = kit.Sum(a => a.Initiative);
        if (sum is < 9 or > 11) throw new ContentException($"{where}: initiative total {sum} outside 9–11.");
        if (kit.GroupBy(a => a.Initiative).Any(g => g.Count() > 2)) throw new ContentException($"{where}: at most two abilities per initiative tier.");
    }

    /// <summary>
    /// v2 kit rules: three abilities, initiative non-decreasing, free abilities (1–2) with no
    /// pattern, rotatable (3) and fixed (4) with one. Up to three effects; no initiative trade.
    /// </summary>
    private static void ValidateKitV2(List<AbilityDef> kit, int[] baseStats, string where)
    {
        for (int i = 0; i < kit.Count; i++)
        {
            AbilityDef a = kit[i];
            string at = $"{where} ability '{a.Name}'";
            if (a.IsSpell) throw new ContentException($"{at}: champion abilities mold (moldUp/moldDown required).");
            if (a.Initiative is < 1 or > 4) throw new ContentException($"{at}: initiative must be 1–4.");
            if (a.Cooldown is < 0 or > 4) throw new ContentException($"{at}: cooldown must be 0–4.");
            if (i > 0 && a.Initiative < kit[i - 1].Initiative) throw new ContentException($"{at}: initiatives must be non-decreasing Q→E.");
            if ((a.Initiative <= 2) != a.IsFree) throw new ContentException($"{at}: initiative 1–2 aim freely (no pattern); 3–4 use a pattern.");
            if (a.Initiative == 4 && a.Pattern.Count is < 3 or > 6) throw new ContentException($"{at}: fixed patterns are 3–6 hexes.");
            if (a.Effects.Count is < 1 or > 3) throw new ContentException($"{at}: one to three effects.");
            if (a.MoldUp == a.MoldDown) throw new ContentException($"{at}: MoldUp and MoldDown must differ.");
            if (a.MoldUp == a.ScalesFrom || a.MoldDown == a.ScalesFrom) throw new ContentException($"{at}: cross rule — never mold the stat you scale from.");
            if (a.PrintedSigil is < -1 or > 2 || a.SlotSigil is < -1 or > 2) throw new ContentException($"{at}: sigils are 0–2 or −1.");
            if (a.Opening.Count != 3) throw new ContentException($"{at}: exactly 3 opening instructions required.");
            if (a.Pattern.Contains(default)) throw new ContentException($"{at}: a pattern may not include the caster's own hex.");
            if (a.Target is TargetRule.Structure or TargetRule.TeleportHex) throw new ContentException($"{at}: Structure and TeleportHex targeting are for summoner spells.");
            bool placesOrDashes = a.Effects.Any(e => e.Kind is EffectKind.Dash or EffectKind.Wall);
            if ((a.Target == TargetRule.EmptyHex) != placesOrDashes) throw new ContentException($"{at}: EmptyHex targeting goes with Dash or Wall, and only them.");
        }

        if (baseStats[(int)Stat.Rch] > 3000) throw new ContentException($"{where}: base RCH above 3000 — reach caps at 3 hexes.");
        if (kit.GroupBy(a => a.Initiative).Any(g => g.Count() > 2)) throw new ContentException($"{where}: at most two abilities per initiative tier.");
    }

    /// <summary>v2: exactly one ability per champion has an attack opening, casting one to three abilities.</summary>
    private static void ValidateCasts(List<AbilityDef> kit, SignatureOpening signature, string where)
    {
        int withCasts = kit.Count(a => a.Opening.Any(i => i.Kind == InstructionKind.Cast))
                        + (signature.Opening.Any(i => i.Kind == InstructionKind.Cast) ? 1 : 0);
        if (withCasts != 1) throw new ContentException($"{where}: exactly one opening casts abilities (owner, 2026-10-04); found {withCasts}.");
    }

    /// <summary>Loads the summoner spell pool, <c>spells.json</c>, if present.</summary>
    public static IReadOnlyList<AbilityDef> LoadSpells(string path)
    {
        if (!File.Exists(path)) return [];
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        var spells = new List<AbilityDef>();
        foreach (JsonElement e in doc.RootElement.GetProperty("spells").EnumerateArray())
        {
            AbilityDef s = ParseAbility(e);
            string at = $"{path} spell '{s.Name}'";
            if (!s.IsSpell) throw new ContentException($"{at}: spells don't mold (no moldUp/moldDown).");
            if (s.Opening.Count != 0) throw new ContentException($"{at}: spells never carry opening instructions (owner).");
            if (s.Pattern.Count != 0) throw new ContentException($"{at}: spells aim freely.");
            if (s.Initiative is < 1 or > 4) throw new ContentException($"{at}: initiative must be 1–4.");
            if (s.Target is not (TargetRule.Self or TargetRule.TeleportHex) && s.FixedRange is < 1 or > 4) throw new ContentException($"{at}: needs a range of 1–4.");
            spells.Add(s);
        }

        if (spells.Count > 250) throw new ContentException($"{path}: too many spells.");
        return spells;
    }

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()
        ?? throw new ContentException($"'{name}' is null.");

    private static int Int(JsonElement e, string name) => e.GetProperty(name).GetInt32();

    private static T Enum<T>(string s) where T : struct, System.Enum =>
        System.Enum.TryParse(s, ignoreCase: true, out T v)
            ? v
            : throw new ContentException($"'{s}' is not a valid {typeof(T).Name}.");
}

/// <summary>Loaded content: the roster, indexed.</summary>
public sealed class ContentDb
{
    /// <summary>Creates a database over a validated roster and spell pool.</summary>
    public ContentDb(IReadOnlyList<ChampionDef> champions, IReadOnlyList<AbilityDef>? spells = null)
    {
        if (champions.Count > 250) throw new ContentException("Roster exceeds 250 champions (byte index).");
        Champions = champions;
        Spells = spells ?? [];
    }

    /// <summary>All champions, in load order. MatchState refers to them by index.</summary>
    public IReadOnlyList<ChampionDef> Champions { get; }

    /// <summary>The summoner spell pool (v2), in file order. Champions refer to them by index.</summary>
    public IReadOnlyList<AbilityDef> Spells { get; }

    /// <summary>Index of a champion by id.</summary>
    public int IndexOf(string id)
    {
        for (int i = 0; i < Champions.Count; i++)
        {
            if (Champions[i].Id == id) return i;
        }

        throw new ContentException($"No champion '{id}'.");
    }

    /// <summary>Champions available for a role.</summary>
    public IEnumerable<int> ForRole(Role role) =>
        Enumerable.Range(0, Champions.Count).Where(i => Champions[i].Role == role);
}
