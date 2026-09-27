namespace Augury.Sim.Content;

/// <summary>The five lane roles. Opening instructions address roles, never champions.</summary>
public enum Role : byte
{
    /// <summary>Starts at the left end of the front line.</summary>
    Top,

    /// <summary>Starts second from the left.</summary>
    Jungle,

    /// <summary>Starts in the middle of the front line.</summary>
    Mid,

    /// <summary>Starts second from the right.</summary>
    Bottom,

    /// <summary>Starts at the right end of the front line.</summary>
    Support
}

/// <summary>The five stats. <c>RES</c> was deleted (schema, unparked 2026-08-17).</summary>
public enum Stat : byte
{
    /// <summary>Vitality: HP pool, permille of HP (30000 = 30 HP).</summary>
    Vit,

    /// <summary>Power: multiplier on damage and healing, permille.</summary>
    Pow,

    /// <summary>Armour: flat reduction per hit, permille of one point.</summary>
    Arm,

    /// <summary>Reach: hexes of free-targeting and basic-attack range, permille; capped at 3.</summary>
    Rch,

    /// <summary>Speed: path-length hexes per basic move, permille.</summary>
    Spd
}

/// <summary>What an ability effect does.</summary>
public enum EffectKind : byte
{
    /// <summary>Deals <c>3 × Power × POW / 1e6 − ARM</c>, minimum 1.</summary>
    Damage,

    /// <summary>Restores <c>3 × Power × POW / 1e6</c> HP, capped at max.</summary>
    Heal,

    /// <summary>Grants <c>Amount</c> shield, cleared at upkeep.</summary>
    Shield,

    /// <summary>Applies poison: <c>Amount</c> damage per status phase for <c>Rounds</c> rounds.</summary>
    Poison,

    /// <summary>Pushes each target <c>Amount</c> hexes away from the caster; negative pulls.</summary>
    Displace,

    /// <summary>Moves the caster to the target hex, at most <c>Amount</c> hexes away.</summary>
    Dash
}

/// <summary>What a free-targeting (initiative 1–2) ability may be aimed at.</summary>
public enum TargetRule : byte
{
    /// <summary>An enemy champion, or a damageable enemy or neutral structure.</summary>
    Enemy,

    /// <summary>A friendly champion, including the caster.</summary>
    Ally,

    /// <summary>An unoccupied playable hex (Dash).</summary>
    EmptyHex
}

/// <summary>Opening instruction kinds (Opening Phase rule 2).</summary>
public enum InstructionKind : byte
{
    /// <summary>Move the champion in <c>Role</c> one hex in <c>Direction</c>.</summary>
    Move,

    /// <summary>Place a friendly beacon of <c>Sigil</c> on the hex <c>Role</c> occupies.</summary>
    PlaceBeacon
}

/// <summary>When a passive fires (MVP rules §10).</summary>
public enum PassiveTrigger : byte
{
    /// <summary>This champion takes damage from an enemy ability or basic attack.</summary>
    OnDamaged,

    /// <summary>An enemy ends a basic move within this champion's reach.</summary>
    OnEnemyEntersReach,

    /// <summary>A friendly champion dies at the death check.</summary>
    OnAllyDies,

    /// <summary>Round-close upkeep.</summary>
    OnRoundClose
}

/// <summary>What a passive does when it fires.</summary>
public enum PassiveEffect : byte
{
    /// <summary>Deal <c>Amount</c> to the source of the damage, if within 1 hex.</summary>
    Retaliate,

    /// <summary>Deal <c>Amount</c> to the triggering enemy.</summary>
    Strike,

    /// <summary>Heal self by <c>Amount</c>.</summary>
    HealSelf,

    /// <summary>Gain <c>Amount</c> shield.</summary>
    ShieldSelf,

    /// <summary>Permanently add <c>Amount</c> permille drift to <c>Stat</c>.</summary>
    EmpowerSelf
}

/// <summary>One effect of an ability.</summary>
/// <param name="Kind">What the effect does.</param>
/// <param name="Power">Permille power for Damage and Heal (≈ ladder <c>M(i)</c>).</param>
/// <param name="Amount">Shield amount, poison per round, displace hexes, or dash distance.</param>
/// <param name="Rounds">Poison duration.</param>
public sealed record EffectDef(EffectKind Kind, int Power, int Amount, int Rounds);

