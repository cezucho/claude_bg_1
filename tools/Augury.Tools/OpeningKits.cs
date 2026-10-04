using Augury.Sim;
using Augury.Sim.Content;

namespace Augury.Tools;

/// <summary>
/// For every champion's four opening abilities: can it be played from the starting line,
/// with towers solid and with towers walkable? Opening play is strict (all three
/// instructions must execute), so an instruction that walks into a tower kills the whole
/// ability. Used to repair placeholder openings after towers became impassable (v2).
/// </summary>
internal static class OpeningKits
{
    public static void Run()
    {
        Game solid = Game.LoadDefault();
        Game open = new(solid.Content, solid.Rules with { TowersBlock = false });
        IReadOnlyList<ChampionDef> roster = solid.Content.Champions;

        int[] defaults = Enum.GetValues<Role>().Select(r => solid.Content.ForRole(r).First()).ToArray();
        int okSolid = 0, okOpen = 0, total = 0;
        Console.WriteLine("OPENINGS FROM THE STARTING LINE — first move of the opening, rest of the team on the line");
        Console.WriteLine($"  {"champion",-12} {"ability",-16} {"walkable",9} {"solid",6}   instructions");
        for (int def = 0; def < roster.Count; def++)
        {
            ChampionDef d = roster[def];
            int[] picks = (int[])defaults.Clone();
            picks[(int)d.Role] = def;
            MatchState sSolid = solid.NewMatch(picks, picks), sOpen = open.NewMatch(picks, picks);
            int slot = MatchState.Slot(Team.A, d.Role);
            for (int a = 0; a < 4; a++)
            {
                bool w = open.OpeningAvailable(sOpen, slot, a), s = solid.OpeningAvailable(sSolid, slot, a);
                total++;
                if (w) okOpen++;
                if (s) okSolid++;
                string mark = w && !s ? "  ◀ broken by towers" : "";
                Console.WriteLine($"  {d.Name,-12} {d.Abilities[a].Name,-16} {(w ? "yes" : "no"),9} {(s ? "yes" : "no"),6}   "
                                  + string.Join(", ", d.Abilities[a].Opening.Select(Text)) + mark);
            }
        }

        Console.WriteLine($"\n  playable from the line: walkable towers {okOpen}/{total}, solid towers {okSolid}/{total}");
    }

    private static string Text(OpeningInstruction i) => i.Kind == InstructionKind.Move
        ? $"{i.Role.ToString().ToLowerInvariant()} {Directions.Names[i.Direction]}"
        : $"beacon {i.Sigil + 1} {i.Role.ToString().ToLowerInvariant()}";
}
