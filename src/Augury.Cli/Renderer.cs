using System.Text;
using Augury.Sim;

namespace Augury.Cli;

/// <summary>Draws the board and the full inspectable state (Pillar 1: nothing hidden).</summary>
internal sealed class Renderer(Game game)
{
    private readonly Game _game = game;

    /// <summary>
    /// The board with team B at the top and team A at the bottom. Hexes listed in
    /// <paramref name="marks"/> show a selection letter instead of their contents.
    /// </summary>
    public string Board(in MatchState s, IReadOnlyDictionary<HexCoord, char>? marks = null)
    {
        var sb = new StringBuilder();
        const int Base = 20;
        var all = Sim.Board.AllHexes.Concat(Sim.Board.Layout.SpawnA).Concat(Sim.Board.Layout.SpawnA.Select(Hex.Mirror)).ToArray();
        int minR = all.Min(h => h.R), maxR = all.Max(h => h.R), minQ = all.Min(h => h.Q), maxQ = all.Max(h => h.Q);
        for (int r = maxR; r >= minR; r--)
        {
            var cells = new SortedDictionary<int, string>();
            for (int q = minQ; q <= maxQ; q++)
            {
                var h = new HexCoord(q, r);
                string? cell = Cell(s, h, marks);
                if (cell is not null) cells[4 * q + 2 * r + Base] = cell;
            }

            if (cells.Count == 0) continue;
            string label = Sim.Board.AllHexes.Any(h => h.R == r) ? $"r{r,3} " : "     ";
            var line = new StringBuilder(Ansi.Dim(label));
            int col = 0;
            foreach (var (c, text) in cells)
            {
                line.Append(new string(' ', Math.Max(0, c - col)));
                line.Append(text);
                col = c + 3;
            }

            sb.AppendLine(line.ToString());
        }

        sb.AppendLine(Ansi.Dim("     A = UPPER CASE (bottom)   b = lower case (top)   ! dying   ~ acted"));
        sb.AppendLine(Ansi.Dim("     T tower (owner)  N nexus  *1 beacon (sigil)  · open  = lane  . jungle  _ spawn"));
        return sb.ToString();
    }

    private string? Cell(in MatchState s, HexCoord h, IReadOnlyDictionary<HexCoord, char>? marks)
    {
        if (marks is not null && marks.TryGetValue(h, out char m)) return Ansi.Hi($"({m})");

        int who = Game.ChampionAt(s, h);
        if (who < 0)
        {
            for (int i = 0; i < 10; i++)
            {
                if (s.Champions[i].Presence == Presence.InSpawn && s.Champions[i].Pos == h) who = i;
            }
        }

        if (who >= 0)
        {
            Champion c = s.Champions[who];
            char g = _game.Def(c).Glyph;
            string glyph = c.Team == Team.A ? char.ToUpperInvariant(g).ToString() : char.ToLowerInvariant(g).ToString();
            string mark = c.Has(ChampFlags.Dying) ? "!" : c.Has(ChampFlags.Acted) ? "~" : " ";
            return Ansi.Team(c.Team, $" {glyph}{mark}");
        }

        bool playable = Sim.Board.Playable(h);
        if (!playable)
        {
            bool spawn = Enumerable.Range(0, 5).Any(r =>
                Sim.Board.SpawnHex(Team.A, (Sim.Content.Role)r) == h || Sim.Board.SpawnHex(Team.B, (Sim.Content.Role)r) == h);
            return spawn ? Ansi.Dim(" _ ") : null;
        }

        int tower = Sim.Board.TowerAt(h);
        if (tower >= 0)
        {
            Team owner = s.Towers[tower].Owner;
            return Ansi.Team(owner, owner == Team.None ? " T·" : $" T{owner}");
        }

        if (Sim.Board.IsNexusHex(h, Team.A)) return Ansi.A(" N ");
        if (Sim.Board.IsNexusHex(h, Team.B)) return Ansi.B(" N ");

        for (int b = 0; b < 12; b++)
        {
            Beacon beacon = s.Beacons[b];
            if (beacon.Team != Team.None && beacon.Pos == h) return Ansi.Team(beacon.Team, $" *{beacon.Sigil + 1}");
        }

        bool jungle = Sim.Board.Layout.Jungle.Contains(h), lane = Sim.Board.Layout.Lanes.Contains(h);
        return Ansi.Dim(jungle ? " . " : lane ? " = " : " · ");
    }

