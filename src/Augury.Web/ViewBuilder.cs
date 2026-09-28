using Augury.Sim;
using Augury.Sim.Content;

namespace Augury.Web;

/// <summary>
/// Turns a match state into the JSON the page draws. Everything the page shows comes from
/// here; the page holds no rules. Previews are exact because the simulation is
/// deterministic: each legal command is applied to a copy of the state and the result diffed.
/// </summary>
public static class ViewBuilder
{
    private static readonly string[] Keys = ["Q", "W", "E", "R"];
    private const int EventWindow = 120;

    /// <summary>Builds the full view.</summary>
    public static object Build(
        Game g, MatchState s, List<Command> legal, List<GameEvent> log, string mode,
        HashSet<Team> humans, bool humanTurn, bool canUndo, string? error = null)
    {
        bool ladder = s.Phase is Phase.Ladder or Phase.LastWord;
        var singles = new HashSet<(int, int)>();
        var chainParts = new HashSet<(int, int)>();
        foreach (Command c in legal)
        {
            if (c.Kind == CommandKind.OpeningPlay || (c.Kind == CommandKind.Ability && !c.IsChain)) singles.Add((c.Champion, c.Ability));
            if (c.IsChain)
            {
                chainParts.Add((c.Champion, c.Ability));
                chainParts.Add((c.Champion2, c.Ability2));
            }
        }

        int start = Math.Max(0, log.Count - EventWindow);
        return new
        {
            error,
            mode,
            humans = humans.Select(t => t.ToString()).OrderBy(t => t).ToArray(),
            humanTurn,
            canUndo,
            phase = s.Phase.ToString(),
            round = s.Round,
            half = s.Half,
            active = s.Active.ToString(),
            halfOpener = s.Round > 0 ? s.HalfOpener.ToString() : null,
            ceiling = s.Ceiling,
            basics = new { a = (int)s.BasicsTaken[0], b = (int)s.BasicsTaken[1], per = g.Rules.BasicsPerHalf },
            score = new { a = s.Score[0], b = s.Score[1], target = g.Rules.TargetScore, killPoints = g.Rules.KillPoints, roundCap = g.Rules.RoundCap },
            nexus = new[] { Team.A, Team.B }.Select(t => new
            {
                team = t.ToString(),
                hp = s.NexusHp[t == Team.A ? 0 : 1],
                max = g.Rules.NexusHp,
                open = Game.NexusVulnerable(s, t),
                hexes = Board.NexusHexes(t).Select(Xy).ToArray(),
            }).ToArray(),
            towers = Enumerable.Range(0, 5).Select(t => new
            {
                i = t,
                q = s.Towers[t].Pos.Q,
                r = s.Towers[t].Pos.R,
                owner = s.Towers[t].Owner.ToString(),
                home = s.Towers[t].Home.ToString(),
                hp = (int)s.Towers[t].Hp,
                max = g.Rules.TowerHp,
            }).ToArray(),
            beacons = Enumerable.Range(0, 12).Where(b => s.Beacons[b].Team != Team.None).Select(b => new
            {
                i = b,
                q = s.Beacons[b].Pos.Q,
                r = s.Beacons[b].Pos.R,
                team = s.Beacons[b].Team.ToString(),
                sigil = Game.SigilName(s.Beacons[b].Sigil),
                durability = (int)s.Beacons[b].Durability,
            }).ToArray(),
            hexes = Board.AllHexes.Select(h => new { q = h.Q, r = h.R, zone = Zone(h) }).ToArray(),
            spawns = new[] { Team.A, Team.B }.SelectMany(t => Enum.GetValues<Role>().Select(r =>
            {
                HexCoord h = Board.SpawnHex(t, r);
                return new { q = h.Q, r = h.R, team = t.ToString(), role = r.ToString() };
            })).ToArray(),
            winner = s.Phase == Phase.MatchOver ? s.Winner.ToString() : null,
            endReason = s.Phase == Phase.MatchOver ? s.EndReason.ToString() : null,
            champions = Enumerable.Range(0, 10).Select(i => ChampionView(g, s, i, ladder, singles, chainParts)).ToArray(),
            roster = s.Phase == Phase.Draft ? g.Content.Champions.Select((d, i) => DefView(d, i)).ToArray() : null,
            legal = legal.Select((c, i) => LegalView(g, s, c, i)).ToArray(),
            events = log.Skip(start).Select((e, k) => new { n = start + k, kind = e.Kind.ToString(), text = e.Text }).ToArray(),
        };
    }

