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
        HashSet<Team> humans, bool humanTurn, bool canUndo, LastAction? last, string? error = null)
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
            race = new { nexusHp = g.Rules.NexusHp, killSiege = g.Rules.KillSiege, towerSiege = g.Rules.TowerSiege, roundCap = g.Rules.RoundCap },
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
            board = new
            {
                name = Board.Layout.Name,
                startA = Enum.GetValues<Role>().Select(r => Xy(Board.StartHex(Team.A, r))).ToArray(),
                towersBlock = g.Rules.TowersBlock,
            },
            walls = Enumerable.Range(0, 4).Where(w => s.Walls[w].Rounds > 0)
                .Select(w => new { q = s.Walls[w].Pos.Q, r = s.Walls[w].Pos.R, rounds = (int)s.Walls[w].Rounds }).ToArray(),
            spells = g.Content.Spells.Select((sp, i) => SpellView(g, sp, i)).ToArray(),
            synergies = g.Content.Synergies.Select(sy => new { id = sy.Id, name = sy.Name, idea = sy.Idea, members = sy.Members }).ToArray(),
            synergy = new[] { Team.A, Team.B }.Select(t => g.Content.SynergyScore(
                Enumerable.Range(MatchState.FirstSlot(t), 5).Select(i => (int)s.Champions[i].Def))).ToArray(),
            spawns = new[] { Team.A, Team.B }.SelectMany(t => Enum.GetValues<Role>().Select(r =>
            {
                HexCoord h = Board.SpawnHex(t, r);
                return new { q = h.Q, r = h.R, team = t.ToString(), role = r.ToString() };
            })).ToArray(),
            winner = s.Phase == Phase.MatchOver ? s.Winner.ToString() : null,
            endReason = s.Phase == Phase.MatchOver ? s.EndReason.ToString() : null,
            champions = Enumerable.Range(0, 10).Select(i => ChampionView(g, s, i, ladder, singles, chainParts, humans)).ToArray(),
            roster = s.Phase == Phase.Draft ? g.Content.Champions.Select((d, i) => DefView(g, d, i)).ToArray() : null,
            legal = legal.Select((c, i) => LegalView(g, s, c, i)).ToArray(),
            last = last is null ? null : LastView(g, last, s, log),
            events = log.Skip(start).Select((e, k) => new { n = start + k, kind = e.Kind.ToString(), text = e.Text }).ToArray(),
        };
    }

    // ───────────────────────────── champions ─────────────────────────────

    /// <summary>
    /// Spells are chosen hidden (owner, v2): until the opening, a team's spells are shown only
    /// to that team's own player, and in hotseat only while that team is choosing.
    /// </summary>
    private static bool SpellVisible(MatchState s, Team team, HashSet<Team> humans) =>
        s.Phase is not (Phase.Draft or Phase.SpellPick) || (humans.Contains(team) && (humans.Count == 1 || s.Active == team));

    private static object ChampionView(Game g, MatchState s, int slot, bool ladder,
        HashSet<(int, int)> singles, HashSet<(int, int)> chainParts, HashSet<Team> humans)
    {
        Champion c = s.Champions[slot];
        if (c.Def == 255)
        {
            return new { slot, team = c.Team.ToString(), role = c.Role.ToString(), drafted = false };
        }

        ChampionDef d = g.Def(c);
        bool placed = s.Phase is not (Phase.Draft or Phase.SpellPick);
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
            status = Statuses(c),
            respawnIn = c.Presence == Presence.Dead && placed ? Math.Max(0, c.RespawnIn - 1) : 0,
            passive = new { name = d.Passive.Name, text = Describe.Passive(d.Passive) },
            abilities = Enumerable.Range(0, 4).Select(a => AbilityView(g, s, slot, a, ladder, singles, chainParts, SpellVisible(s, c.Team, humans))).ToArray(),
        };
    }

    private static object AbilityView(Game g, MatchState s, int slot, int a, bool ladder,
        HashSet<(int, int)> singles, HashSet<(int, int)> chainParts, bool spellVisible)
    {
        Champion c = s.Champions[slot];
        bool spellSlot = g.Def(c).HasSpellSlot && a == 3;
        AbilityDef? maybe = spellSlot && !spellVisible ? null : g.Kit(c, a);
        IReadOnlyList<OpeningInstruction> opening = g.OpeningOf(c, a);
        if (maybe is null)
        {
            // An empty or hidden summoner slot: only its (champion-owned) opening is known.
            string why = c.Spell == 255 ? "summoner spell not chosen yet" : "summoner spell hidden until the opening";
            return new
            {
                key = Keys[a],
                name = c.Spell == 255 ? "Spell" : "Hidden",
                spell = true,
                hidden = true,
                openingName = g.OpeningName(c, a),
                init = 0,
                cooldown = 0,
                cd = (int)c.Cooldowns[a],
                targeting = why,
                effects = "",
                amount = 0,
                printedSigil = (string?)null,
                slotSigil = (string?)null,
                activeSigils = Array.Empty<string>(),
                mold = "",
                opening = opening.Select(EffectText.Instruction).ToArray(),
                kit = Kit(null, 0, opening),
                friendly = false,
                state = s.Phase == Phase.Opening ? State(g, s, slot, a, null, ladder, singles, chainParts, false).Item1 : "unavailable",
                reason = why,
                reach = Array.Empty<int[]>(),
                targets = Array.Empty<int[]>(),
            };
        }

        AbilityDef ab = maybe;
        bool onBoard = c.OnBoard && s.Phase != Phase.Draft;

        // Where it can reach, and what it could hit right now ignoring the ceiling and turn.
        var reach = new List<HexCoord>();
        var targets = new List<HexCoord>();
        if (onBoard)
        {
            if (ab.Target == TargetRule.Self)
            {
                reach.Add(c.Pos);
            }
            else if (ab.Target == TargetRule.TeleportHex)
            {
                // Anywhere beside a friendly tower or beacon: the targets are the reach.
            }
            else if (ab.IsFree)
            {
                int range = g.AbilityRange(c, ab);
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
        EffectDef? scaled = ab.Effects.FirstOrDefault(e => e.Kind is EffectKind.Damage or EffectKind.Heal && e.Power > 0);
        int raw = scaled is not null && s.Phase != Phase.Draft
            ? (int)Arith.FloorDiv((long)g.Rules.AbilityBase * scaled.Power * g.Pow(c), 1_000_000)
            : 0;

        return new
        {
            key = Keys[a],
            name = ab.Name,
            spell = ab.IsSpell,
            hidden = false,
            openingName = g.OpeningName(c, a),
            init = ab.Initiative,
            cooldown = ab.Cooldown,
            cd = (int)c.Cooldowns[a],
            targeting = Describe.Targeting(ab),
            effects = Describe.Effects(ab),
            amount = raw,
            printedSigil = ab.PrintedSigil >= 0 ? Game.SigilName(ab.PrintedSigil) : null,
            slotSigil = ab.SlotSigil >= 0 ? Game.SigilName(ab.SlotSigil) : null,
            activeSigils = onBoard ? SigilList(g.ActiveSigils(s, slot, a)) : [],
            mold = ab.IsSpell ? "summoner spells don't mold" : Describe.Mold(ab),
            opening = opening.Select(EffectText.Instruction).ToArray(),
            kit = Kit(ab, s.Phase == Phase.Draft ? DraftRange(g.Def(c), ab) : g.AbilityRange(c, ab), opening),
            friendly = Friendly(ab),
            state,
            reason,
            reach = reach.Distinct().Select(Xy).ToArray(),
            targets = targets.Distinct().Select(Xy).ToArray(),
        };
    }

    private static (string, string) State(Game g, MatchState s, int slot, int a, AbilityDef? ab, bool ladder,
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
        if (s.Phase is Phase.Draft or Phase.SpellPick || ab is null) return ("idle", "");
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
    private static object DefView(Game g, ChampionDef d, int index) => new
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
        line = d.Line,
        groups = g.Content.GroupsOf(index).Select(sy => sy.Id).ToArray(),
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
            opening = ab.Opening.Select(EffectText.Instruction).ToArray(),
            casts = ab.Opening.Any(i => i.Kind == InstructionKind.Cast),
            kit = Kit(ab, DraftRange(d, ab), ab.Opening),
        }).ToArray(),
        signature = d.Signature is null ? null : new
        {
            key = Keys[3],
            name = d.Signature.Name,
            opening = d.Signature.Opening.Select(EffectText.Instruction).ToArray(),
            casts = d.Signature.Opening.Any(i => i.Kind == InstructionKind.Cast),
            kit = Kit(null, 0, d.Signature.Opening),
        },
    };

    /// <summary>A summoner spell in the shared pool.</summary>
    private static object SpellView(Game g, AbilityDef sp, int index) => new
    {
        index,
        name = sp.Name,
        init = sp.Initiative,
        cooldown = sp.Cooldown,
        targeting = Describe.Targeting(sp),
        effects = Describe.Effects(sp),
        kit = Kit(sp, sp.FixedRange > 0 ? sp.FixedRange : sp.Effects.FirstOrDefault(e => e.Kind == EffectKind.Dash)?.Amount ?? 1, []),
    };

    /// <summary>An ability's range before the match: fixed, a dash's length, or base reach plus bonus.</summary>
    private static int DraftRange(ChampionDef d, AbilityDef ab)
    {
        if (ab.FixedRange > 0) return ab.FixedRange;
        EffectDef? move = ab.Effects.FirstOrDefault(e => e.Kind == EffectKind.Dash);
        if (move is not null && ab.Target == TargetRule.EmptyHex) return move.Amount;
        return Math.Clamp(d.Base(Stat.Rch) / 1000 + ab.RangeBonus, 1, 3);
    }

    /// <summary>True when the ability helps its own side rather than hitting the enemy.</summary>
    private static bool Friendly(AbilityDef ab) => ab.Target is TargetRule.Ally or TargetRule.Self or TargetRule.EmptyHex or TargetRule.TeleportHex;

    /// <summary>The v2 statuses a champion carries, as short labels with their detail.</summary>
    private static object[] Statuses(in Champion c)
    {
        var list = new List<object>();
        void Add(string kind, string label, string text) => list.Add(new { kind, label, text });
        if (c.Rooted) Add("root", "rooted", $"Can't move, dash or be moved ({c.RootHalves} half{(c.RootHalves == 1 ? "" : "s")} left).");
        if (c.Burning) Add("burn", $"burn {c.BurnAmount}", $"Takes {c.BurnAmount} each time it uses an ability or basic attack ({c.BurnRounds} round{(c.BurnRounds == 1 ? "" : "s")} left). Shields don't block it.");
        if (c.Mark > 0) Add("mark", $"marked +{c.Mark}", $"The next hit it takes, from anyone, deals +{c.Mark}.");
        if (c.ExhaustHalves > 0) Add("exhaust", "exhausted", $"Deals half damage ({c.ExhaustHalves} half{(c.ExhaustHalves == 1 ? "" : "s")} left).");
        if (c.Unstoppable) Add("unstop", "unstoppable", "Immune to root, push and pull.");
        if (c.WoundRounds > 0) Add("wound", "wounded", $"Healing on it is halved ({c.WoundRounds} round{(c.WoundRounds == 1 ? "" : "s")} left).");
        return list.ToArray();
    }

    /// <summary>
    /// The shape of an ability as data, for the page's small diagrams: pattern offsets in
    /// the canonical frame (team A, forward = +R), effects, and the three opening steps.
    /// </summary>
    private static object Kit(AbilityDef? ab, int range, IReadOnlyList<OpeningInstruction> opening) => new
    {
        tier = ab?.Initiative ?? 0,
        target = ab?.Target.ToString() ?? "None",
        range,
        pattern = ab is null ? [] : ab.Pattern.Select(Xy).ToArray(),
        fx = ab is null ? [] : ab.Effects.Select(e => new { kind = e.Kind.ToString(), amount = e.Amount, power = e.Power }).ToArray(),
        steps = opening.Select(i => new
        {
            kind = i.Kind switch { InstructionKind.Move => "move", InstructionKind.Cast => "cast", _ => "beacon" },
            role = i.Role.ToString(),
            dir = i.Direction,
            sigil = Game.SigilName(i.Sigil),
            slot = i.Slot >= 0 ? Keys[i.Slot] : null,
        }).ToArray(),
    };

    // ───────────────────────────── legal commands ─────────────────────────────

    /// <summary>
    /// The action just taken, for the page to mark on the board: who acted, what the
    /// command covered (judged on the board it was played on), and what changed.
    /// </summary>
    private static object LastView(Game g, LastAction last, MatchState now, List<GameEvent> log)
    {
        Command c = last.Command;
        MatchState before = last.Before;
        bool hasActor = c.Champion != 255 && c.Kind != CommandKind.Draft;
        return new
        {
            id = last.Id,
            team = before.Active.ToString(),
            kind = c.Kind.ToString(),
            champ = hasActor ? c.Champion : -1,
            ability = c.Kind is CommandKind.Ability or CommandKind.OpeningPlay ? c.Ability : -1,
            spell = -1,
            champ2 = c.IsChain ? c.Champion2 : -1,
            ability2 = c.IsChain ? c.Ability2 : -1,
            actorAt = hasActor ? Xy(before.Champions[c.Champion].Pos) : null,
            actor2At = c.IsChain ? Xy(before.Champions[c.Champion2].Pos) : null,
            label = Label(g, before, c),
            cells = CommandCells(g, before, c, out _).Where(Board.Playable).Distinct().Select(Xy).ToArray(),
            focus = FocusHexes(before, c),
            siege = SiegeLines(before, now),
            diff = Diff(g, before, now, log.Skip(last.LogStart).ToList()),
        };
    }

    /// <summary>
    /// When the action closed a round, one line per held tower to the enemy nexus it fired
    /// at, so the siege is something seen on the board rather than a number changing.
    /// </summary>
    private static object[] SiegeLines(MatchState before, MatchState now)
    {
        bool closed = now.Round != before.Round || (now.Phase == Phase.MatchOver && now.EndReason is EndReason.Siege or EndReason.RoundCap);
        if (!closed) return [];
        var lines = new List<object>();
        for (int t = 0; t < 5; t++)
        {
            Team owner = now.Towers[t].Owner;
            if (owner == Team.None) continue;
            lines.Add(new { team = owner.ToString(), from = Xy(now.Towers[t].Pos), to = Xy(Board.NexusHexes(MatchState.Other(owner)).ElementAt(1)) });
        }

        return lines.ToArray();
    }

    /// <summary>Single-target hexes, for a line from the actor to what it hit.</summary>
    private static int[][] FocusHexes(MatchState s, Command c)
    {
        var list = new List<int[]>();
        void Add(Target t)
        {
            switch (t.Kind)
            {
                case TargetKind.Champion: list.Add(Xy(s.Champions[t.Index].Pos)); break;
                case TargetKind.Tower: list.Add(Xy(s.Towers[t.Index].Pos)); break;
                case TargetKind.Beacon: list.Add(Xy(t.Hex)); break;
                case TargetKind.Nexus: list.Add(Xy(Board.NexusHexes((Team)t.Index).ElementAt(1))); break;
            }
        }

        if (c.Kind is CommandKind.Ability or CommandKind.BasicAttack) Add(c.Target);
        return list.ToArray();
    }

    private static List<HexCoord> CommandCells(Game g, MatchState s, Command c, out List<HexCoord> click)
    {
        var cells = new List<HexCoord>();
        click = new List<HexCoord>();
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

        return cells;
    }

    private static object LegalView(Game g, MatchState s, Command c, int index)
    {
        List<HexCoord> cells = CommandCells(g, s, c, out List<HexCoord> click);
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
            spell = c.Kind == CommandKind.SpellPick ? c.Ability : -1,
            preview = c.Kind is CommandKind.Draft or CommandKind.SpellPick ? null : Preview(g, s, c),
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
        AbilityDef? a = g.Kit(c, ability);
        if (a is null || a.IsFree) return [];
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
        AbilityDef a = g.Kit(c, ability)!;
        int[] lean = Enumerable.Range(0, 6).Select(f => Lean(a, c.Team, f)).ToArray();
        int dir = lean.Distinct().Count() == 6 ? lean[t.Facing] : t.Facing;
        return [c.Pos + Hex.Directions[dir]];
    }

    private static int Lean(AbilityDef a, Team team, int facing)
    {
        // Presentation only: floating point is fine outside Augury.Sim.
        double x = 0, y = 0;
        foreach (HexCoord off in a.Pattern)
        {
            HexCoord r = Hex.Rotate(Board.Frame(off, team), facing);
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
        string Ab(int slot, int a) => g.Kit(s.Champions[slot], a)?.Name ?? "?";
        return c.Kind switch
        {
            CommandKind.Draft => $"Draft {g.Content.Champions[c.Ability].Name} ({(Role)(c.Champion % 5)})",
            CommandKind.OpeningPlay => $"{g.Name(s, c.Champion)} opens with {g.OpeningName(s.Champions[c.Champion], c.Ability)}",
            // Never names the spell: the label is also shown to the other side as the last action.
            CommandKind.SpellPick => $"{g.Name(s, c.Champion)} chooses a summoner spell",
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
        return Diff(g, before, after, log);
    }

    /// <summary>What changed between two states: HP, shields, positions, structures, score.</summary>
    private static object Diff(Game g, MatchState before, MatchState after, List<GameEvent> log)
    {
        var champs = new List<object>();
        for (int i = 0; i < 10; i++)
        {
            Champion a = before.Champions[i], b = after.Champions[i];
            bool moved = a.Pos != b.Pos || a.Presence != b.Presence;
            string[] gained = StatusLabels(b).Except(StatusLabels(a)).ToArray();
            if (a.Hp == b.Hp && a.Shield == b.Shield && !moved && a.PoisonRounds == b.PoisonRounds && a.Flags == b.Flags && gained.Length == 0) continue;
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
                status = gained,
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

        var walls = Enumerable.Range(0, 4)
            .Where(w => after.Walls[w].Rounds > 0 && (before.Walls[w].Rounds == 0 || before.Walls[w].Pos != after.Walls[w].Pos))
            .Select(w => Xy(after.Walls[w].Pos)).ToArray();

        return new
        {
            walls,
            events = log.Take(14).Select(e => e.Text.Trim()).ToArray(),
            more = Math.Max(0, log.Count - 14),
            champs,
            towers,
            beacons,
            nexus = new[] { after.NexusHp[0] - before.NexusHp[0], after.NexusHp[1] - before.NexusHp[1] },
            endsHalf = after.Round != before.Round || after.Half != before.Half || after.Phase != before.Phase && after.Phase is Phase.Basic,
            endsMatch = after.Phase == Phase.MatchOver,
        };
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private static IEnumerable<string> StatusLabels(Champion c)
    {
        if (c.Rooted) yield return "rooted";
        if (c.Burning) yield return "burning";
        if (c.Mark > 0) yield return "marked";
        if (c.ExhaustHalves > 0) yield return "exhausted";
        if (c.Unstoppable) yield return "unstoppable";
        if (c.WoundRounds > 0) yield return "wounded";
    }

    private static int[] Xy(HexCoord h) => [h.Q, h.R];

    private static string Zone(HexCoord h)
    {
        if (Board.Layout.Jungle.Contains(h)) return "jungle";
        return Board.Layout.Lanes.Contains(h) ? "lane" : "open";
    }
}
