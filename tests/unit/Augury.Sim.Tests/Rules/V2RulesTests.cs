using Augury.Sim.AI;
using Augury.Sim.Content;

namespace Augury.Sim.Tests.Rules;

/// <summary>
/// The shipped v2 game: three abilities plus a summoner spell, hidden spell picking, opening
/// casts that may fizzle, status verbs, walls, on Field 7 with solid towers
/// (<c>design/v2-champions.md</c>, <c>design/v2-direction.md</c>). Switches the process-wide
/// board, so it runs in the non-parallel "Board switching" collection and restores classic.
/// </summary>
[Collection("Board switching")]
public sealed class V2RulesTests : IDisposable
{
    // Content indices follow file order: 01 Anchor … 15 Seer.
    private const int Anchor = 0, Ember = 1, Lens = 2, Oriel = 3, Bulwark = 4, Viper = 5, Tempest = 6, Ranger = 7, Gunner = 8, Bastion = 9;
    private const int Briar = 10, Talon = 11, Pyre = 12, Mortar = 13, Seer = 14;

    // Picks per role: Top, Jungle, Mid, Bottom, Support.
    private static readonly int[] PicksA = [Anchor, Ember, Lens, Ranger, Oriel];
    private static readonly int[] PicksB = [Bulwark, Viper, Tempest, Gunner, Bastion];

    private readonly Game _game = Game.LoadDefault();

    public void Dispose() => Board.Use(BoardLayout.Classic);

    private MatchState Opening(Team active, int[]? a = null, int[]? b = null)
    {
        MatchState s = _game.NewMatch(a ?? PicksA, b ?? PicksB);
        Assert.Equal(Phase.Opening, s.Phase);
        s.Active = active;
        return s;
    }

    private static int AbilityIndex(ChampionDef d, Func<OpeningInstruction, bool> any) =>
        Enumerable.Range(0, 3).Single(i => d.Abilities[i].Opening.Any(any));

    [Fact]
    public void Roster_FifteenChampions_ThreePerRole_ThreeAbilitiesSignatureAndOneCastOpening()
    {
        Assert.Equal(15, _game.Content.Champions.Count);
        Assert.All(Enum.GetValues<Role>(), r => Assert.Equal(3, _game.Content.ForRole(r).Count()));
        Assert.Equal(8, _game.Content.Spells.Count);
        Assert.Equal("field7", _game.Rules.Board);
        Assert.True(_game.Rules.TowersBlock);
        foreach (ChampionDef d in _game.Content.Champions)
        {
            Assert.Equal(3, d.Abilities.Count);
            Assert.True(d.HasSpellSlot);
            Assert.NotNull(d.Signature);
            int casting = d.Abilities.Count(a => a.Opening.Any(i => i.Kind == InstructionKind.Cast))
                          + (d.Signature!.Opening.Any(i => i.Kind == InstructionKind.Cast) ? 1 : 0);
            Assert.Equal(1, casting);
        }
    }

    [Fact]
    public void EveryOpening_IsPlayableFromTheStartingLine()
    {
        MatchState a = Opening(Team.A);
        MatchState b = Opening(Team.B, PicksB, PicksA);
        foreach (MatchState s in new[] { a, b })
        {
            for (int slot = 0; slot < 10; slot++)
            {
                for (int ab = 0; ab < 4; ab++) Assert.True(_game.OpeningAvailable(s, slot, ab), $"{_game.Name(s, slot)} opening {ab}");
            }
        }
    }

    [Fact]
    public void Synergies_LoadAndScoreSharedGroupPairs()
    {
        ContentDb db = _game.Content;
        Assert.NotEmpty(db.Synergies);
        Assert.All(db.Champions.Select((_, i) => i), i => Assert.NotEmpty(db.GroupsOf(i)));   // nobody is left out

        // Lockdown: Anchor, Gunner, Seer → 3 pairs. Called Shot: Lens, Talon, Seer → 3 pairs.
        Assert.Equal(6, db.SynergyScore([Anchor, Talon, Lens, Gunner, Seer]));
        // Bulwark and Lens share Crush; Lens and Ranger share Called Shot.
        Assert.Equal(2, db.SynergyScore([Bulwark, Ember, Lens, Ranger, Oriel]));
        Assert.Equal(0, db.SynergyScore([255, 255]));
    }