    // ───────────────────────────── champions ─────────────────────────────

    private static object ChampionView(Game g, MatchState s, int slot, bool ladder,
        HashSet<(int, int)> singles, HashSet<(int, int)> chainParts)
    {
        Champion c = s.Champions[slot];
        if (c.Def == 255)
        {
            return new { slot, team = c.Team.ToString(), role = c.Role.ToString(), drafted = false };
        }

        ChampionDef d = g.Def(c);
        bool placed = s.Phase != Phase.Draft;
        return new
        {
            slot,
            team = c.Team.ToString(),
            role = c.Role.ToString(),
            drafted = true,
            id = d.Id,
            name = d.Name,
            glyph = d.Glyph.ToString(),
            presence = !placed ? "draft" : c.Presence switch { Presence.OnBoard => "board", Presence.InSpawn => "spawn", _ => "dead" },
            q = c.Pos.Q,
            r = c.Pos.R,
            hp = c.Hp,
            maxHp = g.MaxHp(c),
            shield = (int)c.Shield,
            stats = new
            {
                pow = g.StatPermille(c, Stat.Pow),
                powNow = g.Pow(c),
                arm = g.Armour(c),
                rch = g.Reach(c),
                spd = g.Speed(c),
            },
            drift = Enum.GetValues<Stat>().Where(st => c.Drift[(int)st] != 0)
                .Select(st => new { stat = st.ToString().ToUpperInvariant(), permille = (int)c.Drift[(int)st] }).ToArray(),
            dying = c.Has(ChampFlags.Dying),
            acted = c.Has(ChampFlags.Acted),
            basicUsed = c.Has(ChampFlags.BasicUsed),
            openingDone = c.Has(ChampFlags.OpeningDone),
            poison = c.PoisonRounds > 0 ? new { amount = (int)c.PoisonAmount, rounds = (int)c.PoisonRounds } : null,
            respawnIn = c.Presence == Presence.Dead && placed ? Math.Max(0, c.RespawnIn - 1) : 0,
            passive = new { name = d.Passive.Name, text = Describe.Passive(d.Passive) },
            abilities = Enumerable.Range(0, 4).Select(a => AbilityView(g, s, slot, a, ladder, singles, chainParts)).ToArray(),
        };
    }

    private static object AbilityView(Game g, MatchState s, int slot, int a, bool ladder,
        HashSet<(int, int)> singles, HashSet<(int, int)> chainParts)
    {
        Champion c = s.Champions[slot];
        AbilityDef ab = g.Def(c).Abilities[a];
        bool onBoard = c.OnBoard && s.Phase != Phase.Draft;

        // Where it can reach, and what it could hit right now ignoring the ceiling and turn.
        var reach = new List<HexCoord>();
        var targets = new List<HexCoord>();
        if (onBoard)
        {
            if (ab.IsFree)
            {
                int range = ab.Target == TargetRule.EmptyHex ? ab.Effects[0].Amount : g.Range(c, ab);
                reach.AddRange(Board.AllHexes.Where(h => HexCoord.Distance(h, c.Pos) <= range && h != c.Pos || (ab.Target == TargetRule.Ally && h == c.Pos)));
            }
            else if (ab.Initiative == 3)
            {
                for (int f = 0; f < 6; f++) reach.AddRange(g.PatternCells(c, ab, f).Where(Board.Playable));
            }
            else
            {
                reach.AddRange(g.PatternCells(c, ab, 0).Where(Board.Playable));
            }

            foreach (Target t in g.AbilityTargets(s, slot, a)) targets.AddRange(TargetCells(g, s, slot, a, t));
        }

        (string state, string reason) = State(g, s, slot, a, ab, ladder, singles, chainParts, targets.Count > 0);
        int raw = ab.Effects.Any(e => e.Kind is EffectKind.Damage or EffectKind.Heal) && s.Phase != Phase.Draft
            ? (int)Arith.FloorDiv((long)g.Rules.AbilityBase * ab.Effects.First(e => e.Kind is EffectKind.Damage or EffectKind.Heal).Power * g.Pow(c), 1_000_000)
            : 0;

        return new
        {
            key = Keys[a],
            name = ab.Name,
            init = ab.Initiative,
            cooldown = ab.Cooldown,
            cd = (int)c.Cooldowns[a],
            targeting = Describe.Targeting(ab),
            effects = Describe.Effects(ab),
            amount = raw,
            printedSigil = ab.PrintedSigil >= 0 ? Game.SigilName(ab.PrintedSigil) : null,
            slotSigil = ab.SlotSigil >= 0 ? Game.SigilName(ab.SlotSigil) : null,
            activeSigils = onBoard ? SigilList(g.ActiveSigils(s, slot, a)) : [],
            mold = Describe.Mold(ab),
            opening = ab.Opening.Select(Describe.Instruction).ToArray(),
            state,
            reason,
            reach = reach.Distinct().Select(Xy).ToArray(),
            targets = targets.Distinct().Select(Xy).ToArray(),
        };
    }

