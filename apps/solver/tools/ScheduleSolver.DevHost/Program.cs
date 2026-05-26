using System.Diagnostics;
using System.Runtime.Versioning;
using ScheduleSolver.DevHost;

[assembly: SupportedOSPlatform("windows")]

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: ScheduleSolver.DevHost <path-to-ScheduleSolver.Cli.dll> [-- cli args...]");
    return 2;
}

var cliDll = Path.GetFullPath(args[0]);
var forwardArgs = args.Length > 1 && args[1] == "--" ? args[2..] : args[1..];

if (!File.Exists(cliDll))
{
    Console.Error.WriteLine($"CLI assembly not found: {cliDll}");
    return 2;
}

var watchdog = new MemoryWatchdog();
var logPath = Path.GetFullPath(Path.Combine(
    Environment.GetEnvironmentVariable("SCHED_REPO_ROOT") ?? Directory.GetCurrentDirectory(),
    "tmp",
    "solver-watchdog.log"));

Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

await File.AppendAllTextAsync(
    logPath,
    $"{DateTimeOffset.Now:u} START child_cap={watchdog.ChildLimitBytes / (1024 * 1024)} MB system_cap={watchdog.SystemLimitPercent}% poll={watchdog.PollIntervalMs}ms{Environment.NewLine}");

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"exec \"{cliDll}\" {string.Join(' ', forwardArgs.Select(Quote))}",
    UseShellExecute = false,
    RedirectStandardOutput = false,
    RedirectStandardError = false,
};

using var process = Process.Start(psi);
if (process is null)
{
    return 2;
}

var peakChildMb = 0d;
while (!process.HasExited)
{
    if (watchdog.TryCheck(process, out var reason, out var childMb, out var systemPct))
    {
        peakChildMb = Math.Max(peakChildMb, childMb);
        KillTree(process);
        await File.AppendAllTextAsync(
            logPath,
            $"{DateTimeOffset.Now:u} WATCHDOG {reason} — killed PID {process.Id}, peak tree {peakChildMb:F0} MB (system {systemPct:F1}%){Environment.NewLine}");
        Console.Error.WriteLine($"[watchdog] {reason}. Process killed (exit 137). See {logPath}");
        return 137;
    }

    peakChildMb = Math.Max(peakChildMb, childMb);
    await Task.Delay(watchdog.PollIntervalMs);
}

await process.WaitForExitAsync();
await File.AppendAllTextAsync(
    logPath,
    $"{DateTimeOffset.Now:u} OK exit={process.ExitCode} peak tree {peakChildMb:F0} MB{Environment.NewLine}");

return process.ExitCode;

static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;

static void KillTree(Process root)
{
    try
    {
        root.Kill(entireProcessTree: true);
    }
    catch
    {
        // ignore
    }
}