    /// <summary>A ladder state at round 1: picks as given, champions on their start hexes.</summary>
    private MatchState Ladder(Team active, int[]? a = null, int[]? b = null)
    {
        MatchState s = Opening(active, a, b);
        s.Phase = Phase.Ladder;
        s.Round = 1;
        s.Half = 1;
        s.Ceiling = 4;
        s.Active = active;
        return s;
    }

    [Fact]
    public void Slam_RootedOrWalledTarget_TakesExtraDamage_UnstoppableDoesNot()
    {
        // B's Bulwark (slot 5) shoves A's Ember (slot 1), which has no on-damage passive.
        // Arrangements mutate a one-element array so the struct state is changed in place.
        MatchState Prepare(Action<MatchState[]> f) { var arr = new[] { Ladder(Team.B) }; f(arr); return arr[0]; }

        int Loss(Action<MatchState[]> arrange, out HexCoord pos, out List<GameEvent> log)
        {
            MatchState s = Prepare(arr =>
            {
                arr[0].Champions[5].Pos = new HexCoord(1, 0);
                arr[0].Champions[1].Pos = new HexCoord(1, -1);
                arrange(arr);
            });
            int before = s.Champions[1].Hp;
            log = new List<GameEvent>();
            _game.Apply(ref s, new Command(CommandKind.Ability, 5, 0, Target.Champ(1)), log);
            pos = s.Champions[1].Pos;
            return before - s.Champions[1].Hp;
        }

        int plain = Loss(arr => arr[0].Champions[1].UnstoppableHalves = 1, out HexCoord p0, out var l0);
        Assert.Equal(new HexCoord(1, -1), p0);
        Assert.DoesNotContain(l0, e => e.Text.Contains("SLAMS"));

        int rooted = Loss(arr => arr[0].Champions[1].RootHalves = 1, out HexCoord p1, out var l1);
        Assert.Equal(new HexCoord(1, -1), p1);
        Assert.Contains(l1, e => e.Text.Contains("SLAMS"));
        Assert.Equal(plain + 2, rooted);   // Shove's slam is 2

        int walled = Loss(arr =>
        {
            // Every hex that would take Ember further from Bulwark is a wall.
            arr[0].Walls[0] = new Wall { Pos = new HexCoord(1, -2), Rounds = 2 };
            arr[0].Walls[1] = new Wall { Pos = new HexCoord(2, -2), Rounds = 2 };
            arr[0].Walls[2] = new Wall { Pos = new HexCoord(0, -1), Rounds = 2 };
        }, out HexCoord p2, out var l2);
        Assert.Equal(new HexCoord(1, -1), p2);
        Assert.Equal(plain + 2, walled);
    }

    [Fact]
    public void BonusVsMarked_AddsOnTopOfTheMark()
    {
        // A's Ranger (slot 3) Volleys B's Viper (slot 6): +2 vs marked, plus the mark itself.
        int Loss(int mark)
        {
            MatchState s = Ladder(Team.A);
            s.Champions[6].Pos = new HexCoord(2, -1);
            s.Champions[6].Mark = (byte)mark;
            int before = s.Champions[6].Hp;
            _game.Apply(ref s, new Command(CommandKind.Ability, 3, 0, Target.Champ(6)));
            return before - s.Champions[6].Hp;
        }

        Assert.Equal(Loss(0) + 3 + 2, Loss(3));
    }

    [Fact]
    public void SynergyDrafter_BuildsMoreSynergyThanRandomDrafting()
    {
        int Total(Func<uint, IAgent> drafter)
        {
            int sum = 0;
            for (uint seed = 1; seed <= 20; seed++)
            {
                IAgent d = drafter(seed);
                var rng = new RandomPlayTests.Lcg(seed);
                MatchState s = _game.NewMatch();
                while (s.Phase == Phase.Draft)
                {
                    List<Command> legal = _game.Legal(s);
                    _game.Apply(ref s, s.Active == Team.A ? d.Choose(s, legal) : legal[rng.Below(legal.Count)]);
                }

                sum += _game.Content.SynergyScore(Enumerable.Range(0, 5).Select(i => (int)s.Champions[i].Def));
            }

            return sum;
        }

        Assert.True(Total(seed => new SynergyDrafter(_game, seed)) > Total(seed => new RandomAgent(seed)));
    }

