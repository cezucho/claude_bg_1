using Augury.Sim;
using Augury.Web;

// AUGURY — browser client for the first playable.
//   dotnet run --project src/Augury.Web            then open http://localhost:5080
//   dotnet run --project src/Augury.Web -- --port 8080

int port = 5080;
int p = Array.IndexOf(args, "--port");
if (p >= 0 && p + 1 < args.Length && int.TryParse(args[p + 1], out int requested)) port = requested;

Game game;
try
{
    game = Game.LoadDefault();
}
catch (Exception e)
{
    Console.Error.WriteLine($"Could not load game content: {e.Message}");
    Console.Error.WriteLine("Run from inside the repository so assets/data can be found.");
    return 1;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.UseUrls($"http://localhost:{port}");

var app = builder.Build();
var session = new MatchSession(game);

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    // A local tool: always serve the latest page after a rebuild.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-store",
});

app.MapGet("/api/state", () => Results.Json(session.View()));
app.MapPost("/api/new", (string? mode) => Results.Json(session.New(mode ?? "vsai-A")));
app.MapPost("/api/play/{index:int}", (int index) => Results.Json(session.Play(index)));
app.MapPost("/api/step", () => Results.Json(session.Step()));
app.MapPost("/api/undo", () => Results.Json(session.Undo()));

Console.WriteLine($"AUGURY is running — open http://localhost:{port} in your browser. Ctrl+C to stop.");
app.Run();
return 0;
