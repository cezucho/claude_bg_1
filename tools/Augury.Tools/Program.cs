using Augury.Tools;

// Balance harness entry point. Add measurements as subcommands.
string command = args.Length > 0 ? args[0] : "applicability";

switch (command)
{
    case "applicability":
        ApplicabilityMeasurement.Run();
        break;
    case "board":
        BoardLayout.Run();
        break;
    case "sigils":
        SigilDensity.Run();
        break;
    case "beacon":
        BeaconGeometry.Run();
        break;
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Known: applicability, board, sigils, beacon");
        return 1;
}

return 0;
