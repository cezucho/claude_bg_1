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
    Dash,

    /// <summary>v2. Target can't move, dash or be moved for <c>Amount</c> half-ends (2 = this half and the next).</summary>
    Root,

    /// <summary>v2. Target takes <c>Amount</c> (through shields) whenever it resolves an ability or basic attack, for <c>Rounds</c> rounds.</summary>
    Burn,

    /// <summary>v2. The next hit on the target, from anyone, deals <c>Amount</c> more. Cleared at round close.</summary>
    Mark,

    /// <summary>v2. The target empty hex becomes impassable for <c>Amount</c> round-ends.</summary>
    Wall,

    /// <summary>v2. Caster and the target ally trade places.</summary>
    Swap,

    /// <summary>v2. The target ally moves up to <c>Amount</c> hexes toward the caster.</summary>
    PullAlly,

    /// <summary>v2. Target is immune to root and to being moved for <c>Amount</c> half-ends; clears root.</summary>
    Unstoppable,

    /// <summary>v2. Removes root, burn, poison, mark, exhaust and wound from the target.</summary>
    Cleanse,

    /// <summary>v2. Target deals half damage for <c>Amount</c> half-ends.</summary>
    Exhaust,

    /// <summary>v2. Target's healing is halved for <c>Rounds</c> rounds.</summary>
    Wound,

    /// <summary>v2 spell (Smite). <c>Amount</c> damage to a tower or open nexus, ignoring defenders.</summary>
    StructureDamage,

    /// <summary>v2 spell (Teleport). Caster moves to the target hex, even from its spawn hex.</summary>
    Teleport,

    /// <summary>v2 spell (Heal). The most wounded other ally within <c>Rounds</c> hexes heals <c>Amount</c>.</summary>
    HealWoundedAlly
}

/// <summary>A status an effect can key a bonus on (v2).</summary>
public enum StatusKind : byte
{
    /// <summary>No condition.</summary>
    None,

    /// <summary>Rooted.</summary>
    Rooted,

    /// <summary>Burning.</summary>
    Burning,

    /// <summary>Poisoned.</summary>
    Poisoned,

    /// <summary>Marked (checked before the hit consumes the mark).</summary>
    Marked,

    /// <summary>Exhausted.</summary>
    Exhausted
}

/// <summary>What a free-targeting (initiative 1–2) ability may be aimed at.</summary>
public enum TargetRule : byte
{
    /// <summary>An enemy champion, or a damageable enemy or neutral structure.</summary>
    Enemy,

    /// <summary>A friendly champion, including the caster.</summary>
    Ally,

    /// <summary>An unoccupied playable hex (Dash, Wall).</summary>
    EmptyHex,

    /// <summary>v2. The caster itself.</summary>
    Self,

    /// <summary>v2 spell (Smite). A tower the caster's team doesn't own, or an open enemy nexus.</summary>
    Structure,

    /// <summary>v2 spell (Teleport). An empty hex next to a friendly tower or beacon, any distance.</summary>
    TeleportHex
}

/// <summary>Opening instruction kinds (Opening Phase rule 2).</summary>
public enum InstructionKind : byte
{
    /// <summary>Move the champion in <c>Role</c> one hex in <c>Direction</c>.</summary>
    Move,

    /// <summary>Place a friendly beacon of <c>Sigil</c> on the hex <c>Role</c> occupies.</summary>
    PlaceBeacon,

