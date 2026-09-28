using Augury.Sim;
using Augury.Sim.AI;

namespace Augury.Web;

/// <summary>
/// The one match the server holds. Every call takes the lock, so the page can poll freely.
/// The legal-command list is cached per state so that the indices the page was shown are
/// the indices it plays.
/// </summary>
public sealed class MatchSession
{
    private readonly object _lock = new();
    private readonly Game _game;
    private readonly IAgent _ai;
    private IAgent _drafter = new RandomAgent(1);
    private readonly List<GameEvent> _log = [];
    private readonly Stack<(MatchState State, int LogCount, LastAction? Last)> _undo = new();
    private LastAction? _last;
    private int _actions;
    private MatchState _s;
    private List<Command> _legal = [];
    private HashSet<Team> _humans = [Team.A];
    private string _mode = "vsai-A";

    /// <summary>Creates a session and starts a match against the AI as team A.</summary>
    public MatchSession(Game game)
    {
        _game = game;
        _ai = new HeuristicAgent(game);
        New("vsai-A");
    }

    /// <summary>Starts a new match. Modes: <c>vsai-A</c>, <c>vsai-B</c>, <c>hotseat</c>, <c>watch</c>.</summary>
    public object New(string mode)
    {
        lock (_lock)
        {
            _mode = mode switch { "vsai-B" or "hotseat" or "watch" => mode, _ => "vsai-A" };
            _humans = _mode switch
            {
                "vsai-B" => [Team.B],
                "hotseat" => [Team.A, Team.B],
                "watch" => [],
                _ => [Team.A],
            };

            // Presentation, not simulation: a time-based seed so AI drafts differ (ADR-0002
            // binds Augury.Sim only).
            _drafter = new RandomAgent((uint)Environment.TickCount);
            _log.Clear();
            _undo.Clear();
            _last = null;
            _s = _game.NewMatch();
            _log.Add(new GameEvent(EventKind.Phase, "Draft — snake order A, B, B, A, A, B, B, A, A, B."));
            Refresh();
            return ViewLocked();
        }
    }

    /// <summary>The current view.</summary>
    public object View()
    {
        lock (_lock) return ViewLocked();
    }

    /// <summary>Applies the human's choice <paramref name="index"/> from the last legal list.</summary>
    public object Play(int index)
    {
        lock (_lock)
        {
            if (!HumanTurn) return Error("It is not a human player's turn.");
            if (index < 0 || index >= _legal.Count) return Error("That action is no longer legal.");
            _undo.Push((_s, _log.Count, _last));
            ApplyAndRecord(_legal[index]);
            return ViewLocked();
        }
    }

    /// <summary>Applies one AI decision, so the page can show AI turns step by step.</summary>
    public object Step()
    {
        lock (_lock)
        {
            if (_s.Phase == Phase.MatchOver || HumanTurn) return ViewLocked();
            Command cmd = _s.Phase == Phase.Draft ? _drafter.Choose(_s, _legal) : _ai.Choose(_s, _legal);
            ApplyAndRecord(cmd);
            return ViewLocked();
        }
    }

    /// <summary>Takes back the most recent human decision, and any AI moves after it.</summary>
    public object Undo()
    {
        lock (_lock)
        {
            if (_undo.Count == 0) return Error("Nothing to undo.");
            (MatchState state, int logCount, LastAction? last) = _undo.Pop();
            _s = state;
            _last = last;
            _log.RemoveRange(logCount, _log.Count - logCount);
            _log.Add(new GameEvent(EventKind.Phase, "↶ Undone."));
            Refresh();
            return ViewLocked();
        }
    }

    private void ApplyAndRecord(Command cmd)
    {
        MatchState before = _s;
        int logStart = _log.Count;
        _game.Apply(ref _s, cmd, _log);
        _last = new LastAction(++_actions, before, cmd, logStart);
        Refresh();
    }

    private bool HumanTurn => _s.Phase != Phase.MatchOver && _humans.Contains(_s.Active);

    private void Refresh() => _legal = _s.Phase == Phase.MatchOver ? [] : _game.Legal(_s);

    private object ViewLocked(string? error = null) =>
        ViewBuilder.Build(_game, _s, _legal, _log, _mode, _humans, HumanTurn, _undo.Count > 0, _last, error);

    private object Error(string message) => ViewLocked(message);
}

/// <summary>The most recent action: the state it was played on, the command, and where its events start.</summary>
/// <param name="Id">Increasing action number, so the page animates each action once.</param>
/// <param name="Before">The state the command was applied to.</param>
/// <param name="Command">The command.</param>
/// <param name="LogStart">Index of the action's first event in the log.</param>
public sealed record LastAction(int Id, MatchState Before, Command Command, int LogStart);