    [Fact]
    public void SpellPick_FollowsTheDraft_NoTeamDuplicates_AndTheLogHidesTheChoice()
    {
        var rng = new RandomPlayTests.Lcg(7);
        MatchState s = _game.NewMatch();
        while (s.Phase == Phase.Draft)
        {
            List<Command> d = _game.Legal(s);
            _game.Apply(ref s, d[rng.Below(d.Count)]);
        }

        Assert.Equal(Phase.SpellPick, s.Phase);
        Team first = s.Active;
        List<Command> legal = _game.Legal(s);
        Assert.Equal(8, legal.Count);
        Assert.All(legal, c => Assert.Equal(CommandKind.SpellPick, c.Kind));

        var log = new List<GameEvent>();
        Command flash = legal.Single(c => c.Ability == 0);
        _game.Apply(ref s, flash, log);
        Assert.DoesNotContain(log, e => e.Text.Contains("Flash"));

        legal = _game.Legal(s);
        Assert.Equal(first, s.Active);
        Assert.Equal(7, legal.Count);
        Assert.DoesNotContain(legal, c => c.Ability == 0);

        while (s.Phase == Phase.SpellPick) _game.Apply(ref s, _game.Legal(s)[0], log);
        Assert.Equal(Phase.Opening, s.Phase);
        Assert.Contains(log, e => e.Text.Contains("spells revealed") && e.Text.Contains("Flash"));
        for (int t = 0; t < 2; t++)
        {
            var spells = Enumerable.Range(t * 5, 5).Select(i => (int)s.Champions[i].Spell).ToList();
            Assert.Equal(5, spells.Distinct().Count());
            Assert.All(spells, sp => Assert.InRange(sp, 0, 7));
        }
    }

    [Fact]
    public void OpeningCast_WithNothingInReach_FizzlesButGoesOnCooldown()
    {
        MatchState s = Opening(Team.A);
        int kindle = AbilityIndex(_game.Content.Champions[Ember], i => i.Kind == InstructionKind.Cast);
        int hpBefore = s.Champions[5].Hp + s.Champions[6].Hp + s.Champions[7].Hp + s.Champions[8].Hp + s.Champions[9].Hp;
        var log = new List<GameEvent>();

        _game.Apply(ref s, new Command(CommandKind.OpeningPlay, 1, (byte)kindle, Target.None), log);

        Assert.Contains(log, e => e.Text.Contains("fizzles"));
        Assert.True(s.Champions[1].Cooldowns[kindle] >= 1);
        for (int i = 5; i < 10; i++) Assert.False(s.Champions[i].Burning);
        Assert.Equal(hpBefore, s.Champions[5].Hp + s.Champions[6].Hp + s.Champions[7].Hp + s.Champions[8].Hp + s.Champions[9].Hp);
    }

    [Fact]
    public void OpeningCast_MayFireOtherChampionsAbilities_EachGoesOnCooldown()
    {
        // Tempest's Eye of the Storm: top casts Q, jungle casts Q, mid casts Q.
        MatchState s = Opening(Team.B);
        int eye = AbilityIndex(_game.Content.Champions[Tempest], i => i.Kind == InstructionKind.Cast);

        _game.Apply(ref s, new Command(CommandKind.OpeningPlay, 7, (byte)eye, Target.None));

        Assert.True(s.Champions[5].Cooldowns[0] >= 1, "top's Q");
        Assert.True(s.Champions[6].Cooldowns[0] >= 1, "jungle's Q");
        Assert.True(s.Champions[7].Cooldowns[0] >= 1, "mid's Q — cooldown 0 still costs round 1 (D-044)");
        Assert.Equal(0, s.Champions[7].Cooldowns[eye]);   // the opening's own ability is not spent
    }

    [Fact]
    public void OpeningCast_AlreadyOnCooldown_DoesNothing()
    {
        MatchState s = Opening(Team.A);
        int kindle = AbilityIndex(_game.Content.Champions[Ember], i => i.Kind == InstructionKind.Cast);
        s.Champions[1].Cooldowns[kindle] = 4;
        var log = new List<GameEvent>();

        _game.Apply(ref s, new Command(CommandKind.OpeningPlay, 1, (byte)kindle, Target.None), log);

        Assert.Equal(4, s.Champions[1].Cooldowns[kindle]);
        Assert.Contains(log, e => e.Text.Contains("already on cooldown"));
    }

