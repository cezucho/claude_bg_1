using Augury.Cli;
using Augury.Sim;
using Augury.Sim.AI;

// AUGURY — terminal client for the first playable build.
//   augury                 interactive mode menu
//   augury --vs-ai A|B     play team A or B against the AI
//   augury --hotseat       two humans at one keyboard
//   augury --watch         AI against AI
//   --no-color             plain text

var opts = args.ToList();
if (opts.Remove("--no-color")) Ansi.On = false;

Game game;
try
{
    game = Game.LoadDefault();
}
catch (Exception e)
{
    Console.Error.WriteLine($"Could not load game content: {e.Message}");
    Console.Error.WriteLine("Run from inside the repository so assets/data can be found.");
    return 1;
}

var humans = new HashSet<Team>();
if (opts.Contains("--hotseat")) { humans.Add(Team.A); humans.Add(Team.B); }
else if (opts.Contains("--watch")) { }
else if (opts.IndexOf("--vs-ai") is int i && i >= 0)
{
    humans.Add(i + 1 < opts.Count && opts[i + 1].Equals("B", StringComparison.OrdinalIgnoreCase) ? Team.B : Team.A);
}
else
{
    Console.WriteLine(Ansi.Bold("AUGURY — first playable"));
    Console.WriteLine("  1) Play team A (bottom) against the AI");
    Console.WriteLine("  2) Play team B (top) against the AI");
    Console.WriteLine("  3) Hotseat — two players");
    Console.WriteLine("  4) Watch the AI play itself");
    Console.Write("> ");
    switch (Console.ReadLine()?.Trim())
    {
        case "2": humans.Add(Team.B); break;
        case "3": humans.Add(Team.A); humans.Add(Team.B); break;
        case "4": break;
        case null: return 0;
        default: humans.Add(Team.A); break;
    }
}

var session = new Session(game, humans);
return session.Run();

namespace Augury.Cli
{
    internal sealed class Session(Game game, HashSet<Team> humans)
    {
        private readonly Game _game = game;
        private readonly Renderer _r = new(game);
        private readonly IAgent _ai = new HeuristicAgent(game);

        // The AI drafts at random so matches differ; this is presentation, not simulation,
        // so a time-based seed is allowed here (ADR-0002 binds Augury.Sim only).
        private readonly IAgent _drafter = new RandomAgent((uint)Environment.TickCount);
        private readonly List<GameEvent> _log = [];
        private int _shown;

        public int Run()
        {
            MatchState s = _game.NewMatch();
            bool watching = humans.Count == 0;

            while (s.Phase != Phase.MatchOver)
            {
                List<Command> legal = _game.Legal(s);
                Command cmd;

                if (legal.Count == 1 && s.Phase is Phase.Draft)
                {
                    cmd = legal[0];   // one champion per role: nothing to choose
                }
                else if (humans.Contains(s.Active))
                {
                    Command? chosen = HumanTurn(s, legal);
                    if (chosen is null) return 0;
                    cmd = chosen.Value;
                }
                else
                {
                    cmd = s.Phase == Phase.Draft ? _drafter.Choose(s, legal) : _ai.Choose(s, legal);
                }

                _game.Apply(ref s, cmd, _log);
                if (watching) FlushLog();
            }

            FlushLog();
            Console.WriteLine();
            Console.WriteLine(_r.Board(s));
            Console.WriteLine(_r.Header(s));
            Console.WriteLine(Ansi.Bold($"\n  MATCH OVER — {(s.Winner == Team.None ? "draw" : $"{s.Winner} wins")} by {s.EndReason} after {s.Round} rounds."));
            return 0;
        }

        private void FlushLog()
        {
            for (; _shown < _log.Count; _shown++)
            {
                GameEvent e = _log[_shown];
                string text = e.Kind switch
                {
                    EventKind.Phase when e.Text.StartsWith("━━") => "\n" + Ansi.Bold(e.Text),
                    EventKind.Death or EventKind.Dying or EventKind.MatchOver => Ansi.Hi(e.Text),
                    EventKind.Structure when e.Text.Contains("CAPTURES") => Ansi.Hi(e.Text),
                    EventKind.Passive => Ansi.Purple(e.Text),
                    EventKind.Phase => Ansi.Dim(e.Text),
                    _ => e.Text,
                };
                Console.WriteLine(text);
            }
        }

