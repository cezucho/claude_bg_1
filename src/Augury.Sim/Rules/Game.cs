using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>
/// The rules. Stateless apart from content and configuration: every method takes the
/// match state explicitly, so the AI can clone state by assignment and search freely.
/// </summary>
/// <remarks>
/// <para>The single mutation path (ADR-0004) is <see cref="Apply"/>. After every command
/// the engine advances automatically through anything that needs no decision — skipped
/// basics, exhausted ladders, round close — and stops at the next real decision.</para>
/// <para>Rules are stated in <c>design/mvp-rules.md</c>; section numbers are cited inline.</para>
/// </remarks>
public sealed partial class Game
{
    /// <summary>Creates a rules engine over loaded content.</summary>
    public Game(ContentDb content, RulesConfig rules)
    {
        Content = content;
        Rules = rules;
        _roundOneOpener = rules.RoundOneOpener.Trim().ToUpperInvariant() == "A" ? Team.A : Team.B;
    }

    private readonly Team _roundOneOpener;

    /// <summary>The roster.</summary>
    public ContentDb Content { get; }

    /// <summary>Tunable numbers.</summary>
    public RulesConfig Rules { get; }

    /// <summary>Loads content and rules from <c>assets/data</c>.</summary>
    public static Game LoadDefault() =>
        new(ContentLoader.LoadDirectory(ContentLoader.FindChampionDirectory()), RulesConfig.LoadDefault());

    // ───────────────────────────── setup ─────────────────────────────

    /// <summary>A fresh match in the draft (§2).</summary>
    public MatchState NewMatch()
    {
        var s = new MatchState
        {
            Phase = Phase.Draft,
            Active = DraftPicker(0),
            RoundOpener = _roundOneOpener,
            Winner = Team.None,
        };

        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            c.Team = i < 5 ? Team.A : Team.B;
            c.Role = (Role)(i % 5);
            c.Def = 255;
        }

        for (int t = 0; t < 5; t++)
        {
            s.Towers[t] = new Tower
            {
                Pos = Board.TowerHexes[t],
                Hp = (short)Rules.TowerHp,
                Owner = t == 0 ? Team.None : t <= 2 ? Team.A : Team.B,
                Home = t == 0 ? Team.None : t <= 2 ? Team.A : Team.B,
            };
        }