/// <summary>One of an ability's three opening instructions.</summary>
/// <param name="Kind">Move or PlaceBeacon.</param>
/// <param name="Role">The role addressed.</param>
/// <param name="Direction">Canonical-frame direction index 0–5 (see <see cref="Directions"/>).</param>
/// <param name="Sigil">Beacon sigil for PlaceBeacon.</param>
public sealed record OpeningInstruction(InstructionKind Kind, Role Role, int Direction, int Sigil);

/// <summary>An ability definition. Immutable content, never copied into match state.</summary>
public sealed record AbilityDef
{
    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Initiative 1–4; also sets targeting rigidity.</summary>
    public required int Initiative { get; init; }

    /// <summary>Rounds before reuse, 0–4.</summary>
    public required int Cooldown { get; init; }

    /// <summary>Target rule for free-targeting abilities.</summary>
    public TargetRule Target { get; init; }

    /// <summary>Added to the caster's reach for free-targeting range, clamped to 1–3.</summary>
    public int RangeBonus { get; init; }

    /// <summary>Pattern offsets from the caster (tiers 3–4), canonical forward frame.</summary>
    public required IReadOnlyList<HexCoord> Pattern { get; init; }

    /// <summary>One or two effects, applied in order.</summary>
    public required IReadOnlyList<EffectDef> Effects { get; init; }

    /// <summary>The stat this ability scales from (cross rule).</summary>
    public Stat ScalesFrom { get; init; }

    /// <summary>Stat raised after use.</summary>
    public Stat MoldUp { get; init; }

    /// <summary>Permille added to <see cref="MoldUp"/>.</summary>
    public int MoldUpDelta { get; init; }

    /// <summary>Stat lowered after use.</summary>
    public Stat MoldDown { get; init; }

    /// <summary>Permille removed from <see cref="MoldDown"/>.</summary>
    public int MoldDownDelta { get; init; }

    /// <summary>Printed sigil 0–2, or −1 for none.</summary>
    public int PrintedSigil { get; init; } = -1;

    /// <summary>Typed slot sigil 0–2, or −1 for none.</summary>
    public int SlotSigil { get; init; } = -1;

    /// <summary>Exactly three opening instructions.</summary>
    public required IReadOnlyList<OpeningInstruction> Opening { get; init; }

    /// <summary>True for free-targeting tiers (initiative 1–2).</summary>
    public bool IsFree => Initiative <= 2;

    /// <summary>True when any effect is Damage.</summary>
    public bool DealsDamage => Effects.Any(e => e.Kind == EffectKind.Damage);
}

/// <summary>A champion's passive.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Trigger">When it fires.</param>
/// <param name="Effect">What it does.</param>
/// <param name="Amount">Magnitude.</param>
/// <param name="Stat">Stat for EmpowerSelf.</param>
public sealed record PassiveDef(string Name, PassiveTrigger Trigger, PassiveEffect Effect, int Amount, Stat Stat);

/// <summary>A champion definition.</summary>
public sealed record ChampionDef
{
    /// <summary>Stable identifier, e.g. <c>warden</c>.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Lane role.</summary>
    public required Role Role { get; init; }

    /// <summary>One-letter board glyph.</summary>
    public required char Glyph { get; init; }

    /// <summary>Base stats in permille, indexed by <see cref="Stat"/>, after the initiative trade.</summary>
    public required IReadOnlyList<int> BaseStats { get; init; }

    /// <summary>Q, W, E, R.</summary>
    public required IReadOnlyList<AbilityDef> Abilities { get; init; }

    /// <summary>The passive.</summary>
    public required PassiveDef Passive { get; init; }

    /// <summary>Base stat by enum.</summary>
    public int Base(Stat s) => BaseStats[(int)s];
}

/// <summary>Team-relative direction names for opening instructions.</summary>
public static class Directions
{
    /// <summary>Direction names in <see cref="Hex.Directions"/> order, canonical frame (forward = +R).</summary>
    public static readonly string[] Names = ["right", "back-right", "back-left", "left", "forward-left", "forward-right"];

    /// <summary>Parses a direction name to its index.</summary>
    public static int Parse(string name)
    {
        int i = Array.IndexOf(Names, name.Trim().ToLowerInvariant());
        if (i < 0)
        {
            throw new ContentException($"Unknown direction '{name}'. Expected one of: {string.Join(", ", Names)}.");
        }

        return i;
    }
}

/// <summary>Content failed to load or validate. Content fails loudly (ADR-0007).</summary>
public sealed class ContentException(string message) : Exception(message);