        // ───────────────────────────── human turn ─────────────────────────────

        private sealed record Option(string Label, List<Command> Commands);

        private Command? HumanTurn(in MatchState s, List<Command> legal)
        {
            List<Option> options = TopLevel(s, legal);
            MatchState snapshot = s;
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine(Ansi.Dim("──────────────────────────── what happened ────────────────────────────"));
                FlushLog();
                Console.WriteLine();
                Console.WriteLine(_r.Board(snapshot, MarksFor(snapshot, legal.Where(c => c.Kind is CommandKind.OpeningFallback).ToList())));
                Console.WriteLine(_r.Header(snapshot));
                Console.WriteLine();
                Console.WriteLine(_r.Champions(snapshot));
                Console.WriteLine(Ansi.Bold(Prompt(snapshot)));
                for (int k = 0; k < options.Count; k++) Console.WriteLine($"  {k + 1,2}) {options[k].Label}");
                Console.WriteLine(Ansi.Dim("  i <#> inspect champion · h help · q quit"));
                Console.Write(Ansi.Team(snapshot.Active, $"{snapshot.Active}> "));

                string? input = Console.ReadLine();
                if (input is null) return null;
                input = input.Trim();
                if (input == "q") return null;
                if (input == "h") { Help(); continue; }
                if (input.StartsWith('i') && int.TryParse(input[1..].Trim(), out int who) && who is >= 0 and < 10)
                {
                    Console.WriteLine(_r.Inspect(snapshot, who));
                    Pause();
                    continue;
                }

                if (!int.TryParse(input, out int n) || n < 1 || n > options.Count) continue;
                Option opt = options[n - 1];
                if (opt.Commands.Count == 1) return opt.Commands[0];
                Command? target = ChooseTarget(snapshot, opt);
                if (target is not null) return target;
            }
        }