    /// <summary>
    /// v2. The champion in <c>Role</c> casts its ability in slot <c>Slot</c> (0–2). It aims
    /// itself (D-043); with nothing to hit it fizzles but still goes on cooldown.
    /// </summary>
    Cast
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
/// <param name="Rounds">Poison, burn or wound duration; range for HealWoundedAlly.</param>
/// <param name="BonusVs">v2: a status on the target that improves this effect.</param>
/// <param name="BonusFlat">v2: extra damage against such a target.</param>
/// <param name="BonusDouble">v2: double damage against such a target.</param>
/// <param name="Slam">v2, Displace only: damage dealt when a push is stopped short by a wall,
/// tower, champion, the board's edge, or a root.</param>
/// <param name="BonusPermille">v2: multiplier against such a target, permille (1500 = ×1.5). With
/// <paramref name="BonusFlat"/> as well, the bonus is at least that much (owner 2026-10-08:
/// synergy payoffs medium-sized, leaning small — D-052).</param>
public sealed record EffectDef(EffectKind Kind, int Power, int Amount, int Rounds,
    StatusKind BonusVs = StatusKind.None, int BonusFlat = 0, bool BonusDouble = false, int Slam = 0, int BonusPermille = 0);

/// <summary>
/// v2: champions that work well together (<c>assets/data/synergies.json</c>). Data for the
/// draft screen, the AI drafter and the harness; the rules never read it.
/// </summary>
/// <param name="Id">Stable id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Idea">One sentence: what the group does together.</param>
/// <param name="Members">Champion ids: givers and wanters.</param>
/// <param name="Givers">Champions that apply the group's status (or make walls).</param>
/// <param name="Wanters">Champions with a payoff on it.</param>
public sealed record SynergyGroup(string Id, string Name, string Idea, IReadOnlyList<string> Members,
    IReadOnlyList<string> Givers, IReadOnlyList<string> Wanters)
{
    /// <summary>True when one of the two gives what the other wants (D-056).</summary>
    public bool Links(string a, string b) =>
        (Givers.Contains(a) && Wanters.Contains(b)) || (Givers.Contains(b) && Wanters.Contains(a));
}

/// <summary>One of an ability's three opening instructions.</summary>
/// <param name="Kind">Move or PlaceBeacon.</param>
/// <param name="Role">The role addressed.</param>
/// <param name="Direction">Canonical-frame direction index 0–5 (see <see cref="Directions"/>).</param>
/// <param name="Sigil">Beacon sigil for PlaceBeacon.</param>
/// <param name="Slot">Ability slot 0–2 for Cast.</param>
public sealed record OpeningInstruction(InstructionKind Kind, Role Role, int Direction, int Sigil, int Slot = -1);

/// <summary>
/// v2. A champion's own fourth opening, tied to the summoner slot. The spell fills the slot
/// in combat but never brings an opening (owner, <c>design/v2-direction.md</c>).
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Opening">Exactly three instructions.</param>
public sealed record SignatureOpening(string Name, IReadOnlyList<OpeningInstruction> Opening);

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

    /// <summary>
    /// v2 spells: a fixed range in hexes, instead of the caster's reach plus a bonus. 0 means
    /// reach-based.
    /// </summary>
    public int FixedRange { get; init; }

    /// <summary>True for a summoner spell.</summary>
    public bool IsSpell { get; init; }

    /// <summary>True for free-targeting abilities: no pattern (initiative 1–2, and spells).</summary>
    public bool IsFree => Pattern.Count == 0;

    /// <summary>True when an effect moves the caster (Dash, Teleport, Swap): a root stops it.</summary>
    public bool MovesCaster => Effects.Any(e => e.Kind is EffectKind.Dash or EffectKind.Teleport or EffectKind.Swap);

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

    /// <summary>
    /// v2 champions: their own fourth opening. Present means three abilities plus a summoner
    /// slot; absent means a v1 champion with four abilities.
    /// </summary>
    public SignatureOpening? Signature { get; init; }

    /// <summary>True for a v2 champion (three abilities, a signature opening and a spell slot).</summary>
    public bool HasSpellSlot => Signature is not null;

    /// <summary>
    /// v2 (D-056): the status this champion thrives on. Everything it deals to an enemy carrying it
    /// — abilities and basic attacks — hits harder (<see cref="RulesConfig.WantBonus"/>). Its own
    /// kit never applies it; a teammate has to.
    /// </summary>
    public StatusKind Wants { get; init; }

    /// <summary>v2: how the champion is meant to be played, in one sentence (may be empty).</summary>
    public string Line { get; init; } = "";

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