    /// <summary>Header line: round, phase, whose turn, ceiling, score, structures.</summary>
    public string Header(in MatchState s)
    {
        string phase = s.Phase switch
        {
            Phase.Ladder => $"LADDER  ceiling {s.Ceiling}",
            Phase.LastWord => $"LAST WORD  ceiling {s.Ceiling}",
            Phase.Basic => $"BASICS  A {s.BasicsTaken[0]}/{_game.Rules.BasicsPerHalf}  B {s.BasicsTaken[1]}/{_game.Rules.BasicsPerHalf}",
            _ => s.Phase.ToString().ToUpperInvariant(),
        };
        string round = s.Round > 0 ? $"Round {s.Round} · Half {s.Half} · " : "";
        string score = $"Siege: each tower held hits the enemy nexus for {_game.Rules.TowerSiege} per round, each death costs your nexus {_game.Rules.KillSiege}";
        string nexus = $"Nexus {Ansi.A($"A {Math.Max(0, s.NexusHp[0])}")}{(Game.NexusVulnerable(s, Team.A) ? Ansi.Hi(" OPEN") : "")}"
                       + $" / {Ansi.B($"B {Math.Max(0, s.NexusHp[1])}")}{(Game.NexusVulnerable(s, Team.B) ? Ansi.Hi(" OPEN") : "")}";
        var towers = new StringBuilder("Towers ");
        for (int t = 0; t < 5; t++)
        {
            Tower tw = s.Towers[t];
            towers.Append(Ansi.Team(tw.Owner, $"{Game.Fmt(tw.Pos)}{(tw.Owner == Team.None ? "·" : tw.Owner.ToString())}{tw.Hp} "));
        }

        return $"{Ansi.Bold($"{round}{phase}")}   {Ansi.Team(s.Active, $"▶ {s.Active} to act")}\n{score}   {nexus}\n{towers}";
    }

    /// <summary>One line per champion: HP, stats, flags and cooldowns.</summary>
    public string Champions(in MatchState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Ansi.Dim("  #  champion         HP      shld  POW   ARM RCH SPD  cooldowns Q W E R   state"));
        for (int i = 0; i < 10; i++)
        {
            Champion c = s.Champions[i];
            if (c.Def == 255) continue;
            var def = _game.Def(c);
            string where = c.Presence switch
            {
                Presence.Dead => $"DEAD ({c.RespawnIn - 1})",
                Presence.InSpawn => "in spawn",
                _ => Game.Fmt(c.Pos),
            };
            var flags = new List<string>();
            if (c.Has(ChampFlags.Dying)) flags.Add("DYING");
            if (c.PoisonRounds > 0) flags.Add($"poison {c.PoisonAmount}×{c.PoisonRounds}");
            if (s.Phase is Phase.Ladder or Phase.LastWord && c.Has(ChampFlags.Acted)) flags.Add("acted");
            if (s.Phase == Phase.Basic && c.Has(ChampFlags.BasicUsed)) flags.Add("basic used");
            string cds = string.Join(" ", Enumerable.Range(0, 4).Select(a => c.Cooldowns[a] == 0 ? "·" : c.Cooldowns[a].ToString()));
            string hp = c.Presence == Presence.Dead ? "  –   " : $"{c.Hp,3}/{_game.MaxHp(c),-3}";
            string line = $"  {i}  {c.Team}:{def.Name,-12} {hp}  {(c.Shield > 0 ? c.Shield.ToString() : "·"),4}  "
                          + $"{_game.StatPermille(c, Sim.Content.Stat.Pow) / 1000.0,4:0.00}  {_game.Armour(c),3} {_game.Reach(c),3} {_game.Speed(c),3}"
                          + $"  {cds,-9}           {where} {string.Join(", ", flags)}";
            sb.AppendLine(Ansi.Team(c.Team, line));
        }

        return sb.ToString();
    }

    /// <summary>Everything about one champion: kit, opening instructions, passive, drift.</summary>
    public string Inspect(in MatchState s, int slot)
    {
        Champion c = s.Champions[slot];
        var def = _game.Def(c);
        var sb = new StringBuilder();
        sb.AppendLine(Ansi.Team(c.Team, Ansi.Bold($"{c.Team}:{def.Name} — {def.Role}")));
        sb.AppendLine($"  Passive  {Describe.Passive(def.Passive)}");
        string[] keys = ["Q", "W", "E", "R"];
        for (int a = 0; a < 4; a++)
        {
            var ab = _game.Kit(c, a);
            string line = ab is null ? "(no spell yet)" : Describe.Ability(ab);
            sb.AppendLine($"  {keys[a]}  {line}" + (c.Cooldowns[a] > 0 ? Ansi.Dim($"  (cooldown {c.Cooldowns[a]})") : ""));
            sb.AppendLine(Ansi.Dim($"       opening {_game.OpeningName(c, a)}: {Describe.Opening(_game.OpeningOf(c, a))}"));
        }

        var status = new List<string>();
        if (c.Rooted) status.Add("ROOTED");
        if (c.Burning) status.Add($"burning {c.BurnAmount} ({c.BurnRounds} rd)");
        if (c.PoisonRounds > 0) status.Add($"poisoned {c.PoisonAmount} ({c.PoisonRounds} rd)");
        if (c.Mark > 0) status.Add($"marked +{c.Mark}");
        if (c.ExhaustHalves > 0) status.Add("exhausted");
        if (c.Unstoppable) status.Add("unstoppable");
        if (c.WoundRounds > 0) status.Add($"wounded ({c.WoundRounds} rd)");
        if (status.Count > 0) sb.AppendLine($"  Status   {string.Join(", ", status)}");

        var drift = Enum.GetValues<Sim.Content.Stat>().Where(st => c.Drift[(int)st] != 0)
            .Select(st => $"{st} {(c.Drift[(int)st] > 0 ? "+" : "")}{c.Drift[(int)st]}");
        sb.AppendLine($"  Molding drift (permille): {(drift.Any() ? string.Join(", ", drift) : "none yet")}");
        return sb.ToString();
    }
}