    [Fact]
    public void OpeningDamage_CannotKill()
    {
        // Lens's Lance opening steps forward-left to (0,−2) and fires its line. An enemy at 1 HP
        // stands right in front of it.
        MatchState s = Opening(Team.A);
        int lance = AbilityIndex(_game.Content.Champions[Lens], i => i.Kind == InstructionKind.Cast);
        s.Champions[6].Pos = new HexCoord(0, -1);
        s.Champions[6].Hp = 1;
        var log = new List<GameEvent>();

        _game.Apply(ref s, new Command(CommandKind.OpeningPlay, 2, (byte)lance, Target.None), log);

        Assert.DoesNotContain(log, e => e.Text.Contains("fizzles"));
        Assert.Equal(1, s.Champions[6].Hp);
        Assert.True(s.Champions[2].Cooldowns[lance] >= 1);
    }

    [Fact]
    public void Rooted_ChampionsOpeningsThatMoveIt_AreUnavailable()
    {
        MatchState s = Opening(Team.A);
        s.Champions[0].RootHalves = 1;   // A's top
        ChampionDef anchor = _game.Content.Champions[Anchor];
        for (int ab = 0; ab < 4; ab++)
        {
            IReadOnlyList<OpeningInstruction> ins = _game.OpeningOf(s.Champions[0], ab);
            bool movesTop = ins.Any(i => i.Kind == InstructionKind.Move && i.Role == Role.Top);
            Assert.Equal(!movesTop, _game.OpeningAvailable(s, 0, ab));
        }

        Assert.NotNull(anchor.Signature);
    }

    [Fact]
    public void Wall_BlocksOpeningMoves_UntilItFalls()
    {
        MatchState s = Opening(Team.A);
        int hook = AbilityIndex(_game.Content.Champions[Anchor], i => i.Kind == InstructionKind.Cast);
        Assert.True(_game.OpeningAvailable(s, 0, hook));

        s.Walls[0] = new Wall { Pos = new HexCoord(-1, -2), Rounds = 1 };   // top's forward-right
        Assert.False(_game.OpeningAvailable(s, 0, hook));

        s.Walls[0].Rounds = 0;
        Assert.True(_game.OpeningAvailable(s, 0, hook));
    }

    [Fact]
    public void SolidTowers_NoChampionEverStandsOnATowerOrWall()
    {
        for (uint seed = 1; seed <= 25; seed++)
        {
            var rng = new RandomPlayTests.Lcg(seed);
            MatchState s = _game.NewMatch();
            var legal = new List<Command>();
            int n = 0;
            while (s.Phase != Phase.MatchOver)
            {
                legal.Clear();
                _game.Legal(s, legal);
                Assert.True(legal.Count > 0, $"no legal command in {s.Phase} (seed {seed})");
                _game.Apply(ref s, legal[rng.Below(legal.Count)]);
                Assert.True(++n < 20_000, $"match did not end (seed {seed})");
                for (int i = 0; i < 10; i++)
                {
                    if (!s.Champions[i].OnBoard) continue;
                    Assert.True(Board.Playable(s.Champions[i].Pos), $"off the board (seed {seed})");
                    Assert.True(Board.TowerAt(s.Champions[i].Pos) < 0, $"{_game.Name(s, i)} on a tower (seed {seed}, {s.Phase})");
                    Assert.True(Game.WallAt(s, s.Champions[i].Pos) < 0, $"{_game.Name(s, i)} in a wall (seed {seed})");
                }
            }

            Assert.NotEqual(EndReason.None, s.EndReason);
        }
    }

    [Fact]
    public void SameSeed_ProducesByteIdenticalFinalState()
    {
        static MatchState Play(Game g, uint seed)
        {
            var rng = new RandomPlayTests.Lcg(seed);
            MatchState s = g.NewMatch();
            while (s.Phase != Phase.MatchOver)
            {
                List<Command> legal = g.Legal(s);
                g.Apply(ref s, legal[rng.Below(legal.Count)]);
            }

            return s;
        }

        Assert.Equal(RandomPlayTests.Bytes(Play(_game, 99)), RandomPlayTests.Bytes(Play(_game, 99)));
    }
}
