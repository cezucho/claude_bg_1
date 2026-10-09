using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>The initiative ladder (§5), chains (§5.2), targeting (§6) and effects (§7).</summary>
public sealed partial class Game
{
    // ───────────────────────────── targeting ─────────────────────────────

    /// <summary>Every legal target for the ability in a combat slot, ignoring the ceiling and readiness.</summary>
    public List<Target> AbilityTargets(in MatchState s, int slot, int ability)
    {
        var result = new List<Target>();
        Champion c = s.Champions[slot];
        AbilityDef? a = Kit(c, ability);
        if (a is null) return result;
        if (a.MovesCaster && c.Rooted) return result;   // a root stops dashes, swaps and teleports
        Team enemy = MatchState.Other(c.Team);

        if (a.IsFree)
        {
            int range = AbilityRange(c, a);
            bool movesAlly = a.Effects.Any(e => e.Kind is EffectKind.Swap or EffectKind.PullAlly);
            switch (a.Target)
            {
                case TargetRule.Enemy:
                    for (int i = 0; i < 10; i++)
                    {
                        Champion e = s.Champions[i];
                        if (e.Team == enemy && e.OnBoard && HexCoord.Distance(c.Pos, e.Pos) <= range) result.Add(Target.Champ(i));
                    }

                    if (a.DealsDamage) AddStructureTargets(s, c.Team, c.Pos, range, result);
                    break;

                case TargetRule.Ally:
                    for (int i = 0; i < 10; i++)
                    {
                        Champion f = s.Champions[i];
                        if (f.Team != c.Team || !f.OnBoard || HexCoord.Distance(c.Pos, f.Pos) > range) continue;
                        if (movesAlly && (i == slot || f.Rooted)) continue;   // swap and pull need another, movable ally
                        result.Add(Target.Champ(i));
                    }

                    break;

                case TargetRule.Self:
                    if (c.OnBoard) result.Add(Target.Champ(slot));
                    break;

                case TargetRule.EmptyHex:
                    foreach (HexCoord h in Board.HexesInFrame(c.Team))
                    {
                        int d = HexCoord.Distance(c.Pos, h);
                        if (d >= 1 && d <= range && !Occupied(s, h) && Board.TowerAt(h) < 0) result.Add(Target.At(h));
                    }

                    break;

                case TargetRule.Structure:
                    AddStructureTargets(s, c.Team, c.Pos, range, result);
                    break;

                case TargetRule.TeleportHex:
                    foreach (HexCoord h in Board.HexesInFrame(c.Team))
                    {
                        if (Occupied(s, h) || Board.TowerAt(h) >= 0) continue;
                        if (NextToFriendlyAnchor(s, c.Team, h)) result.Add(Target.At(h));
                    }

                    break;
            }

            return result;
        }

        bool allies = a.Target == TargetRule.Ally;
        if (a.Initiative == 3)
        {
            for (int k = 0; k < 6; k++)
            {
                int f = Board.FrameDirection(k, c.Team);   // facings listed in the team's frame
                if (PatternHasTarget(s, c.Team, PatternCells(c, a, f), a.DealsDamage, allies)) result.Add(Target.Face(f));
            }
        }
        else if (PatternHasTarget(s, c.Team, PatternCells(c, a, 0), a.DealsDamage, allies))
        {
            result.Add(Target.None);
        }

        return result;
    }

    /// <summary>Free-aim reach: a spell's fixed range, a dash's distance, or the caster's reach plus the ability's bonus.</summary>
    public int AbilityRange(in Champion c, AbilityDef a)
    {
        if (a.FixedRange > 0) return a.FixedRange;
        EffectDef? dash = a.Effects.FirstOrDefault(e => e.Kind == EffectKind.Dash);
        return dash is not null ? dash.Amount : Range(c, a);
    }

    private bool NextToFriendlyAnchor(in MatchState s, Team team, HexCoord h)
    {
        for (int t = 0; t < 5; t++)
        {
            if (s.Towers[t].Owner == team && HexCoord.Distance(s.Towers[t].Pos, h) == 1) return true;
        }

        for (int b = 0; b < 12; b++)
        {
            if (s.Beacons[b].Team == team && HexCoord.Distance(s.Beacons[b].Pos, h) <= 1) return true;
        }

        return false;
    }