    private static (string, string) State(Game g, MatchState s, int slot, int a, AbilityDef ab, bool ladder,
        HashSet<(int, int)> singles, HashSet<(int, int)> chainParts, bool hasTargets)
    {
        Champion c = s.Champions[slot];
        if (s.Phase == Phase.Opening)
        {
            if (c.Has(ChampFlags.OpeningDone)) return ("done", "already played its opening");
            if (singles.Contains((slot, a))) return ("usable", "");
            return g.OpeningAvailable(s, slot, a)
                ? ("idle", "available when this team places")
                : ("blocked", "an instruction would leave the board or hit an occupied hex");
        }

        if (c.Cooldowns[a] > 0) return ("cooldown", $"on cooldown for {c.Cooldowns[a]} more round{(c.Cooldowns[a] == 1 ? "" : "s")}");
        if (s.Phase == Phase.Draft) return ("idle", "");
        if (c.Presence == Presence.Dead) return ("unavailable", "dead");
        if (c.Presence == Presence.InSpawn) return ("unavailable", "in spawn — must move onto the board with a basic first");
        if (!ladder) return (hasTargets ? "idle" : "notarget", hasTargets ? "the ladder opens after the basics" : "nothing in range");
        if (c.Has(ChampFlags.Acted)) return ("acted", "already acted this half");
        if (singles.Contains((slot, a))) return ("usable", "");
        if (chainParts.Contains((slot, a))) return ("chain", "playable only as part of a chain");
        if (!hasTargets) return ("notarget", "nothing in range");
        if (ab.Initiative > s.Ceiling) return ("ceiling", $"initiative {ab.Initiative} is above the ceiling {s.Ceiling}");
        return ("idle", "waiting for its team's turn");
    }

    private static string[] SigilList(int mask) =>
        Enumerable.Range(0, 3).Where(i => (mask & (1 << i)) != 0).Select(Game.SigilName).ToArray();

    /// <summary>Static content for the draft screen.</summary>
    private static object DefView(ChampionDef d, int index) => new
    {
        index,
        id = d.Id,
        name = d.Name,
        role = d.Role.ToString(),
        glyph = d.Glyph.ToString(),
        stats = new
        {
            hp = d.Base(Stat.Vit) / 1000,
            pow = d.Base(Stat.Pow),
            arm = d.Base(Stat.Arm) / 1000,
            rch = Math.Clamp(d.Base(Stat.Rch) / 1000, 1, 3),
            spd = d.Base(Stat.Spd) / 1000,
        },
        passive = new { name = d.Passive.Name, text = Describe.Passive(d.Passive) },
        abilities = d.Abilities.Select((ab, a) => new
        {
            key = Keys[a],
            name = ab.Name,
            init = ab.Initiative,
            cooldown = ab.Cooldown,
            targeting = Describe.Targeting(ab),
            effects = Describe.Effects(ab),
            printedSigil = ab.PrintedSigil >= 0 ? Game.SigilName(ab.PrintedSigil) : null,
            slotSigil = ab.SlotSigil >= 0 ? Game.SigilName(ab.SlotSigil) : null,
            mold = Describe.Mold(ab),
            opening = ab.Opening.Select(Describe.Instruction).ToArray(),
        }).ToArray(),
    };

    // ───────────────────────────── legal commands ─────────────────────────────

    private static object LegalView(Game g, MatchState s, Command c, int index)
    {
        var cells = new List<HexCoord>();
        var click = new List<HexCoord>();
        switch (c.Kind)
        {
            case CommandKind.BasicMove:
            case CommandKind.BasicAttack:
                cells.AddRange(TargetCells(g, s, c.Champion, -1, c.Target));
                click.AddRange(cells);
                break;
            case CommandKind.OpeningFallback:
                HexCoord at = s.Champions[c.Champion].Pos;
                HexCoord to = c.Target.Facing == 255 ? at : at + Hex.Directions[c.Target.Facing];
                cells.Add(to);
                click.Add(to);
                break;
            case CommandKind.Ability:
                cells.AddRange(TargetCells(g, s, c.Champion, c.Ability, c.Target));
                if (c.IsChain)
                {
                    cells.AddRange(TargetCells(g, s, c.Champion2, c.Ability2, c.Target2));
                }
                else
                {
                    click.AddRange(ClickCells(g, s, c.Champion, c.Ability, c.Target));
                }

                break;
        }

