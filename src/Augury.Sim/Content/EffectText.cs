namespace Augury.Sim.Content;

/// <summary>
/// Plain-language text for effects and opening instructions, shared by every client so the
/// terminal and the browser describe content identically.
/// </summary>
public static class EffectText
{
    /// <summary>One effect, e.g. <c>damage ×1.2</c> or <c>root (1 half)</c>.</summary>
    public static string Of(EffectDef e)
    {
        string text = e.Kind switch
        {
            EffectKind.Damage => e.Power > 0 ? $"damage ×{e.Power / 1000.0:0.##}" : $"{e.Amount} damage",
            EffectKind.Heal => e.Power > 0 ? $"heal ×{e.Power / 1000.0:0.##}" : $"heal {e.Amount}",
            EffectKind.Shield => $"shield {e.Amount}",
            EffectKind.Poison => $"poison {e.Amount}/round for {e.Rounds}",
            EffectKind.Displace => e.Amount > 0 ? $"push {e.Amount}" : $"pull {-e.Amount}",
            EffectKind.Dash => $"dash up to {e.Amount}",
            EffectKind.Root => $"root ({Halves(e.Amount)})",
            EffectKind.Burn => $"burn {e.Amount} per action for {e.Rounds} rounds",
            EffectKind.Mark => $"mark (next hit +{e.Amount})",
            EffectKind.Wall => $"wall for {e.Amount} round{(e.Amount == 1 ? "" : "s")}",
            EffectKind.Swap => "swap places",
            EffectKind.PullAlly => $"pull ally up to {e.Amount} toward you",
            EffectKind.Unstoppable => $"unstoppable ({Halves(e.Amount)})",
            EffectKind.Cleanse => "cleanse",
            EffectKind.Exhaust => $"exhaust: half damage ({Halves(e.Amount)})",
            EffectKind.Wound => $"healing halved for {e.Rounds} rounds",
            EffectKind.StructureDamage => $"{e.Amount} to a tower or open nexus, ignoring defenders",
            EffectKind.Teleport => "teleport beside a friendly tower or beacon",
            EffectKind.HealWoundedAlly => $"heal the most wounded ally within {e.Rounds} for {e.Amount}",
            _ => e.Kind.ToString(),
        };

        if (e.Slam > 0) text += $", {e.Slam} if it slams into something";
        if (e.BonusVs != StatusKind.None)
        {
            string vs = e.BonusVs.ToString().ToLowerInvariant();
            text += e.BonusPermille > 0
                ? $", ×{e.BonusPermille / 1000.0:0.##} vs {vs}{(e.BonusFlat > 0 ? $" (at least +{e.BonusFlat})" : "")}"
                : e.BonusDouble ? $", doubled vs {vs}" : $", +{e.BonusFlat} vs {vs}";
        }

        return text;
    }

    /// <summary>All of an ability's effects.</summary>
    public static string Effects(AbilityDef a) => string.Join(" + ", a.Effects.Select(Of));

    /// <summary>"QWE" plus the summoner slot.</summary>
    public static char SlotKey(int slot) => "QWER"[slot];

    /// <summary>One opening instruction, e.g. <c>top → forward-right</c> or <c>mid casts E</c>.</summary>
    public static string Instruction(OpeningInstruction i) => i.Kind switch
    {
        InstructionKind.Move => $"{i.Role.ToString().ToLowerInvariant()} → {Directions.Names[i.Direction]}",
        InstructionKind.Cast => $"{i.Role.ToString().ToLowerInvariant()} casts {SlotKey(i.Slot)}",
        _ => $"beacon {Game.SigilName(i.Sigil)} under {i.Role.ToString().ToLowerInvariant()}",
    };

    private static string Halves(int n) => n == 1 ? "1 half" : $"{n} halves";
}
