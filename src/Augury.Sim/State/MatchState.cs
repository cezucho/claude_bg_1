using System.Runtime.CompilerServices;
using Augury.Sim.Content;

namespace Augury.Sim;

/// <summary>A side. <see cref="None"/> marks neutral structures and empty beacon slots.</summary>
public enum Team : byte
{
    /// <summary>Front line at rank −4, advances toward +R.</summary>
    A,

    /// <summary>Front line at rank +4, advances toward −R.</summary>
    B,

    /// <summary>Neutral, or unused.</summary>
    None
}

/// <summary>Where the match is. Every decision point belongs to exactly one phase.</summary>
public enum Phase : byte
{
    /// <summary>Snake draft, one pick per command.</summary>
    Draft,

    /// <summary>
    /// v2: each team chooses its five summoner spells. Both choose before either is shown
    /// (owner: hidden and simultaneous); revealed when the opening begins.
    /// </summary>
    SpellPick,

    /// <summary>Opening Phase: one ability per champion issues three instructions.</summary>
    Opening,

    /// <summary>Two basics per team per half, before the ladder.</summary>
    Basic,

    /// <summary>The initiative ladder.</summary>
    Ladder,

    /// <summary>A team passed; the other may take one unanswerable action.</summary>
    LastWord,

    /// <summary>Terminal.</summary>
    MatchOver
}

/// <summary>Why the match ended.</summary>
public enum EndReason : byte
{
    /// <summary>Not over.</summary>
    None,

    /// <summary>A nexus fell to a direct attack.</summary>
    Nexus,

    /// <summary>A nexus fell at round close, to tower siege and kills (v2; replaced the target score).</summary>
    Siege,

    /// <summary>The round cap was hit.</summary>
    RoundCap
}

/// <summary>Where a champion is.</summary>
public enum Presence : byte
{
    /// <summary>On a playable hex.</summary>
    OnBoard,

    /// <summary>In its off-board spawn hex: untargetable, cannot act on the ladder.</summary>
    InSpawn,

    /// <summary>Dead, waiting for the respawn timer.</summary>
    Dead
}

/// <summary>Per-champion flags.</summary>
[Flags]
public enum ChampFlags : byte
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Entered Dying in the status phase; half POW, dies at the next death check if still ≤0.</summary>
    Dying = 1,

    /// <summary>Used its ladder action this half.</summary>
    Acted = 2,

    /// <summary>Took a basic this half.</summary>
    BasicUsed = 4,

    /// <summary>Has played its opening ability (or taken the fallback).</summary>
    OpeningDone = 8
}

/// <summary>Five stat drifts, indexed by <see cref="Stat"/>.</summary>
[InlineArray(5)]
public struct StatBuffer
{
    private short _element0;
}

/// <summary>Four ability cooldowns, Q–R.</summary>
[InlineArray(4)]
public struct CooldownBuffer
{
    private byte _element0;
}

/// <summary>One champion's mutable match state. Definitions live in content, referenced by <see cref="Def"/>.</summary>
public struct Champion
{
    /// <summary>Position (valid when on board or in spawn).</summary>
    public HexCoord Pos;

    /// <summary>Current HP. May be negative; nothing dies until the death check.</summary>
    public int Hp;

    /// <summary>Permanent molding drift per stat, permille.</summary>
    public StatBuffer Drift;

    /// <summary>Rounds remaining per ability.</summary>
    public CooldownBuffer Cooldowns;

    /// <summary>Absorbs damage before HP; cleared at upkeep.</summary>
    public short Shield;

    /// <summary>Content index of this champion's definition; 255 before the draft fills it.</summary>
    public byte Def;

    /// <summary>Owning team.</summary>
    public Team Team;

    /// <summary>Lane role.</summary>
    public Role Role;

    /// <summary>Board, spawn or dead.</summary>
    public Presence Presence;

    /// <summary>State flags.</summary>
    public ChampFlags Flags;

    /// <summary>Poison damage per status phase.</summary>
    public byte PoisonAmount;

    /// <summary>Poison rounds remaining.</summary>
    public byte PoisonRounds;

    /// <summary>Upkeeps until respawn.</summary>
    public byte RespawnIn;

    /// <summary>v2: content index of the summoner spell in slot 4; 255 for none.</summary>
    public byte Spell;

    /// <summary>v2: half-ends left while rooted (can't move, dash or be moved).</summary>
    public byte RootHalves;

    /// <summary>v2: half-ends left while exhausted (deals half damage).</summary>
    public byte ExhaustHalves;

    /// <summary>v2: half-ends left while unstoppable (immune to root and to being moved).</summary>
    public byte UnstoppableHalves;

    /// <summary>v2: damage taken each time it acts while burning.</summary>
    public byte BurnAmount;

    /// <summary>v2: rounds of burning left.</summary>
    public byte BurnRounds;

    /// <summary>v2: extra damage the next hit on it deals; cleared at round close.</summary>
    public byte Mark;

    /// <summary>v2: rounds of halved healing left.</summary>
    public byte WoundRounds;

