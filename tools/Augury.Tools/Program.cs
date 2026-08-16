using Augury.Tools;

// Balance harness entry point. Add measurements as subcommands.
string command = args.Length > 0 ? args[0] : "applicability";

switch (command)
{
    case "applicability":
        ApplicabilityMeasurement.Run();
        break;
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Known: applicability");
        return 1;
}

return 0;