        for (int b = 0; b < 12; b++) s.Beacons[b].Team = Team.None;
        s.NexusHp[0] = Rules.NexusHp;
        s.NexusHp[1] = Rules.NexusHp;
        return s;
    }

    /// <summary>A match with the draft already made, positioned at the Opening Phase.</summary>
    /// <param name="picksA">Content index per role for team A.</param>
    /// <param name="picksB">Content index per role for team B.</param>
    public MatchState NewMatch(IReadOnlyList<int> picksA, IReadOnlyList<int> picksB)
    {
        MatchState s = NewMatch();
        for (int r = 0; r < 5; r++)
        {
            s.Champions[r].Def = (byte)picksA[r];
            s.Champions[5 + r].Def = (byte)picksB[r];
        }

        s.DraftPicks = 10;
        Normalize(ref s, null);
        return s;
    }

    /// <summary>Snake draft order A,B,B,A,A,B,B,A,A,B (D-020).</summary>
    public static Team DraftPicker(int pick) => pick switch
    {
        0 or 3 or 4 or 7 or 8 => Team.A,
        _ => Team.B,
    };

    private void StartOpening(ref MatchState s, List<GameEvent>? log)
    {
        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            c.Presence = Presence.OnBoard;
            c.Pos = Board.StartHex(c.Team, c.Role);
            c.Hp = MaxHp(c);
        }

        s.Phase = Phase.Opening;
        s.Active = MatchState.Other(_roundOneOpener);
        Log(log, EventKind.Phase, $"Opening Phase — {s.Active} places first.");
    }

    // ───────────────────────────── stat reads ─────────────────────────────

    /// <summary>The champion's definition.</summary>
    public ChampionDef Def(in Champion c) => Content.Champions[c.Def];

    /// <summary>Base plus drift, permille.</summary>
    public int StatPermille(in Champion c, Stat s) => Def(c).Base(s) + c.Drift[(int)s];

    /// <summary>Maximum HP.</summary>
    public int MaxHp(in Champion c) => Math.Max(1, (int)Arith.FloorDiv(StatPermille(c, Stat.Vit), 1000));

    /// <summary>Effective POW in permille, halved while Dying (D-007).</summary>
    public int Pow(in Champion c)
    {
        int pow = Math.Max(0, StatPermille(c, Stat.Pow));
        return c.Has(ChampFlags.Dying) ? Arith.ScalePermille(pow, Rules.DyingPowPermille) : pow;
    }

    /// <summary>Flat armour.</summary>
    public int Armour(in Champion c) => Math.Max(0, (int)Arith.FloorDiv(StatPermille(c, Stat.Arm), 1000));

    /// <summary>Reach in hexes, 1–3.</summary>
    public int Reach(in Champion c) => Math.Clamp((int)Arith.FloorDiv(StatPermille(c, Stat.Rch), 1000), 1, 3);

    /// <summary>Speed in hexes per basic move.</summary>
    public int Speed(in Champion c) => Math.Max(0, (int)Arith.FloorDiv(StatPermille(c, Stat.Spd), 1000));

    /// <summary>Free-targeting range of an ability for this champion, 1–3.</summary>
    public int Range(in Champion c, AbilityDef a) => Math.Clamp(Reach(c) + a.RangeBonus, 1, 3);

    // ───────────────────────────── flow ─────────────────────────────

    /// <summary>
    /// Applies a legal command and advances to the next decision. The caller is responsible
    /// for passing a command from <c>Legal</c>; illegal commands are not re-validated
    /// on the hot path.
    /// </summary>
    public void Apply(ref MatchState s, in Command cmd, List<GameEvent>? log = null)
    {
        switch (cmd.Kind)
        {
            case CommandKind.Draft: ApplyDraft(ref s, cmd, log); break;
            case CommandKind.OpeningPlay: ApplyOpeningPlay(ref s, cmd, log); break;
            case CommandKind.OpeningFallback: ApplyOpeningFallback(ref s, cmd, log); break;
            case CommandKind.BasicMove: ApplyBasicMove(ref s, cmd, log); break;
            case CommandKind.BasicAttack: ApplyBasicAttack(ref s, cmd, log); break;
            case CommandKind.Ability: ApplyAbility(ref s, cmd, log); break;
            case CommandKind.Pass: ApplyPass(ref s, log); break;
            case CommandKind.Decline: ApplyDecline(ref s, log); break;
            default: throw new InvalidOperationException($"Unknown command {cmd.Kind}.");
        }

        Normalize(ref s, log);
    }

    /// <summary>Every legal command for the active team.</summary>
    public List<Command> Legal(in MatchState s)
    {
        var into = new List<Command>();
        Legal(s, into);
        return into;
    }

    /// <summary>Every legal command for the active team, appended to a caller-owned list.</summary>
    public void Legal(in MatchState s, List<Command> into)
    {
        switch (s.Phase)
        {
            case Phase.Draft: LegalDraft(s, into); break;
            case Phase.Opening: LegalOpening(s, into); break;
            case Phase.Basic: LegalBasics(s, s.Active, into); break;
            case Phase.Ladder:
                EnumerateAbilities(s, s.Active, into);
                if (into.Count > 0) into.Add(Command.PassCmd);
                break;
            case Phase.LastWord:
                EnumerateAbilities(s, s.Active, into);
                into.Add(Command.DeclineCmd);
                break;
        }
    }

    /// <summary>Advances through every state that needs no decision.</summary>
    private void Normalize(ref MatchState s, List<GameEvent>? log)
    {
        for (int guard = 0; guard < 1000; guard++)
        {
            switch (s.Phase)
            {
                case Phase.MatchOver:
                    return;

                case Phase.Draft:
                    if (s.DraftPicks < 10) return;
                    StartOpening(ref s, log);
                    continue;

                case Phase.Opening:
                    if (OpeningComplete(s, Team.A) && OpeningComplete(s, Team.B))
                    {
                        StartRound(ref s, 1, log);
                        continue;
                    }

                    if (OpeningComplete(s, s.Active)) s.Active = MatchState.Other(s.Active);
                    return;

                case Phase.Basic:
                    if (CanBasic(s, s.Active)) return;
                    Team other = MatchState.Other(s.Active);
                    if (CanBasic(s, other))
                    {
                        s.Active = other;
                        return;
                    }

                    StartLadder(ref s, log);
                    continue;

                case Phase.Ladder:
                case Phase.LastWord:
                    if (HasLegalAbility(s, s.Active)) return;
                    if (s.Phase == Phase.Ladder)
                    {
                        Log(log, EventKind.Phase, $"{s.Active} has no legal answer — the half ends, no Last Word.");
                    }

                    EndHalf(ref s, log);
                    continue;
            }
        }

        throw new InvalidOperationException("Normalize did not settle — a rules loop.");
    }

    private void StartRound(ref MatchState s, int round, List<GameEvent>? log)
    {
        s.Round = round;
        s.RoundOpener = round == 1 ? _roundOneOpener : MatchState.Other(s.RoundOpener);
        s.Half = 1;
        Log(log, EventKind.Phase, $"━━ Round {round} — {s.RoundOpener} opens ━━");
        StartHalf(ref s, log);
    }

    private void StartHalf(ref MatchState s, List<GameEvent>? log)
    {
        for (int i = 0; i < 10; i++)
        {
            s.Champions[i].Flags &= ~(ChampFlags.Acted | ChampFlags.BasicUsed);
        }

        s.BasicsTaken[0] = 0;
        s.BasicsTaken[1] = 0;
        s.Ceiling = 4;
        s.ResolutionsThisHalf = 0;
        s.Phase = Phase.Basic;
        s.Active = s.HalfOpener;
        Log(log, EventKind.Phase, $"Half {s.Half} — basics, {s.Active} first.");
    }

    private void StartLadder(ref MatchState s, List<GameEvent>? log)
    {
        s.Phase = Phase.Ladder;
        s.Ceiling = 4;
        s.Active = s.HalfOpener;
        Log(log, EventKind.Phase, $"Half {s.Half} — ladder, {s.Active} opens.");
    }

    private void EndHalf(ref MatchState s, List<GameEvent>? log)
    {
        if (s.Half == 1)
        {
            s.Half = 2;
            StartHalf(ref s, log);
            return;
        }

        CloseRound(ref s, log);
        if (s.Phase != Phase.MatchOver) StartRound(ref s, s.Round + 1, log);
    }

    private void EndMatch(ref MatchState s, Team winner, EndReason reason, List<GameEvent>? log)
    {
        s.Phase = Phase.MatchOver;
        s.Winner = winner;
        s.EndReason = reason;
        Log(log, EventKind.MatchOver, winner == Team.None
            ? $"Match drawn ({reason}) {s.Score[0]}–{s.Score[1]}."
            : $"{winner} wins by {reason}. Score {s.Score[0]}–{s.Score[1]}.");
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private static void Log(List<GameEvent>? log, EventKind kind, string text) => log?.Add(new GameEvent(kind, text));

    /// <summary>"A:Warden" style label.</summary>
    public string Name(in MatchState s, int slot)
    {
        Champion c = s.Champions[slot];
        return c.Def == 255 ? $"{c.Team}:{c.Role}" : $"{c.Team}:{Def(c).Name}";
    }

    /// <summary>Slot of the on-board champion at a hex, or −1.</summary>
    public static int ChampionAt(in MatchState s, HexCoord h)
    {
        for (int i = 0; i < 10; i++)
        {
            if (s.Champions[i].OnBoard && s.Champions[i].Pos == h) return i;
        }

        return -1;
    }

    private static bool Occupied(in MatchState s, HexCoord h) => ChampionAt(s, h) >= 0;

    private static int TeamIndex(Team t) => t == Team.A ? 0 : 1;
}
