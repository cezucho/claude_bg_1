using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>The initiative ladder (§5), chains (§5.2), targeting (§6) and effects (§7).</summary>
public sealed partial class Game
{
    // ───────────────────────────── targeting ─────────────────────────────

    /// <summary>Every legal target for an ability, ignoring the ceiling and readiness.</summary>
    public List<Target> AbilityTargets(in MatchState s, int slot, int ability)
    {
        var result = new List<Target>();
        Champion c = s.Champions[slot];
        AbilityDef a = Def(c).Abilities[ability];
        Team enemy = MatchState.Other(c.Team);

        if (a.IsFree)
        {
            int range = Range(c, a);
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
                        if (f.Team == c.Team && f.OnBoard && HexCoord.Distance(c.Pos, f.Pos) <= range) result.Add(Target.Champ(i));
                    }

                    break;

                case TargetRule.EmptyHex:
                    int dash = a.Effects[0].Amount;
                    foreach (HexCoord h in Board.HexesInFrame(c.Team))
                    {
                        int d = HexCoord.Distance(c.Pos, h);
                        if (d >= 1 && d <= dash && !Occupied(s, h)) result.Add(Target.At(h));
                    }

                    break;
            }

            return result;
        }

        if (a.Initiative == 3)
        {
            for (int k = 0; k < 6; k++)
            {
                int f = Board.FrameDirection(k, c.Team);   // facings listed in the team's frame
                if (PatternHasTarget(s, c.Team, PatternCells(c, a, f), a.DealsDamage)) result.Add(Target.Face(f));
            }
        }
        else if (PatternHasTarget(s, c.Team, PatternCells(c, a, 0), a.DealsDamage))
        {
            result.Add(Target.None);
        }

        return result;
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

    private bool PatternHasTarget(in MatchState s, Team team, HexCoord[] cells, bool damages)
    {
        Team enemy = MatchState.Other(team);
        foreach (HexCoord h in cells)
        {
            if (!Board.Playable(h)) continue;
            int who = ChampionAt(s, h);
            if (who >= 0 && s.Champions[who].Team == enemy) return true;
            if (!damages) continue;
            int tower = Board.TowerAt(h);
            if (tower >= 0 && s.Towers[tower].Owner != team) return true;
            if (Board.IsNexusHex(h, enemy) && NexusVulnerable(s, enemy)) return true;
        }

        return false;
    }

    /// <summary>Bitmask of this ability's active sigils (§5.2).</summary>
    public int ActiveSigils(in MatchState s, int slot, int ability)
    {
        Champion c = s.Champions[slot];
        AbilityDef a = Def(c).Abilities[ability];
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
        return c.OnBoard && !c.Has(ChampFlags.Acted) && c.Cooldowns[ability] == 0;
    }

    private bool HasLegalAbility(in MatchState s, Team team)
    {
        int first = MatchState.FirstSlot(team);
        for (int slot = first; slot < first + 5; slot++)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!Ready(s, slot, i)) continue;
                if (Def(s.Champions[slot]).Abilities[i].Initiative > s.Ceiling) continue;
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
                if (Def(s.Champions[slot]).Abilities[i].Initiative > s.Ceiling) continue;
                foreach (Target t in targets) into.Add(new Command(CommandKind.Ability, (byte)slot, (byte)i, t));
            }
        }

        foreach (var one in usable)
        {
            if (Def(s.Champions[one.Slot]).Abilities[one.Ability].Initiative > s.Ceiling) continue;
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
        int init1 = Def(s.Champions[cmd.Champion]).Abilities[cmd.Ability].Initiative;
        int ceiling = init1;

        if (cmd.IsChain)
        {
            AbilityDef second = Def(s.Champions[cmd.Champion2]).Abilities[cmd.Ability2];
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
        AbilityDef a = Def(caster).Abilities[ability];
        Team team = caster.Team;
        Team enemy = MatchState.Other(team);

        var champTargets = new List<int>();
        var structTargets = new List<Target>();
        HexCoord dashTo = default;

        if (a.IsFree)
        {
            switch (target.Kind)
            {
                case TargetKind.Champion: champTargets.Add(target.Index); break;
                case TargetKind.Tower:
                case TargetKind.Nexus: structTargets.Add(target); break;
                case TargetKind.Hex: dashTo = target.Hex; break;
            }
        }
        else
        {
            HexCoord[] cells = PatternCells(caster, a, a.Initiative == 3 ? target.Facing : 0);
            bool nexusHit = false;
            for (int i = 0; i < 10; i++)
            {
                if (s.Champions[i].Team == enemy && s.Champions[i].OnBoard && cells.Contains(s.Champions[i].Pos)) champTargets.Add(i);
            }

            foreach (HexCoord h in cells)
            {
                if (!Board.Playable(h)) continue;
                int tower = Board.TowerAt(h);
                if (tower >= 0 && s.Towers[tower].Owner != team) structTargets.Add(new Target(TargetKind.Tower, (byte)tower, h));
                if (!nexusHit && Board.IsNexusHex(h, enemy) && NexusVulnerable(s, enemy))
                {
                    nexusHit = true;
                    structTargets.Add(new Target(TargetKind.Nexus, (byte)enemy, h));
                }
            }
        }

        Log(log, EventKind.AbilityResolved, $"{Name(s, slot)} casts {a.Name} [init {a.Initiative}]" + TargetText(s, a, target));

        int pow = Pow(caster);
        foreach (EffectDef e in a.Effects)
        {
            switch (e.Kind)
            {
                case EffectKind.Damage:
                    int raw = (int)Arith.FloorDiv((long)Rules.AbilityBase * e.Power * pow, 1_000_000);
                    foreach (int t in champTargets)
                    {
                        DamageChampion(ref s, t, Math.Max(1, raw - Armour(s.Champions[t])), slot, fromPassive: false, log);
                    }

                    foreach (Target st in structTargets)
                    {
                        if (s.Phase == Phase.MatchOver) break;
                        DamageStructure(ref s, team, st, Math.Max(1, raw), log);
                    }

                    break;

                case EffectKind.Heal:
                    int heal = (int)Arith.FloorDiv((long)Rules.AbilityBase * e.Power * pow, 1_000_000);
                    foreach (int t in champTargets) Heal(ref s, t, heal, log);
                    break;

                case EffectKind.Shield:
                    foreach (int t in champTargets)
                    {
                        s.Champions[t].Shield += (short)e.Amount;
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
                    foreach (int t in champTargets) Displace(ref s, slot, t, e.Amount, log);
                    break;

                case EffectKind.Dash:
                    s.Champions[slot].Pos = dashTo;
                    Log(log, EventKind.Move, $"  {Name(s, slot)} dashes to {Fmt(dashTo)}");
                    break;
            }

            if (s.Phase == Phase.MatchOver) break;
        }

        // Molding strictly after the effect (ADR-0004): an ability never benefits from its own delta.
        Mold(ref s, slot, a.MoldUp, a.MoldUpDelta);
        Mold(ref s, slot, a.MoldDown, -a.MoldDownDelta);
        s.Champions[slot].Cooldowns[ability] = (byte)a.Cooldown;
        s.Champions[slot].Flags |= ChampFlags.Acted;
    }

    private string TargetText(in MatchState s, AbilityDef a, Target t) => t.Kind switch
    {
        TargetKind.Champion => $" → {Name(s, t.Index)}",
        TargetKind.Tower or TargetKind.Nexus => $" → {StructureName(t)}",
        TargetKind.Hex => $" → {Fmt(t.Hex)}",
        TargetKind.Facing => $" facing {t.Facing}",
        _ => a.Initiative == 4 ? " (fixed pattern)" : "",
    };

    // ───────────────────────────── effects ─────────────────────────────

    private void DamageChampion(ref MatchState s, int target, int amount, int source, bool fromPassive, List<GameEvent>? log)
    {
        ref Champion t = ref s.Champions[target];
        int absorbed = Math.Min(t.Shield, amount);
        t.Shield -= (short)absorbed;
        t.Hp -= amount - absorbed;
        Log(log, EventKind.Damage,
            $"  {Name(s, target)} takes {amount}{(absorbed > 0 ? $" ({absorbed} shielded)" : "")} → {t.Hp} HP");

        if (!fromPassive && source >= 0 && s.Champions[source].Team != t.Team)
        {
            FirePassive(ref s, target, PassiveTrigger.OnDamaged, source, log);
        }
    }

    private void Heal(ref MatchState s, int target, int amount, List<GameEvent>? log)
    {
        ref Champion t = ref s.Champions[target];
        int max = MaxHp(t);
        int before = t.Hp;
        t.Hp = Math.Min(max, t.Hp + amount);
        if (t.Hp > 0) t.Flags &= ~ChampFlags.Dying;   // healing above 0 clears Dying (ladder edge cases)
        Log(log, EventKind.Heal, $"  {Name(s, target)} heals {t.Hp - before} → {t.Hp} HP");
    }

    private void DamageStructure(ref MatchState s, Team attacker, Target st, int damage, List<GameEvent>? log)
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

        int scaled = Math.Max(1, (int)Arith.FloorDiv((long)damage * 1000, 1000 + Rules.DefenderWeight * defenders));

        if (st.Kind == TargetKind.Tower)
        {
            ref Tower tower = ref s.Towers[st.Index];
            tower.Hp -= (short)scaled;
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
        Log(log, EventKind.Structure, $"  NEXUS {(Team)idx} takes {scaled}{(defenders > 0 ? $" ({defenders} defending)" : "")} → {Math.Max(0, s.NexusHp[idx])}");
        if (s.NexusHp[idx] <= 0) EndMatch(ref s, attacker, EndReason.Nexus, log);
    }

    private void Displace(ref MatchState s, int caster, int target, int amount, List<GameEvent>? log)
    {
        HexCoord from = s.Champions[caster].Pos;
        bool push = amount > 0;
        int steps = Math.Abs(amount);
        HexCoord start = s.Champions[target].Pos;
        Team frame = s.Champions[caster].Team;

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
        }

        if (s.Champions[target].Pos != start)
        {
            Log(log, EventKind.Move, $"  {Name(s, target)} is {(push ? "pushed" : "pulled")} to {Fmt(s.Champions[target].Pos)}");
        }
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
                Heal(ref s, slot, p.Amount, log);
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
