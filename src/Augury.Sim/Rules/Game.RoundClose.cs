using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>Round close (§13), death and respawn (§14), winning (§15).</summary>
public sealed partial class Game
{
    /// <summary>
    /// Strict order, asserted by test (ADR-0006): death check, status phase, siege,
    /// upkeep, win check. Reordering the first two deletes the dying round.
    /// </summary>
    /// <remarks>
    /// v2 (<c>design/v2-direction.md</c>): there are no points. A death damages the dead
    /// champion's own nexus, and every held tower fires at the enemy nexus. A nexus at zero
    /// after the siege ends the match.
    /// </remarks>
    private void CloseRound(ref MatchState s, List<GameEvent>? log)
    {
        // 1. Death check.
        Log(log, EventKind.DeathCheck, $"Round {s.Round} closes — death check.");
        Span<bool> died = stackalloc bool[10];
        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            if (!c.OnBoard || c.Hp > 0) continue;

            died[i] = true;
            int respawn = Rules.RespawnBase + s.Round / Math.Max(1, Rules.RespawnEvery);
            c.Presence = Presence.Dead;
            c.RespawnIn = (byte)(respawn + 1);   // this round's upkeep decrements once
            c.Flags &= ~ChampFlags.Dying;
            c.PoisonAmount = 0;
            c.PoisonRounds = 0;
            c.Shield = 0;
            s.NexusHp[TeamIndex(c.Team)] -= Rules.KillSiege;
            Log(log, EventKind.Death, $"  ✝ {Name(s, i)} dies (respawn in {respawn}). NEXUS {c.Team} −{Rules.KillSiege} → {Math.Max(0, s.NexusHp[TeamIndex(c.Team)])}");
        }

        for (int i = 0; i < 10; i++)
        {
            if (!died[i]) continue;
            for (int j = 0; j < 10; j++)
            {
                if (j != i && s.Champions[j].Team == s.Champions[i].Team && s.Champions[j].OnBoard)
                {
                    FirePassive(ref s, j, PassiveTrigger.OnAllyDies, i, log);
                }
            }
        }

        // 2. Status phase: poison, then tower shots. ≤0 here means Dying, not dead.
        Log(log, EventKind.StatusPhase, "  Status phase.");
        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            if (!c.OnBoard || c.PoisonRounds == 0) continue;
            c.Hp -= c.PoisonAmount;
            c.PoisonRounds--;
            Log(log, EventKind.Damage, $"  {Name(s, i)} suffers {c.PoisonAmount} poison → {c.Hp} HP");
            if (c.PoisonRounds == 0) c.PoisonAmount = 0;
        }

        for (int t = 0; t < 5; t++)
        {
            Tower tower = s.Towers[t];
            if (tower.Owner == Team.None) continue;
            for (int i = 0; i < 10; i++)
            {
                ref Champion c = ref s.Champions[i];
                if (c.OnBoard && c.Team != tower.Owner && HexCoord.Distance(c.Pos, tower.Pos) <= 1)
                {
                    c.Hp -= Rules.TowerShot;
                    Log(log, EventKind.Damage, $"  tower {Fmt(tower.Pos)} shoots {Name(s, i)} for {Rules.TowerShot} → {c.Hp} HP");
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            if (c.OnBoard && c.Hp <= 0 && !c.Has(ChampFlags.Dying))
            {
                c.Flags |= ChampFlags.Dying;
                Log(log, EventKind.Dying, $"  ☠ {Name(s, i)} is DYING — one more round, half power");
            }
        }

        // 3. Siege: every held tower fires at the enemy nexus, whether or not its gate is open.
        for (int k = 0; k < 2; k++)
        {
            Team t = k == 0 ? Team.A : Team.B;
            int towers = TowersOwned(s, t);
            if (towers == 0) continue;
            Team enemy = MatchState.Other(t);
            s.NexusHp[TeamIndex(enemy)] -= towers * Rules.TowerSiege;
            Log(log, EventKind.Score, $"  Siege: {t}'s {towers} tower{(towers == 1 ? "" : "s")} fire at NEXUS {enemy} −{towers * Rules.TowerSiege} → {Math.Max(0, s.NexusHp[TeamIndex(enemy)])}");
        }

        // 4. Upkeep.
        for (int i = 0; i < 10; i++)
        {
            ref Champion c = ref s.Champions[i];
            for (int a = 0; a < 4; a++)
            {
                if (c.Cooldowns[a] > 0) c.Cooldowns[a]--;
            }

            c.Shield = 0;
            if (c.Presence == Presence.Dead && --c.RespawnIn == 0)
            {
                c.Presence = Presence.InSpawn;
                c.Pos = Board.SpawnHex(c.Team, c.Role);
                c.Hp = MaxHp(c);
                for (int a = 0; a < 4; a++) c.Cooldowns[a] = 0;
                Log(log, EventKind.Respawn, $"  {Name(s, i)} respawns in its spawn hex.");
            }
        }

        for (int i = 0; i < 10; i++) FirePassive(ref s, i, PassiveTrigger.OnRoundClose, -1, log);

        // 5. Win check. If both fall together, the nexus with more left stands.
        int a0 = s.NexusHp[0], b0 = s.NexusHp[1];
        Team leader = a0 == b0 ? Team.None : a0 > b0 ? Team.A : Team.B;
        if (a0 <= 0 || b0 <= 0)
        {
            EndMatch(ref s, leader, EndReason.Siege, log);
        }
        else if (s.Round >= Rules.RoundCap)
        {
            EndMatch(ref s, leader, EndReason.RoundCap, log);
        }
    }

    /// <summary>Towers a team owns.</summary>
    public static int TowersOwned(in MatchState s, Team t)
    {
        int n = 0;
        for (int i = 0; i < 5; i++)
        {
            if (s.Towers[i].Owner == t) n++;
        }

        return n;
    }
}