    /// <summary>True while rooted.</summary>
    public readonly bool Rooted => RootHalves > 0;

    /// <summary>True while burning.</summary>
    public readonly bool Burning => BurnRounds > 0;

    /// <summary>True while unstoppable.</summary>
    public readonly bool Unstoppable => UnstoppableHalves > 0;

    /// <summary>True when the champion stands on a playable hex.</summary>
    public readonly bool OnBoard => Presence == Presence.OnBoard;

    /// <summary>Tests a flag.</summary>
    public readonly bool Has(ChampFlags f) => (Flags & f) != 0;
}

/// <summary>A tower: captured, never destroyed.</summary>
public struct Tower
{
    /// <summary>Hex.</summary>
    public HexCoord Pos;

    /// <summary>Current HP.</summary>
    public short Hp;

    /// <summary>Owner, or <see cref="Team.None"/> for neutral.</summary>
    public Team Owner;

    /// <summary>The team whose home tower this is, or None for the centre.</summary>
    public Team Home;
}

/// <summary>A beacon slot. <see cref="Team"/> = None means empty.</summary>
public struct Beacon
{
    /// <summary>Hex.</summary>
    public HexCoord Pos;

    /// <summary>Owner, or None if the slot is empty.</summary>
    public Team Team;

    /// <summary>Sigil 0–2.</summary>
    public byte Sigil;

    /// <summary>Enemy basic attacks remaining before it breaks.</summary>
    public byte Durability;
}

/// <summary>v2: a wall raised on an empty hex. <see cref="Rounds"/> = 0 means the slot is empty.</summary>
public struct Wall
{
    /// <summary>Hex.</summary>
    public HexCoord Pos;

    /// <summary>Round-ends left before it falls.</summary>
    public byte Rounds;
}

/// <summary>Wall slots.</summary>
[InlineArray(4)]
public struct WallBuffer
{
    private Wall _element0;
}

/// <summary>Ten champions: slots 0–4 team A by role, 5–9 team B by role.</summary>
[InlineArray(10)]
public struct ChampionBuffer
{
    private Champion _element0;
}

/// <summary>Five towers: 0 centre, 1–2 team A's, 3–4 team B's.</summary>
[InlineArray(5)]
public struct TowerBuffer
{
    private Tower _element0;
}

/// <summary>Beacon slots.</summary>
[InlineArray(12)]
public struct BeaconBuffer
{
    private Beacon _element0;
}

/// <summary>Two per-team integers.</summary>
[InlineArray(2)]
public struct TeamInts
{
    private int _element0;
}

/// <summary>Two per-team bytes.</summary>
[InlineArray(2)]
public struct TeamBytes
{
    private byte _element0;
}

/// <summary>
/// The whole match. A blittable value struct (ADR-0003): cloning is assignment, with no
/// reference members and no allocation. Definitions are referenced by index, never copied.
/// </summary>
public struct MatchState
{
    /// <summary>All ten champions.</summary>
    public ChampionBuffer Champions;

    /// <summary>All five towers.</summary>
    public TowerBuffer Towers;

    /// <summary>Beacon slots.</summary>
    public BeaconBuffer Beacons;

    /// <summary>v2: wall slots.</summary>
    public WallBuffer Walls;

    /// <summary>Remaining nexus HP per team.</summary>
    public TeamInts NexusHp;

    /// <summary>Basics taken this half per team.</summary>
    public TeamBytes BasicsTaken;

    /// <summary>1 when a team's opening has fallen back to one hex per champion.</summary>
    public TeamBytes OpeningFallback;

    /// <summary>Current round, 1-based; 0 before round 1.</summary>
    public int Round;

    /// <summary>Current phase.</summary>
    public Phase Phase;

    /// <summary>The team whose decision it is.</summary>
    public Team Active;

    /// <summary>Team that opens the first half of the current round.</summary>
    public Team RoundOpener;

    /// <summary>1 or 2.</summary>
    public byte Half;

    /// <summary>Ladder ceiling, 1–4.</summary>
    public byte Ceiling;

    /// <summary>Draft picks made, 0–10.</summary>
    public byte DraftPicks;

    /// <summary>Winner once over; None for a draw.</summary>
    public Team Winner;

    /// <summary>Why it ended.</summary>
    public EndReason EndReason;

    /// <summary>Abilities resolved in the current half, both teams.</summary>
    public byte ResolutionsThisHalf;

    /// <summary>Home towers a team must lose before its nexus opens (from rules; D-002).</summary>
    public byte NexusGate;

    /// <summary>The opener of the current half.</summary>
    public readonly Team HalfOpener => Half == 1 ? RoundOpener : Other(RoundOpener);

    /// <summary>The opposing team.</summary>
    public static Team Other(Team t) => t == Team.A ? Team.B : Team.A;

    /// <summary>Slot index range for a team.</summary>
    public static int FirstSlot(Team t) => t == Team.A ? 0 : 5;

    /// <summary>Slot of a team's champion in a role.</summary>
    public static int Slot(Team t, Role r) => FirstSlot(t) + (int)r;
}