        private Command? ChooseTarget(in MatchState s, Option opt)
        {
            Dictionary<HexCoord, char> marks = [];
            var labels = new List<string>();
            for (int k = 0; k < opt.Commands.Count; k++)
            {
                char letter = Letter(k);
                labels.Add($"  ({letter}) {TargetLabel(s, opt.Commands[k])}");
                HexCoord? hex = MarkHex(s, opt.Commands[k]);
                if (hex is not null) marks.TryAdd(hex.Value, letter);
            }

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine(_r.Board(s, marks));
                Console.WriteLine(Ansi.Bold(opt.Label));
                foreach (string l in labels) Console.WriteLine(l);
                Console.Write("letter, or b to go back > ");
                string? input = Console.ReadLine()?.Trim();
                if (input is null || input == "b") return null;
                int idx = input.Length == 1 ? IndexOfLetter(input[0]) : -1;
                if (idx >= 0 && idx < opt.Commands.Count) return opt.Commands[idx];
            }
        }

        private List<Option> TopLevel(in MatchState s, List<Command> legal)
        {
            var result = new List<Option>();
            switch (s.Phase)
            {
                case Phase.Draft:
                    foreach (Command c in legal)
                    {
                        var def = _game.Content.Champions[c.Ability];
                        string kit = string.Join("-", def.Abilities.Select(a => a.Initiative));
                        result.Add(new Option(
                            $"{(Sim.Content.Role)(c.Champion % 5),-8} {def.Name,-11} kit {kit}  "
                            + Ansi.Dim($"{string.Join(", ", def.Abilities.Select(a => a.Name))} · passive {def.Passive.Name}"), [c]));
                    }

                    break;

                case Phase.Opening:
                    var fallback = legal.Where(c => c.Kind == CommandKind.OpeningFallback).ToList();
                    if (fallback.Count > 0)
                    {
                        result.Add(new Option($"FALLBACK — no ability is available; {_game.Name(s, fallback[0].Champion)} moves one hex", fallback));
                        break;
                    }

                    foreach (Command c in legal)
                    {
                        var ab = _game.Def(s.Champions[c.Champion]).Abilities[c.Ability];
                        result.Add(new Option($"{_game.Name(s, c.Champion),-15} {ab.Name,-14} {Ansi.Dim(Describe.Opening(ab))}", [c]));
                    }

                    break;

                case Phase.Basic:
                    foreach (var g in legal.GroupBy(c => (c.Champion, c.Kind)))
                    {
                        string verb = g.Key.Kind == CommandKind.BasicMove ? "move" : "basic attack";
                        result.Add(new Option($"{_game.Name(s, g.Key.Champion),-15} {verb} ({g.Count()} options)", g.ToList()));
                    }

                    break;

                case Phase.Ladder:
                case Phase.LastWord:
                    foreach (var g in legal.Where(c => c.Kind == CommandKind.Ability && !c.IsChain).GroupBy(c => (c.Champion, c.Ability)))
                    {
                        var ab = _game.Def(s.Champions[g.Key.Champion]).Abilities[g.Key.Ability];
                        result.Add(new Option($"{_game.Name(s, g.Key.Champion),-15} {Describe.Ability(ab)}", g.ToList()));
                    }

                    var chains = legal.Where(c => c.IsChain).ToList();
                    if (chains.Count > 0) result.Add(new Option(Ansi.Purple($"⛓ CHAIN — two abilities as one step ({chains.Count} options)"), chains));
                    if (legal.Contains(Command.PassCmd)) result.Add(new Option($"PASS — give {MatchState.Other(s.Active)} the Last Word", [Command.PassCmd]));
                    if (legal.Contains(Command.DeclineCmd)) result.Add(new Option("DECLINE the Last Word", [Command.DeclineCmd]));
                    break;
            }

            return result;
        }

        private string Prompt(in MatchState s) => s.Phase switch
        {
            Phase.Draft => "Draft a champion:",
            Phase.Opening => "Opening — play one champion's ability (all three instructions run):",
            Phase.Basic => $"Basic action ({s.BasicsTaken[s.Active == Team.A ? 0 : 1] + 1} of {_game.Rules.BasicsPerHalf}) — move one champion, or basic-attack:",
            Phase.Ladder => $"Ladder — play an ability at initiative ≤ {s.Ceiling}, or pass:",
            Phase.LastWord => $"LAST WORD — one unanswerable ability at ≤ {s.Ceiling}, or decline:",
            _ => "",
        };

        private string TargetLabel(in MatchState s, Command c)
        {
            if (c.IsChain)
            {
                var a1 = _game.Def(s.Champions[c.Champion]).Abilities[c.Ability];
                var a2 = _game.Def(s.Champions[c.Champion2]).Abilities[c.Ability2];
                return $"{_game.Name(s, c.Champion)} {a1.Name}{Hits(s, c.Champion, c.Ability, c.Target)}  +  "
                       + $"{_game.Name(s, c.Champion2)} {a2.Name} [init {a2.Initiative}]{Hits(s, c.Champion2, c.Ability2, c.Target2)}";
            }

            if (c.Kind == CommandKind.OpeningFallback)
            {
                return c.Target.Facing == 255 ? "stay (enclosed)" : Game.Fmt(s.Champions[c.Champion].Pos + Hex.Directions[c.Target.Facing]);
            }

            return c.Kind == CommandKind.BasicMove ? $"move to {Game.Fmt(c.Target.Hex)}" : Hits(s, c.Champion, c.Ability, c.Target).TrimStart(' ', '→');
        }

        private string Hits(in MatchState s, int slot, int ability, Target t)
        {
            switch (t.Kind)
            {
                case TargetKind.Champion:
                    Champion v = s.Champions[t.Index];
                    return $" → {_game.Name(s, t.Index)} ({v.Hp} HP)";
                case TargetKind.Tower:
                    return $" → tower {Game.Fmt(s.Towers[t.Index].Pos)} ({s.Towers[t.Index].Hp} HP)";
                case TargetKind.Nexus:
                    return $" → NEXUS {(Team)t.Index} ({s.NexusHp[t.Index]} HP)";
                case TargetKind.Beacon:
                    return $" → beacon at {Game.Fmt(t.Hex)}";
                case TargetKind.Hex:
                    return $" → dash to {Game.Fmt(t.Hex)}";
            }

            // Patterns: say what they hit.
            Champion caster = s.Champions[slot];
            var ab = _game.Def(caster).Abilities[ability];
            HexCoord[] cells = _game.PatternCells(caster, ab, t.Kind == TargetKind.Facing ? t.Facing : 0);
            var hit = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                Champion e = s.Champions[i];
                if (e.Team != caster.Team && e.OnBoard && cells.Contains(e.Pos)) hit.Add(_game.Name(s, i));
            }

            foreach (HexCoord h in cells)
            {
                int tw = Sim.Board.TowerAt(h);
                if (tw >= 0 && s.Towers[tw].Owner != caster.Team) hit.Add($"tower {Game.Fmt(h)}");
            }

            string facing = t.Kind == TargetKind.Facing ? $" facing {t.Facing}" : " (fixed)";
            return $"{facing} → hits {(hit.Count == 0 ? "nexus" : string.Join(", ", hit))}";
        }

        private HexCoord? MarkHex(in MatchState s, Command c)
        {
            if (c.IsChain) return null;
            return c.Kind switch
            {
                CommandKind.OpeningFallback when c.Target.Facing != 255 => s.Champions[c.Champion].Pos + Hex.Directions[c.Target.Facing],
                _ => c.Target.Kind switch
                {
                    TargetKind.Hex => c.Target.Hex,
                    TargetKind.Champion => s.Champions[c.Target.Index].Pos,
                    TargetKind.Tower => s.Towers[c.Target.Index].Pos,
                    TargetKind.Beacon => c.Target.Hex,
                    _ => null,
                },
            };
        }

        private Dictionary<HexCoord, char>? MarksFor(in MatchState s, List<Command> cmds)
        {
            if (cmds.Count == 0) return null;
            var marks = new Dictionary<HexCoord, char>();
            for (int k = 0; k < cmds.Count; k++)
            {
                HexCoord? h = MarkHex(s, cmds[k]);
                if (h is not null) marks.TryAdd(h.Value, Letter(k));
            }

            return marks;
        }

        private static char Letter(int k) => k < 26 ? (char)('a' + k) : k < 52 ? (char)('A' + k - 26) : '?';

        private static int IndexOfLetter(char c) => c is >= 'a' and <= 'z' ? c - 'a' : c is >= 'A' and <= 'Z' ? c - 'A' + 26 : -1;

        private static void Pause()
        {
            Console.Write(Ansi.Dim("(enter to continue) "));
            Console.ReadLine();
        }

        private static void Help()
        {
            Console.WriteLine("""

              HOW A ROUND WORKS
                Each half: BASICS first (each team moves or basic-attacks with 2 different champions),
                then the LADDER. On the ladder you play one ability at initiative ≤ the ceiling; the
                ceiling drops to what you played. Each champion acts once per half. PASS gives the
                other side one unanswerable Last Word. If you have nothing legal, the half ends.
                Round close: death check → poison and tower shots (≤0 here = DYING, one more round)
                → siege: your towers fire at the enemy nexus → cooldowns tick, respawns.

              WINNING  Destroy the enemy NEXUS. Each tower you hold hits it every round close, each
                       enemy death hits it too, and once you've taken one of their home towers
                       you can attack it directly.

              CHAINS   Two abilities sharing an active sigil resolve as one step, no answer between,
                       and the second may exceed the ceiling. Slot sigils light up near your beacon.

              Full rules: design/mvp-rules.md
            """);
            Pause();
        }
    }
}