    /// <summary>
    /// Board hexes a pattern covers. Every pattern is first put in the caster's team frame
    /// (mirrored for team B); tier 3 then rotates to <paramref name="facing"/>, tier 4 does
    /// not (ADR-0005, second amendment).
    /// </summary>
    public HexCoord[] PatternCells(in Champion c, AbilityDef a, int facing)
    {
        var cells = new HexCoord[a.Pattern.Count];
        for (int i = 0; i < cells.Length; i++)
        {
            HexCoord framed = Board.Frame(a.Pattern[i], c.Team);
            HexCoord off = a.Initiative == 3 ? Hex.Rotate(framed, facing) : framed;
            cells[i] = c.Pos + off;
        }

        return cells;
    }

    private bool PatternHasTarget(in MatchState s, Team team, HexCoord[] cells, bool damages, bool allies = false)
    {
        Team enemy = MatchState.Other(team);
        foreach (HexCoord h in cells)
        {
            if (!Board.Playable(h)) continue;
            int who = ChampionAt(s, h);
            if (allies)
            {
                if (who >= 0 && s.Champions[who].Team == team) return true;
                continue;
            }

            if (who >= 0 && s.Champions[who].Team == enemy) return true;
            if (!damages) continue;
            int tower = Board.TowerAt(h);
            if (tower >= 0 && s.Towers[tower].Owner != team) return true;
            if (Board.IsNexusHex(h, enemy) && NexusVulnerable(s, enemy)) return true;
        }

        return false;
    }

    /// <summary>
    /// How an opening cast aims itself (D-043): enemy abilities at the nearest enemy champion
    /// (ties to lowest HP); ally abilities at an ally with a status to cleanse, else the most
    /// wounded; rotatable patterns toward the most enemies; dashes and walls at the empty hex
    /// nearest the closest enemy. Never at structures. Null when there is nothing to hit.
    /// </summary>
    public Target? OpeningAim(in MatchState s, int who, int slot)
    {
        Champion c = s.Champions[who];
        AbilityDef? a = Kit(c, slot);
        if (a is null || a.IsSpell) return null;
        List<Target> targets = AbilityTargets(s, who, slot);
        Team enemy = MatchState.Other(c.Team);
        Target? best = null;
        int bestKey = int.MaxValue;

        if (a.IsFree)
        {
            foreach (Target t in targets)
            {
                int key;
                switch (a.Target)
                {
                    case TargetRule.Enemy:
                        if (t.Kind != TargetKind.Champion) continue;
                        key = HexCoord.Distance(c.Pos, s.Champions[t.Index].Pos) * 1000 + Math.Max(0, s.Champions[t.Index].Hp);
                        break;
                    case TargetRule.Ally:
                        Champion f = s.Champions[t.Index];
                        key = (HasStatus(f) ? 0 : 100_000) - (MaxHp(f) - f.Hp) * 100 + HexCoord.Distance(c.Pos, f.Pos);
                        break;
                    case TargetRule.Self:
                        key = 0;
                        break;
                    case TargetRule.EmptyHex:
                        key = NearestEnemyDistance(s, enemy, t.Hex);
                        break;
                    default:
                        continue;
                }

                if (key < bestKey)
                {
                    bestKey = key;
                    best = t;
                }
            }

            return best;
        }

        bool allies = a.Target == TargetRule.Ally;
        int bestCount = 0;
        foreach (Target t in targets)
        {
            int count = 0;
            foreach (HexCoord h in PatternCells(c, a, t.Kind == TargetKind.Facing ? t.Facing : 0))
            {
                int w = ChampionAt(s, h);
                if (w >= 0 && (allies ? s.Champions[w].Team == c.Team : s.Champions[w].Team == enemy)) count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                best = t;
            }
        }

        return best;
    }

    private static int NearestEnemyDistance(in MatchState s, Team enemy, HexCoord h)
    {
        int nearest = 99;
        for (int i = 0; i < 10; i++)
        {
            if (s.Champions[i].Team == enemy && s.Champions[i].OnBoard) nearest = Math.Min(nearest, HexCoord.Distance(h, s.Champions[i].Pos));
        }

        return nearest;
    }

    private static bool HasStatus(in Champion c) =>
        c.Rooted || c.Burning || c.PoisonRounds > 0 || c.Mark > 0 || c.ExhaustHalves > 0 || c.WoundRounds > 0;

    /// <summary>Bitmask of this ability's active sigils (§5.2).</summary>
    public int ActiveSigils(in MatchState s, int slot, int ability)
    {
        Champion c = s.Champions[slot];
        AbilityDef? a = Kit(c, ability);
        if (a is null) return 0;
        int mask = a.PrintedSigil >= 0 ? 1 << a.PrintedSigil : 0;
        if (a.SlotSigil >= 0)
        {
            for (int b = 0; b < 12; b++)
            {
                Beacon beacon = s.Beacons[b];
                if (beacon.Team == c.Team && beacon.Sigil == a.SlotSigil && HexCoord.Distance(beacon.Pos, c.Pos) <= 1)
                {
                    mask |= 1 << a.SlotSigil;
                    break;
                }
            }
        }

        return mask;
    }