        return new
        {
            i = index,
            kind = c.Kind.ToString(),
            champ = c.Champion == 255 ? -1 : c.Champion,
            ability = (int)c.Ability,
            champ2 = c.IsChain ? c.Champion2 : -1,
            ability2 = c.IsChain ? c.Ability2 : -1,
            facing = c.Target.Kind == TargetKind.Facing ? c.Target.Facing : -1,
            label = Label(g, s, c),
            cells = cells.Where(Board.Playable).Distinct().Select(Xy).ToArray(),
            click = click.Distinct().Select(Xy).ToArray(),
            preview = c.Kind == CommandKind.Draft ? null : Preview(g, s, c),
        };
    }

    /// <summary>Hexes a target covers: the target's hex, a nexus, or a pattern.</summary>
    private static IEnumerable<HexCoord> TargetCells(Game g, MatchState s, int slot, int ability, Target t)
    {
        switch (t.Kind)
        {
            case TargetKind.Champion: return [s.Champions[t.Index].Pos];
            case TargetKind.Tower: return [s.Towers[t.Index].Pos];
            case TargetKind.Nexus: return Board.NexusHexes((Team)t.Index);
            case TargetKind.Beacon:
            case TargetKind.Hex: return [t.Hex];
        }

        if (ability < 0) return [];
        Champion c = s.Champions[slot];
        AbilityDef a = g.Def(c).Abilities[ability];
        return g.PatternCells(c, a, t.Kind == TargetKind.Facing ? t.Facing : 0).Where(Board.Playable);
    }

    /// <summary>
    /// Where the player clicks to choose this command. A tier-3 facing is chosen by clicking
    /// the neighbour of the caster the pattern leans toward, so each facing has its own hex.
    /// </summary>
    private static IEnumerable<HexCoord> ClickCells(Game g, MatchState s, int slot, int ability, Target t)
    {
        if (t.Kind != TargetKind.Facing) return TargetCells(g, s, slot, ability, t);
        Champion c = s.Champions[slot];
        AbilityDef a = g.Def(c).Abilities[ability];
        int[] lean = Enumerable.Range(0, 6).Select(f => Lean(a, f)).ToArray();
        int dir = lean.Distinct().Count() == 6 ? lean[t.Facing] : t.Facing;
        return [c.Pos + Hex.Directions[dir]];
    }

    private static int Lean(AbilityDef a, int facing)
    {
        // Presentation only: floating point is fine outside Augury.Sim.
        double x = 0, y = 0;
        foreach (HexCoord off in a.Pattern)
        {
            HexCoord r = Hex.Rotate(off, facing);
            x += r.Q + r.R / 2.0;
            y += r.R * 0.8660254;
        }

        int best = 0;
        double bestDot = double.MinValue;
        for (int d = 0; d < 6; d++)
        {
            HexCoord n = Hex.Directions[d];
            double dot = x * (n.Q + n.R / 2.0) + y * n.R * 0.8660254;
            if (dot > bestDot)
            {
                bestDot = dot;
                best = d;
            }
        }

        return best;
    }

    private static string Label(Game g, MatchState s, Command c)
    {
        string Ab(int slot, int a) => g.Def(s.Champions[slot]).Abilities[a].Name;
        return c.Kind switch
        {
            CommandKind.Draft => $"Draft {g.Content.Champions[c.Ability].Name} ({(Role)(c.Champion % 5)})",
            CommandKind.OpeningPlay => $"{g.Name(s, c.Champion)} opens with {Ab(c.Champion, c.Ability)}",
            CommandKind.OpeningFallback => c.Target.Facing == 255
                ? $"{g.Name(s, c.Champion)} stays (enclosed)"
                : $"{g.Name(s, c.Champion)} falls back to {Game.Fmt(s.Champions[c.Champion].Pos + Hex.Directions[c.Target.Facing])}",
            CommandKind.BasicMove => $"{g.Name(s, c.Champion)} moves to {Game.Fmt(c.Target.Hex)}",
            CommandKind.BasicAttack => $"{g.Name(s, c.Champion)} basic-attacks {TargetName(g, s, c.Target)}",
            CommandKind.Ability when c.IsChain =>
                $"⛓ {g.Name(s, c.Champion)} {Ab(c.Champion, c.Ability)}{On(g, s, c.Target)} + {g.Name(s, c.Champion2)} {Ab(c.Champion2, c.Ability2)}{On(g, s, c.Target2)}",
            CommandKind.Ability => $"{g.Name(s, c.Champion)} {Ab(c.Champion, c.Ability)}{On(g, s, c.Target)}",
            CommandKind.Pass => "Pass",
            CommandKind.Decline => "Decline the Last Word",
            _ => c.Kind.ToString(),
        };
    }

