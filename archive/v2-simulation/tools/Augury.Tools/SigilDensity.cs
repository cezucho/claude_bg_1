namespace Augury.Tools;

/// <summary>
/// Measures how often a team actually holds a usable sigil chain.
/// </summary>
/// <remarks>
/// <para>A chain requires two abilities sharing an <b>active</b> sigil on <b>two different
/// champions</b> — a champion acts once per half, so an ability can never chain into another
/// on the same champion. Printed sigils are live everywhere; slots are inert until a beacon
/// fills them, and a beacon carries exactly one sigil.</para>
/// <para>Teams here are drawn at random, which makes every number a <b>floor</b>. Real drafters
/// pick for synergy, so live play sits above these figures. The purpose is to find the
/// parameters at which a random team is neither starved nor swimming.</para>
/// </remarks>
public static class SigilDensity
{
    private const int Champions = 5;
    private const int AbilitiesPer = 4;
    private const int Trials = 200_000;

    /// <summary>Deterministic LCG — the harness must reproduce exactly (ADR-0002).</summary>
    private sealed class Rng(uint seed)
    {
        private uint _s = seed;
        public uint Next() => _s = _s * 1664525u + 1013904223u;
        public double Unit() => (Next() >> 8) / (double)(1 << 24);
        public int Below(int n) => (int)(Unit() * n);
    }

    private readonly record struct Result(
        double AnyNatural, double MeanNatural, double DeadDraft,
        double AnyWithBeacon, double MeanBeacon, double Saturated);

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("SIGIL CHAIN DENSITY — 5 champions x 4 abilities = 20 abilities per team");
        Console.WriteLine($"{Trials:N0} random teams per configuration. Random draft, so every");
        Console.WriteLine("figure is a FLOOR — drafting for synergy can only raise it.");
        Console.WriteLine();

        Console.WriteLine("  Unit is the CHAMPION DUO, not the ability pair: 5 champions make 10 duos,");
        Console.WriteLine("  and a duo counts if it holds any two abilities sharing an active sigil.");
        Console.WriteLine("  A half has room for at most two chains, so duo coverage is what binds.");
        Console.WriteLine();
        Console.WriteLine("  TYPED slots accept one named sigil; a beacon lights only matching slots.");
        Console.WriteLine("  WILD  slots accept whatever sigil the beacon carries.");
        Console.WriteLine();

        foreach (bool typed in new[] { true, false })
        {
            Console.WriteLine($"  ══ {(typed ? "TYPED" : "WILD")} SLOTS "
                              + new string('═', 58));

            foreach (int k in new[] { 3, 5 })
            {
                Console.WriteLine($"   {k} sigils          │  no beacon        │  one beacon       │");
                Console.WriteLine("   sigil%  slot%    │ can chain   duos  │ can chain   duos  │ dead draft");
                Console.WriteLine("  ───────────────────┼───────────────────┼───────────────────┼───────────");

                foreach (double pSig in new[] { 0.15, 0.25, 0.35 })
                {
                    foreach (double pSlot in new[] { 0.15, 0.25, 0.35 })
                    {
                        Result r = Measure(k, pSig, pSlot, typed);
                        Console.WriteLine($"    {pSig,4:P0}   {pSlot,4:P0}   │  {r.AnyNatural,6:P1}  {r.MeanNatural,5:F2}"
                                          + $"  │  {r.AnyWithBeacon,6:P1}  {r.MeanBeacon,5:F2}  │  {r.DeadDraft,5:P1}");
                    }
                }

                Console.WriteLine();
            }
        }

        Console.WriteLine("  READING IT");
        Console.WriteLine("    'has chain' too low  → the mechanic is invisible to that player all match.");
        Console.WriteLine("    'duos' near 10       → every pairing combos, so combos stop being moments");
        Console.WriteLine("                           and become the default line. That eats the ladder.");
        Console.WriteLine("    'dead draft'         → the player literally cannot combo. Should be near zero.");
        Console.WriteLine("    beacon lift          → how much a beacon MANUFACTURES. If small, beacons are");
        Console.WriteLine("                           decoration; if huge, printed sigils stop mattering.");
        Console.WriteLine();
    }

    private static Result Measure(int sigilCount, double pSigil, double pSlot, bool typedSlots)
    {
        var rng = new Rng(0xA06E5Fu);
        int anyNatural = 0, dead = 0, anyBeacon = 0, threePlus = 0;
        long totalNatural = 0, totalBeacon = 0;

        for (int t = 0; t < Trials; t++)
        {
            // -1 = nothing, 0..k-1 = printed sigil, -2 = empty slot.
            var kit = new int[Champions, AbilitiesPer];
            for (int c = 0; c < Champions; c++)
            {
                for (int a = 0; a < AbilitiesPer; a++)
                {
                    double roll = rng.Unit();
                    // >= 0 printed sigil · -1 nothing · typed slot encoded as -10 - sigil
                    kit[c, a] = roll < pSigil ? rng.Below(sigilCount)
                              : roll < pSigil + pSlot
                                  ? (typedSlots ? -10 - rng.Below(sigilCount) : -2)
                              : -1;
                }
            }

            int natural = CountDuos(kit, sigilCount, beaconSigil: -1);
            // One beacon, carrying whichever sigil serves this team best — the placing
            // team chooses it, so the best case is the honest read.
            int best = natural;
            for (int s = 0; s < sigilCount; s++)
            {
                best = Math.Max(best, CountDuos(kit, sigilCount, beaconSigil: s));
            }

            totalNatural += natural;
            totalBeacon += best;
            if (natural > 0) anyNatural++;
            if (best >= 6) threePlus++;
            if (best > 0) anyBeacon++;
            else dead++;
        }

        return new Result(
            (double)anyNatural / Trials, (double)totalNatural / Trials, (double)dead / Trials,
            (double)anyBeacon / Trials, (double)totalBeacon / Trials, (double)threePlus / Trials);
    }

    /// <summary>
    /// Counts how many of the ten champion duos hold a chain. A duo counts once however many
    /// ability pairs it holds, because a half has room to fire it at most once.
    /// </summary>
    private static int CountDuos(int[,] kit, int sigilCount, int beaconSigil)
    {
        // active[c, s] = how many of champion c's abilities currently carry sigil s.
        var active = new int[Champions, sigilCount];
        for (int c = 0; c < Champions; c++)
        {
            for (int a = 0; a < AbilitiesPer; a++)
            {
                int v = kit[c, a];
                if (v >= 0) active[c, v]++;
                else if (v == -2 && beaconSigil >= 0) active[c, beaconSigil]++;   // wild slot
                else if (v <= -10 && -10 - v == beaconSigil) active[c, beaconSigil]++;
            }
        }

        int duos = 0;
        for (int c1 = 0; c1 < Champions; c1++)
        {
            for (int c2 = c1 + 1; c2 < Champions; c2++)
            {
                for (int s = 0; s < sigilCount; s++)
                {
                    if (active[c1, s] > 0 && active[c2, s] > 0) { duos++; break; }
                }
            }
        }

        return duos;
    }
}