    private bool Ready(in MatchState s, int slot, int ability)
    {
        Champion c = s.Champions[slot];
        AbilityDef? a = Kit(c, ability);
        if (a is null || c.Has(ChampFlags.Acted) || c.Cooldowns[ability] > 0) return false;
        if (c.OnBoard) return true;
        // Teleport is the one thing a champion can do from its spawn hex (v2 spell).
        return c.Presence == Presence.InSpawn && a.Effects.Any(e => e.Kind == EffectKind.Teleport);
    }

    private bool HasLegalAbility(in MatchState s, Team team)
    {
        int first = MatchState.FirstSlot(team);
        for (int slot = first; slot < first + 5; slot++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!Ready(s, slot, i)) continue;
                if (Kit(s.Champions[slot], i)!.Initiative > s.Ceiling) continue;
                if (AbilityTargets(s, slot, i).Count > 0) return true;
            }
        }

        return false;
    }

    /// <summary>Single abilities and chains legal for a team at the current ceiling.</summary>
    private void EnumerateAbilities(in MatchState s, Team team, List<Command> into)
    {
        int first = MatchState.FirstSlot(team);
        var usable = new List<(int Slot, int Ability, List<Target> Targets)>();
        for (int slot = first; slot < first + 5; slot++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!Ready(s, slot, i)) continue;
                List<Target> targets = AbilityTargets(s, slot, i);
                if (targets.Count == 0) continue;
                usable.Add((slot, i, targets));
                if (Kit(s.Champions[slot], i)!.Initiative > s.Ceiling) continue;
                foreach (Target t in targets) into.Add(new Command(CommandKind.Ability, (byte)slot, (byte)i, t));
            }
        }

        foreach (var one in usable)
        {
            if (Kit(s.Champions[one.Slot], one.Ability)!.Initiative > s.Ceiling) continue;
            int sig1 = ActiveSigils(s, one.Slot, one.Ability);
            if (sig1 == 0) continue;
            foreach (var two in usable)
            {
                if (two.Slot == one.Slot) continue;
                if ((ActiveSigils(s, two.Slot, two.Ability) & sig1) == 0) continue;
                foreach (Target t1 in one.Targets)
                {
                    foreach (Target t2 in two.Targets)
                    {
                        into.Add(new Command(CommandKind.Ability, (byte)one.Slot, (byte)one.Ability, t1,
                            (byte)two.Slot, (byte)two.Ability, t2));
                    }
                }
            }
        }
    }

    // ───────────────────────────── resolution ─────────────────────────────

    private void ApplyAbility(ref MatchState s, in Command cmd, List<GameEvent>? log)
    {
        bool lastWord = s.Phase == Phase.LastWord;
        Team team = s.Champions[cmd.Champion].Team;
        int init1 = Kit(s.Champions[cmd.Champion], cmd.Ability)!.Initiative;
        int ceiling = init1;

        if (cmd.IsChain)
        {
            AbilityDef second = Kit(s.Champions[cmd.Champion2], cmd.Ability2)!;
            Log(log, EventKind.AbilityResolved,
                $"⛓ CHAIN: {Name(s, cmd.Champion)} + {Name(s, cmd.Champion2)} — one step, no answer between");
            ceiling = Math.Max(init1, second.Initiative);
        }

        ResolveOne(ref s, cmd.Champion, cmd.Ability, cmd.Target, log);
        s.ResolutionsThisHalf++;
        if (cmd.IsChain && s.Phase != Phase.MatchOver)
        {
            ResolveOne(ref s, cmd.Champion2, cmd.Ability2, cmd.Target2, log);
            s.ResolutionsThisHalf++;
        }

        if (s.Phase == Phase.MatchOver) return;
        s.Ceiling = (byte)ceiling;

        if (lastWord)
        {
            EndHalf(ref s, log);
            return;
        }

        s.Active = MatchState.Other(team);
    }

    private void ApplyPass(ref MatchState s, List<GameEvent>? log)
    {
        Log(log, EventKind.Pass, $"{s.Active} passes — {MatchState.Other(s.Active)} has the Last Word at ≤{s.Ceiling}.");
        s.Phase = Phase.LastWord;
        s.Active = MatchState.Other(s.Active);
    }

    private void ApplyDecline(ref MatchState s, List<GameEvent>? log)
    {
        Log(log, EventKind.Decline, $"{s.Active} declines the Last Word.");
        EndHalf(ref s, log);
    }

    private void ResolveOne(ref MatchState s, int slot, int ability, Target target, List<GameEvent>? log)
    {
        Champion caster = s.Champions[slot];
        AbilityDef a = Kit(caster, ability)!;
        Team team = caster.Team;
        Team enemy = MatchState.Other(team);

        var champTargets = new List<int>();
        var structTargets = new List<Target>();
        HexCoord hexTarget = default;

        if (a.IsFree)
        {
            switch (target.Kind)
            {
                case TargetKind.Champion: champTargets.Add(target.Index); break;
                case TargetKind.Tower:
                case TargetKind.Nexus: structTargets.Add(target); break;
                case TargetKind.Hex: hexTarget = target.Hex; break;
            }
        }
        else
        {
            HexCoord[] cells = PatternCells(caster, a, a.Initiative == 3 ? target.Facing : 0);
            bool allies = a.Target == TargetRule.Ally;
            bool nexusHit = false;
            for (int i = 0; i < 10; i++)
            {
                Champion w = s.Champions[i];
                if (w.OnBoard && cells.Contains(w.Pos) && w.Team == (allies ? team : enemy)) champTargets.Add(i);
            }

            foreach (HexCoord h in cells)
            {
                if (allies || !Board.Playable(h)) continue;
                int tower = Board.TowerAt(h);
                if (tower >= 0 && s.Towers[tower].Owner != team) structTargets.Add(new Target(TargetKind.Tower, (byte)tower, h));
                if (!nexusHit && Board.IsNexusHex(h, enemy) && NexusVulnerable(s, enemy))
                {
                    nexusHit = true;
                    structTargets.Add(new Target(TargetKind.Nexus, (byte)enemy, h));
                }
            }
        }

        Log(log, EventKind.AbilityResolved, $"{Name(s, slot)} {(s.Phase == Phase.Opening ? "casts (opening)" : "casts")} {a.Name} [init {a.Initiative}]" + TargetText(s, a, target));
        BurnOnAct(ref s, slot, log);

        int pow = Pow(caster);
        foreach (EffectDef e in a.Effects)
        {
            switch (e.Kind)
            {
                case EffectKind.Damage:
                    int raw = e.Power > 0 ? (int)Arith.FloorDiv((long)Rules.AbilityBase * e.Power * pow, 1_000_000) : e.Amount;
                    if (s.Champions[slot].ExhaustHalves > 0) raw = Math.Max(1, raw / 2);
                    foreach (int t in champTargets)
                    {
                        if (e.BonusVs != StatusKind.None && BonusApplies(s.Champions[t], e.BonusVs))
                        {
                            Log(log, EventKind.Damage, $"  PAYOFF: {a.Name} on {Name(s, t)}, who is {e.BonusVs.ToString().ToLowerInvariant()}");
                            _observer?.Payoff(slot, t);
                        }

                        int hit = HitDamage(s, t, raw, e);
                        DamageChampion(ref s, t, hit + WantedExtra(s, slot, t, hit, log), slot, fromPassive: false, log);
                    }

                    foreach (Target st in structTargets)
                    {
                        if (s.Phase == Phase.MatchOver) break;
                        DamageStructure(ref s, team, st, Math.Max(1, raw), log, source: slot);
                    }

                    break;

                case EffectKind.Heal:
                    int heal = e.Power > 0 ? (int)Arith.FloorDiv((long)Rules.AbilityBase * e.Power * pow, 1_000_000) : e.Amount;
                    foreach (int t in champTargets) Heal(ref s, t, heal, log, slot);
                    break;

                case EffectKind.Shield:
                    foreach (int t in champTargets)
                    {
                        s.Champions[t].Shield += (short)e.Amount;
                        _observer?.Shielded(slot, t, e.Amount);
                        Log(log, EventKind.Heal, $"  {Name(s, t)} gains {e.Amount} shield");
                    }

                    break;

                case EffectKind.Poison:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        if (e.Amount >= v.PoisonAmount)
                        {
                            v.PoisonAmount = (byte)e.Amount;
                            v.PoisonRounds = (byte)e.Rounds;
                            Log(log, EventKind.Damage, $"  {Name(s, t)} is poisoned ({e.Amount}/round for {e.Rounds})");
                        }
                    }

                    break;

                case EffectKind.Displace:
                    foreach (int t in champTargets)
                    {
                        bool stopped = Displace(ref s, slot, t, e.Amount, log);
                        if (stopped && e.Slam > 0 && e.Amount > 0 && s.Champions[t].OnBoard)
                        {
                            Log(log, EventKind.Damage, $"  {Name(s, t)} SLAMS into something");
                            _observer?.Slam(slot, t);
                            DamageChampion(ref s, t, Math.Max(1, e.Slam * Rules.PayoffScale / 1000), slot, fromPassive: false, log);
                        }
                    }

                    break;

                case EffectKind.Dash:
                case EffectKind.Teleport:
                    ref Champion mover = ref s.Champions[slot];
                    bool fromSpawn = mover.Presence == Presence.InSpawn;
                    mover.Pos = hexTarget;
                    mover.Presence = Presence.OnBoard;
                    Log(log, EventKind.Move, $"  {Name(s, slot)} {(e.Kind == EffectKind.Teleport ? (fromSpawn ? "teleports from spawn" : "teleports") : "dashes")} to {Fmt(hexTarget)}");
                    break;

                case EffectKind.Root:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        if (v.Unstoppable)
                        {
                            Log(log, EventKind.Move, $"  {Name(s, t)} is unstoppable — the root fails");
                            continue;
                        }

                        v.RootHalves = (byte)Math.Max(v.RootHalves, e.Amount);
                        Log(log, EventKind.Move, $"  {Name(s, t)} is ROOTED");
                    }

                    break;

                case EffectKind.Burn:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        v.BurnAmount = (byte)Math.Max(v.BurnAmount, e.Amount);
                        v.BurnRounds = (byte)Math.Max(v.BurnRounds, e.Rounds);
                        Log(log, EventKind.Damage, $"  {Name(s, t)} is BURNING ({e.Amount} each time it acts, {e.Rounds} rounds)");
                    }

                    break;

                case EffectKind.Mark:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        v.Mark = (byte)Math.Max(v.Mark, e.Amount);
                        Log(log, EventKind.Damage, $"  {Name(s, t)} is MARKED (+{e.Amount} on the next hit)");
                    }

                    break;

                case EffectKind.Wall:
                    PlaceWall(ref s, hexTarget, e.Amount, log);
                    break;

                case EffectKind.Swap:
                    foreach (int t in champTargets)
                    {
                        if (s.Champions[t].Rooted || s.Champions[slot].Rooted) continue;
                        (s.Champions[slot].Pos, s.Champions[t].Pos) = (s.Champions[t].Pos, s.Champions[slot].Pos);
                        Log(log, EventKind.Move, $"  {Name(s, slot)} and {Name(s, t)} swap places");
                    }

                    break;

                case EffectKind.PullAlly:
                    foreach (int t in champTargets) PullToward(ref s, slot, t, e.Amount, log);
                    break;

                case EffectKind.Unstoppable:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        v.UnstoppableHalves = (byte)Math.Max(v.UnstoppableHalves, e.Amount);
                        v.RootHalves = 0;
                        Log(log, EventKind.Heal, $"  {Name(s, t)} is UNSTOPPABLE");
                    }

                    break;

                case EffectKind.Cleanse:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        bool any = HasStatus(v);
                        v.RootHalves = 0;
                        v.BurnRounds = 0;
                        v.BurnAmount = 0;
                        v.PoisonRounds = 0;
                        v.PoisonAmount = 0;
                        v.Mark = 0;
                        v.ExhaustHalves = 0;
                        v.WoundRounds = 0;
                        if (any) Log(log, EventKind.Heal, $"  {Name(s, t)} is cleansed");
                    }

                    break;

                case EffectKind.Exhaust:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        v.ExhaustHalves = (byte)Math.Max(v.ExhaustHalves, e.Amount);
                        Log(log, EventKind.Damage, $"  {Name(s, t)} is EXHAUSTED (half damage)");
                    }

                    break;

                case EffectKind.Wound:
                    foreach (int t in champTargets)
                    {
                        ref Champion v = ref s.Champions[t];
                        v.WoundRounds = (byte)Math.Max(v.WoundRounds, e.Rounds);
                        Log(log, EventKind.Damage, $"  {Name(s, t)}'s healing is halved");
                    }

                    break;

                case EffectKind.StructureDamage:
                    foreach (Target st in structTargets)
                    {
                        if (s.Phase == Phase.MatchOver) break;
                        DamageStructure(ref s, team, st, e.Amount, log, ignoreDefenders: true, source: slot);
                    }

                    break;

                case EffectKind.HealWoundedAlly:
                    int pick = -1, missing = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        Champion f = s.Champions[i];
                        if (i == slot || f.Team != team || !f.OnBoard || HexCoord.Distance(f.Pos, s.Champions[slot].Pos) > e.Rounds) continue;
                        int m = MaxHp(f) - f.Hp;
                        if (m > missing)
                        {
                            missing = m;
                            pick = i;
                        }
                    }

                    if (pick >= 0) Heal(ref s, pick, e.Amount, log, slot);
                    break;
            }

            if (s.Phase == Phase.MatchOver) break;
        }

        // Molding strictly after the effect (ADR-0004): an ability never benefits from its own delta.
        if (!a.IsSpell)
        {
            Mold(ref s, slot, a.MoldUp, a.MoldUpDelta);
            Mold(ref s, slot, a.MoldDown, -a.MoldDownDelta);
        }

        s.Champions[slot].Cooldowns[ability] = (byte)a.Cooldown;
        s.Champions[slot].Flags |= ChampFlags.Acted;
    }

    /// <summary>Damage of one hit after armour and the effect's status bonus (v2).</summary>
    private int HitDamage(in MatchState s, int target, int raw, EffectDef e)
    {
        Champion t = s.Champions[target];
        int dmg = Math.Max(1, raw - Armour(t));
        if (BonusApplies(t, e.BonusVs))
        {
            if (e.BonusPermille > 0)
            {
                // Multiplier, with the flat amount as a floor so cheap hits still pay off. Only the
                // extra is scaled by PayoffScale.
                int extra = Math.Max(dmg * (e.BonusPermille - 1000) / 1000, e.BonusFlat);
                dmg += extra * Rules.PayoffScale / 1000;
            }
            else
            {
                if (e.BonusDouble) dmg *= 2;
                dmg += e.BonusFlat;
            }
        }

        return dmg;
    }

    /// <summary>A burning champion takes its burn each time it acts (v2): through shields, never killing in the opening.</summary>
    /// <summary>
    /// Extra damage when the attacker wants a status the target carries (D-056): the synergy
    /// payoff, on every hit — abilities and basic attacks.
    /// </summary>
    private int WantedExtra(in MatchState s, int source, int target, int hit, List<GameEvent>? log)
    {
        StatusKind wants = Def(s.Champions[source]).Wants;
        if (wants == StatusKind.None || !BonusApplies(s.Champions[target], wants)) return 0;
        int extra = Math.Max(hit * (Rules.WantBonus - 1000) / 1000, Rules.WantFloor) * Rules.PayoffScale / 1000;
        if (extra <= 0) return 0;
        Log(log, EventKind.Damage, $"  PAYOFF: {Name(s, source)} on {Name(s, target)}, who is {wants.ToString().ToLowerInvariant()} (+{extra})");
        _observer?.Payoff(source, target);
        return extra;
    }

    private static bool BonusApplies(in Champion t, StatusKind k) => k switch
    {
        StatusKind.Rooted => t.Rooted,
        StatusKind.Burning => t.Burning,
        StatusKind.Poisoned => t.PoisonRounds > 0,
        StatusKind.Marked => t.Mark > 0,
        StatusKind.Exhausted => t.ExhaustHalves > 0,
        _ => false,
    };

    private void BurnOnAct(ref MatchState s, int slot, List<GameEvent>? log)
    {
        ref Champion c = ref s.Champions[slot];
        if (c.BurnRounds == 0 || c.BurnAmount == 0) return;
        int loss = c.BurnAmount;
        if (s.Phase == Phase.Opening) loss = Math.Min(loss, Math.Max(0, c.Hp - 1));
        c.Hp -= loss;
        _observer?.ChampionDamaged(-1, slot, loss, 0);
        Log(log, EventKind.Damage, $"  {Name(s, slot)} burns for {loss} → {c.Hp} HP");
    }

    private void PlaceWall(ref MatchState s, HexCoord at, int rounds, List<GameEvent>? log)
    {
        int slot = 0;
        for (int w = 0; w < 4; w++)
        {
            if (s.Walls[w].Rounds == 0)
            {
                slot = w;
                break;
            }

            if (s.Walls[w].Rounds < s.Walls[slot].Rounds) slot = w;   // full: the oldest falls
        }

        s.Walls[slot] = new Wall { Pos = at, Rounds = (byte)Math.Max(1, rounds) };
        Log(log, EventKind.Move, $"  a WALL rises at {Fmt(at)}");
    }

    private void PullToward(ref MatchState s, int caster, int target, int steps, List<GameEvent>? log)
    {
        if (s.Champions[target].Rooted) return;
        HexCoord to = s.Champions[caster].Pos;
        HexCoord start = s.Champions[target].Pos;
        Team frame = s.Champions[caster].Team;
        for (int step = 0; step < steps; step++)
        {
            HexCoord at = s.Champions[target].Pos;
            int current = HexCoord.Distance(at, to);
            if (current <= 1) break;
            HexCoord best = at;
            for (int k = 0; k < 6; k++)
            {
                HexCoord n = at + Hex.Directions[Board.FrameDirection(k, frame)];
                if (!Board.Playable(n) || Occupied(s, n)) continue;
                if (HexCoord.Distance(n, to) < current)
                {
                    best = n;
                    break;
                }
            }

            if (best == at) break;
            s.Champions[target].Pos = best;
        }

        if (s.Champions[target].Pos != start) Log(log, EventKind.Move, $"  {Name(s, target)} is pulled to {Fmt(s.Champions[target].Pos)}");
    }

    private string TargetText(in MatchState s, AbilityDef a, Target t) => t.Kind switch
    {
        TargetKind.Champion => $" → {Name(s, t.Index)}",
        TargetKind.Tower or TargetKind.Nexus => $" → {StructureName(t)}",
        TargetKind.Hex => $" → {Fmt(t.Hex)}",
        TargetKind.Facing => $" facing {t.Facing}",
        _ => a.Initiative == 4 && !a.IsFree ? " (fixed pattern)" : "",
    };

    // ───────────────────────────── effects ─────────────────────────────

    private void DamageChampion(ref MatchState s, int target, int amount, int source, bool fromPassive, List<GameEvent>? log)
    {
        ref Champion t = ref s.Champions[target];
        string marked = "";
        if (!fromPassive && t.Mark > 0)
        {
            amount += t.Mark;   // a mark adds to the next hit from anyone, then is spent (v2)
            marked = $" (+{t.Mark} marked)";
            t.Mark = 0;
        }

        int absorbed = Math.Min(t.Shield, amount);
        t.Shield -= (short)absorbed;
        int loss = amount - absorbed;
        if (s.Phase == Phase.Opening) loss = Math.Min(loss, Math.Max(0, t.Hp - 1));   // opening damage can't kill (D-042)
        t.Hp -= loss;
        _observer?.ChampionDamaged(source, target, loss, absorbed);
        Log(log, EventKind.Damage,
            $"  {Name(s, target)} takes {amount}{marked}{(absorbed > 0 ? $" ({absorbed} shielded)" : "")} → {t.Hp} HP");

        if (!fromPassive && source >= 0 && s.Champions[source].Team != t.Team)
        {
            FirePassive(ref s, target, PassiveTrigger.OnDamaged, source, log);
        }
    }

    private void Heal(ref MatchState s, int target, int amount, List<GameEvent>? log, int source = -1)
    {
        ref Champion t = ref s.Champions[target];
        if (t.WoundRounds > 0) amount /= 2;   // wounded: healing halved (v2)
        int max = MaxHp(t);
        int before = t.Hp;
        t.Hp = Math.Min(max, t.Hp + amount);
        if (t.Hp > 0) t.Flags &= ~ChampFlags.Dying;   // healing above 0 clears Dying (ladder edge cases)
        if (t.Hp > before) _observer?.ChampionHealed(source, target, t.Hp - before);
        Log(log, EventKind.Heal, $"  {Name(s, target)} heals {t.Hp - before} → {t.Hp} HP");
    }

    private void DamageStructure(ref MatchState s, Team attacker, Target st, int damage, List<GameEvent>? log, bool ignoreDefenders = false, int source = -1)
    {
        Team defending = MatchState.Other(attacker);
        IEnumerable<HexCoord> hexes = st.Kind == TargetKind.Nexus
            ? Board.NexusHexes((Team)st.Index)
            : [Board.TowerHexes[st.Index]];

        int defenders = 0;
        for (int i = 0; i < 10; i++)
        {
            Champion c = s.Champions[i];
            if (c.Team == defending && c.OnBoard && hexes.Any(h => HexCoord.Distance(h, c.Pos) <= 1)) defenders++;
        }

        if (ignoreDefenders) defenders = 0;
        int scaled = Math.Max(1, (int)Arith.FloorDiv((long)damage * 1000, 1000 + Rules.DefenderWeight * defenders));

        if (st.Kind == TargetKind.Tower)
        {
            ref Tower tower = ref s.Towers[st.Index];
            tower.Hp -= (short)scaled;
            _observer?.StructureDamaged(source, scaled, nexus: false);
            Log(log, EventKind.Structure, $"  {StructureName(st)} takes {scaled}{(defenders > 0 ? $" ({defenders} defending)" : "")} → {Math.Max(0, (int)tower.Hp)}");
            if (tower.Hp <= 0)
            {
                tower.Owner = attacker;
                tower.Hp = (short)Rules.TowerHp;
                Log(log, EventKind.Structure, $"  ⚑ {attacker} CAPTURES {StructureName(st)}");
            }

            return;
        }

        int idx = st.Index;
        s.NexusHp[idx] -= scaled;
        _observer?.StructureDamaged(source, scaled, nexus: true);
        Log(log, EventKind.Structure, $"  NEXUS {(Team)idx} takes {scaled}{(defenders > 0 ? $" ({defenders} defending)" : "")} → {Math.Max(0, s.NexusHp[idx])}");
        if (s.NexusHp[idx] <= 0) EndMatch(ref s, attacker, EndReason.Nexus, log);
    }

    /// <returns>True when the target moved fewer hexes than asked: blocked, at the edge, or rooted
    /// (it "slams"). Unstoppable targets ignore the push entirely and never slam.</returns>
    private bool Displace(ref MatchState s, int caster, int target, int amount, List<GameEvent>? log)
    {
        if (s.Champions[target].Unstoppable)
        {
            Log(log, EventKind.Move, $"  {Name(s, target)} is unstoppable and holds its ground");
            return false;
        }

        if (s.Champions[target].Rooted)
        {
            Log(log, EventKind.Move, $"  {Name(s, target)} is rooted and holds its ground");
            return true;
        }

        HexCoord from = s.Champions[caster].Pos;
        bool push = amount > 0;
        int steps = Math.Abs(amount);
        HexCoord start = s.Champions[target].Pos;
        Team frame = s.Champions[caster].Team;
        int moved = 0;

        for (int step = 0; step < steps; step++)
        {
            HexCoord at = s.Champions[target].Pos;
            int current = HexCoord.Distance(from, at);
            HexCoord best = at;
            int bestDist = current;
            for (int k = 0; k < 6; k++)
            {
                // Ties break by direction order, in the caster's frame, so a push lands the
                // same way for both teams (ADR-0005, second amendment).
                HexCoord to = at + Hex.Directions[Board.FrameDirection(k, frame)];
                if (!Board.Playable(to) || Occupied(s, to)) continue;
                int dist = HexCoord.Distance(from, to);
                if (push ? dist > bestDist : dist < bestDist && dist >= 1)
                {
                    best = to;
                    bestDist = dist;
                }
            }

            if (best == at) break;   // truncated at the last free hex (schema edge case 4)
            s.Champions[target].Pos = best;
            moved++;
        }

        if (s.Champions[target].Pos != start)
        {
            Log(log, EventKind.Move, $"  {Name(s, target)} is {(push ? "pushed" : "pulled")} to {Fmt(s.Champions[target].Pos)}");
        }

        return moved < steps;
    }

    private void Mold(ref MatchState s, int slot, Stat stat, int delta)
    {
        ref Champion c = ref s.Champions[slot];
        int upper = stat == Stat.Rch ? 1000 : 2000;
        c.Drift[(int)stat] = (short)Math.Clamp(c.Drift[(int)stat] + delta, -1000, upper);
        if (stat == Stat.Vit) c.Hp = Math.Min(c.Hp, MaxHp(c));
    }

    private void FirePassive(ref MatchState s, int slot, PassiveTrigger trigger, int source, List<GameEvent>? log)
    {
        Champion c = s.Champions[slot];
        PassiveDef p = Def(c).Passive;
        if (p.Trigger != trigger || !c.OnBoard) return;

        switch (p.Effect)
        {
            case PassiveEffect.Retaliate:
                if (source < 0 || !s.Champions[source].OnBoard || HexCoord.Distance(c.Pos, s.Champions[source].Pos) > 1) return;
                Log(log, EventKind.Passive, $"  ✦ {Name(s, slot)}: {p.Name}");
                DamageChampion(ref s, source, p.Amount, slot, fromPassive: true, log);
                break;

            case PassiveEffect.Strike:
                if (source < 0 || !s.Champions[source].OnBoard) return;
                Log(log, EventKind.Passive, $"  ✦ {Name(s, slot)}: {p.Name}");
                DamageChampion(ref s, source, p.Amount, slot, fromPassive: true, log);
                break;

            case PassiveEffect.HealSelf:
                Log(log, EventKind.Passive, $"  ✦ {Name(s, slot)}: {p.Name}");
                Heal(ref s, slot, p.Amount, log, slot);
                break;

            case PassiveEffect.ShieldSelf:
                s.Champions[slot].Shield += (short)p.Amount;
                Log(log, EventKind.Passive, $"  ✦ {Name(s, slot)}: {p.Name} (+{p.Amount} shield)");
                break;

            case PassiveEffect.EmpowerSelf:
                Mold(ref s, slot, p.Stat, p.Amount);
                Log(log, EventKind.Passive, $"  ✦ {Name(s, slot)}: {p.Name} (+{p.Amount} {p.Stat})");
                break;
        }
    }
}
