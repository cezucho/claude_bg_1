using Augury.Sim;

namespace Augury.Tools;

/// <summary>Prints the full event log of one seeded random match — a rules sanity read.</summary>
public static class Trace
{
    public static void Run(string[] args)
    {
        uint seed = args.Length > 1 ? uint.Parse(args[1]) : 7;
        Game game = Game.LoadDefault();
        string? board = args.Skip(2).FirstOrDefault(a => a.StartsWith("board="))?["board=".Length..];
        if (board is not null) Board.Use(Augury.Sim.BoardLayout.Load(board));
        MatchState s = game.NewMatch();
        var log = new List<GameEvent>();
        var legal = new List<Command>();
        uint x = seed;
        while (s.Phase != Phase.MatchOver)
        {
            legal.Clear();
            game.Legal(s, legal);
            x = x * 1664525u + 1013904223u;
            game.Apply(ref s, legal[(int)((x >> 8) % (uint)legal.Count)], log);
        }

        foreach (GameEvent e in log) Console.WriteLine(e.Text);
    }
}
