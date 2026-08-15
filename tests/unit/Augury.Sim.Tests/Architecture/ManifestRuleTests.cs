using System.Reflection;
using System.Runtime.CompilerServices;

namespace Augury.Sim.Tests.Architecture;

/// <summary>
/// Executable enforcement of the rules marked 🤖 in
/// <c>docs/architecture/control-manifest.md</c>.
/// </summary>
/// <remarks>
/// A rule a machine checks is worth more than a rule a human remembers. These
/// four rules are the ones that silently destroy determinism, headless testing
/// or the dying round if they lapse — so none of them is left to code review.
/// </remarks>
public class ManifestRuleTests
{
    private static readonly Assembly Sim = typeof(Arith).Assembly;

    /// <summary>ADR-0001: the simulation does not know Godot exists.</summary>
    [Fact]
    public void Sim_ReferencesNoGodotAssembly()
    {
        string[] offenders = Sim.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.Contains("Godot", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"ADR-0001 breached: Augury.Sim references {string.Join(", ", offenders)}. " +
            "The simulation must have no Godot reference — headless testing, " +
            "determinism and post-launch async PvP all depend on it.");
    }

    /// <summary>ADR-0002: the simulation is integer-only.</summary>
    [Fact]
    public void Sim_ContainsNoFloatingPointFields()
    {
        var banned = new[] { typeof(float), typeof(double), typeof(decimal) };

        string[] offenders = Sim.GetTypes()
            .Where(t => !IsCompilerGenerated(t))
            .SelectMany(t => t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                           BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => banned.Contains(f.FieldType))
                .Select(f => $"{t.FullName}.{f.Name} ({f.FieldType.Name})"))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "ADR-0002 breached: floating-point fields found in Augury.Sim — " +
            string.Join(", ", offenders) +
            ". Determinism requires integer-only arithmetic; use permille " +
            "scalars via Arith.ScalePermille.");
    }

    /// <summary>
    /// ADR-0003: MatchState is blittable — no reference-typed members.
    /// Becomes active automatically once the type exists.
    /// </summary>
    [Fact]
    public void MatchState_HasNoReferenceTypedMembers()
    {
        Type? matchState = Sim.GetType("Augury.Sim.MatchState");
        if (matchState is null)
        {
            return; // Not yet implemented. This test arms itself when it is.
        }

        string[] offenders = ReferenceTypedFields(matchState, new HashSet<Type>()).ToArray();

        Assert.True(
            offenders.Length == 0,
            "ADR-0003 breached: MatchState contains reference-typed members — " +
            string.Join(", ", offenders) +
            ". A single one destroys blittability, deterministic serialisation " +
            "and the allocation-free clone at the same time.");
    }

    /// <summary>ADR-0003: MatchState stays under the 1 KB copy budget.</summary>
    [Fact]
    public void MatchState_IsUnderOneKilobyte()
    {
        Type? matchState = Sim.GetType("Augury.Sim.MatchState");
        if (matchState is null)
        {
            return; // Not yet implemented.
        }

        int size = System.Runtime.InteropServices.Marshal.SizeOf(matchState);

        Assert.True(
            size < 1024,
            $"ADR-0003 breached: MatchState is {size} bytes, over the 1 KB budget. " +
            "It is cloned ~19,000 times per round.");
    }

    private static IEnumerable<string> ReferenceTypedFields(Type type, HashSet<Type> seen)
    {
        if (!seen.Add(type))
        {
            yield break;
        }

        foreach (FieldInfo f in type.GetFields(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            Type ft = f.FieldType;
            if (!ft.IsValueType)
            {
                yield return $"{type.Name}.{f.Name} ({ft.Name})";
            }
            else if (!ft.IsPrimitive && !ft.IsEnum && ft != typeof(decimal))
            {
                foreach (string nested in ReferenceTypedFields(ft, seen))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool IsCompilerGenerated(Type t)
        => t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
           || (t.FullName?.Contains('<') ?? false);
}