    private static string On(Game g, MatchState s, Target t) => t.Kind switch
    {
        TargetKind.None => "",
        TargetKind.Facing => $" facing {t.Facing}",
        TargetKind.Hex => $" to {Game.Fmt(t.Hex)}",
        _ => $" on {TargetName(g, s, t)}",
    };

    private static string TargetName(Game g, MatchState s, Target t) => t.Kind switch
    {
        TargetKind.Champion => g.Name(s, t.Index),
        TargetKind.Tower => $"tower {Game.Fmt(s.Towers[t.Index].Pos)}",
        TargetKind.Nexus => $"{(Team)t.Index} nexus",
        TargetKind.Beacon => $"beacon {Game.Fmt(t.Hex)}",
        TargetKind.Hex => Game.Fmt(t.Hex),
        _ => "",
    };

    // ───────────────────────────── previews ─────────────────────────────

    private static object Preview(Game g, MatchState before, Command c)
    {
        MatchState after = before;
        var log = new List<GameEvent>();
        g.Apply(ref after, c, log);

        var champs = new List<object>();
        for (int i = 0; i < 10; i++)
        {
            Champion a = before.Champions[i], b = after.Champions[i];
            bool moved = a.Pos != b.Pos || a.Presence != b.Presence;
            if (a.Hp == b.Hp && a.Shield == b.Shield && !moved && a.PoisonRounds == b.PoisonRounds && a.Flags == b.Flags) continue;
            champs.Add(new
            {
                slot = i,
                hp = b.Hp,
                dHp = b.Hp - a.Hp,
                shield = (int)b.Shield,
                dShield = b.Shield - a.Shield,
                from = moved ? Xy(a.Pos) : null,
                to = moved && b.Presence != Presence.Dead ? Xy(b.Pos) : null,
                dies = a.Presence != Presence.Dead && b.Presence == Presence.Dead,
                dying = !a.Has(ChampFlags.Dying) && b.Has(ChampFlags.Dying),
                poisoned = b.PoisonRounds > a.PoisonRounds || b.PoisonAmount > a.PoisonAmount,
            });
        }

        var towers = Enumerable.Range(0, 5)
            .Where(t => before.Towers[t].Hp != after.Towers[t].Hp || before.Towers[t].Owner != after.Towers[t].Owner)
            .Select(t => new
            {
                i = t,
                hp = (int)after.Towers[t].Hp,
                dHp = after.Towers[t].Hp - before.Towers[t].Hp,
                owner = after.Towers[t].Owner.ToString(),
                captured = before.Towers[t].Owner != after.Towers[t].Owner,
            }).ToArray();

        var beacons = Enumerable.Range(0, 12)
            .Where(b => after.Beacons[b].Team != Team.None && (before.Beacons[b].Team == Team.None || before.Beacons[b].Pos != after.Beacons[b].Pos))
            .Select(b => new { q = after.Beacons[b].Pos.Q, r = after.Beacons[b].Pos.R, team = after.Beacons[b].Team.ToString(), sigil = Game.SigilName(after.Beacons[b].Sigil) })
            .ToArray();

        return new
        {
            events = log.Take(14).Select(e => e.Text.Trim()).ToArray(),
            more = Math.Max(0, log.Count - 14),
            champs,
            towers,
            beacons,
            nexus = new[] { after.NexusHp[0] - before.NexusHp[0], after.NexusHp[1] - before.NexusHp[1] },
            score = new[] { after.Score[0] - before.Score[0], after.Score[1] - before.Score[1] },
            endsHalf = after.Round != before.Round || after.Half != before.Half || after.Phase != before.Phase && after.Phase is Phase.Basic,
            endsMatch = after.Phase == Phase.MatchOver,
        };
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private static int[] Xy(HexCoord h) => [h.Q, h.R];

    private static string Zone(HexCoord h)
    {
        if (Math.Abs(Board.File(h)) >= 5) return "jungle";
        return h.Q == 0 || h.Q + h.R == 0 ? "lane" : "open";
    }
}
