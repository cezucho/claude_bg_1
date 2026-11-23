namespace Augury.Sim;

/// <summary>
/// Every way a player can change the match. ADR-0004's four ladder kinds plus the draft,
/// opening and basic-phase kinds introduced since (D-019).
/// </summary>
public enum CommandKind : byte
{
    /// <summary>Pick <see cref="Command.Ability"/> (a content index) into slot <see cref="Command.Champion"/>.</summary>
    Draft,

    /// <summary>Play ability <see cref="Command.Ability"/> of a champion in the Opening Phase.</summary>
    OpeningPlay,

    /// <summary>Opening fallback: move one hex in <see cref="Target.Facing"/>, or stay (255).</summary>
    OpeningFallback,

    /// <summary>Basic move to <see cref="Target.Hex"/>.</summary>
    BasicMove,

    /// <summary>Basic attack on <see cref="Command.Target"/>.</summary>
    BasicAttack,

    /// <summary>Play an ability on the ladder, optionally chained with a second.</summary>
    Ability,

    /// <summary>Pass: concede the Last Word.</summary>
    Pass,

    /// <summary>Decline the Last Word.</summary>
    Decline,

    /// <summary>v2: give champion <see cref="Command.Champion"/> summoner spell <see cref="Command.Ability"/> (a pool index).</summary>
    SpellPick
}

/// <summary>What a target refers to.</summary>
public enum TargetKind : byte
{
    /// <summary>No target (tier-4 abilities, pass).</summary>
    None,

    /// <summary>A champion slot.</summary>
    Champion,

    /// <summary>A tower index.</summary>
    Tower,

    /// <summary>A team's nexus; <see cref="Target.Index"/> is the team.</summary>
    Nexus,

    /// <summary>A beacon slot.</summary>
    Beacon,

    /// <summary>A hex.</summary>
    Hex,

    /// <summary>A tier-3 facing, 0–5.</summary>
    Facing
}

/// <summary>A command's target.</summary>
/// <param name="Kind">What it refers to.</param>
/// <param name="Index">Champion slot, tower, team, beacon slot, or facing.</param>
/// <param name="Hex">For hex targets.</param>
public readonly record struct Target(TargetKind Kind, byte Index, HexCoord Hex)
{
    /// <summary>No target.</summary>
    public static readonly Target None = new(TargetKind.None, 0, default);

    /// <summary>Facing for tier-3 patterns and the opening fallback direction.</summary>
    public byte Facing => Index;

    /// <summary>A champion target.</summary>
    public static Target Champ(int slot) => new(TargetKind.Champion, (byte)slot, default);

    /// <summary>A hex target.</summary>
    public static Target At(HexCoord h) => new(TargetKind.Hex, 0, h);

    /// <summary>A facing target.</summary>
    public static Target Face(int facing) => new(TargetKind.Facing, (byte)facing, default);
}

/// <summary>A single command. A chain fills the second champion, ability and target.</summary>
/// <param name="Kind">Kind.</param>
/// <param name="Champion">Acting champion slot (draft: slot being filled).</param>
/// <param name="Ability">Ability index 0–3 (draft: content index).</param>
/// <param name="Target">Target.</param>
/// <param name="Champion2">Chain partner slot, or 255.</param>
/// <param name="Ability2">Chain partner ability.</param>
/// <param name="Target2">Chain partner target.</param>
public readonly record struct Command(
    CommandKind Kind,
    byte Champion,
    byte Ability,
    Target Target,
    byte Champion2 = 255,
    byte Ability2 = 0,
    Target Target2 = default)
{
    /// <summary>True when this is a two-ability chain.</summary>
    public bool IsChain => Kind == CommandKind.Ability && Champion2 != 255;

    /// <summary>A pass.</summary>
    public static readonly Command PassCmd = new(CommandKind.Pass, 255, 0, Target.None);

    /// <summary>Decline the Last Word.</summary>
    public static readonly Command DeclineCmd = new(CommandKind.Decline, 255, 0, Target.None);
}

/// <summary>Event categories, for playback, tests and the log.</summary>
public enum EventKind : byte
{
    /// <summary>Informational phase change.</summary>
    Phase,

    /// <summary>Draft pick.</summary>
    Draft,

    /// <summary>Opening play or instruction.</summary>
    Opening,

    /// <summary>Basic move or attack.</summary>
    Basic,

    /// <summary>An ability resolved.</summary>
    AbilityResolved,

    /// <summary>Damage dealt.</summary>
    Damage,

    /// <summary>HP restored or shield gained.</summary>
    Heal,

    /// <summary>Displacement or dash.</summary>
    Move,

    /// <summary>A passive fired.</summary>
    Passive,

    /// <summary>A pass.</summary>
    Pass,

    /// <summary>The Last Word was declined.</summary>
    Decline,

    /// <summary>Round-close death check.</summary>
    DeathCheck,

    /// <summary>Round-close status phase.</summary>
    StatusPhase,

    /// <summary>A champion died.</summary>
    Death,

    /// <summary>A champion entered Dying.</summary>
    Dying,

    /// <summary>Points scored.</summary>
    Score,

    /// <summary>Structure captured or damaged.</summary>
    Structure,

    /// <summary>Beacon placed or broken.</summary>
    Beacon,

    /// <summary>Respawn.</summary>
    Respawn,

    /// <summary>Match over.</summary>
    MatchOver
}

/// <summary>What happened. Only produced when a log is supplied, so AI search pays nothing for it.</summary>
/// <param name="Kind">Category.</param>
/// <param name="Text">Human-readable description.</param>
public sealed record GameEvent(EventKind Kind, string Text);
