using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;

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

var limitPct = int.TryParse(Environment.GetEnvironmentVariable("SCHED_MEM_LIMIT_PCT"), out var p) ? p : 95;
var logPath = Path.GetFullPath(Path.Combine(
    Environment.GetEnvironmentVariable("SCHED_REPO_ROOT") ?? Directory.GetCurrentDirectory(),
    "tmp",
    "solver-watchdog.log"));

Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

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

    var peakWorkingSetMb = 0d;

    while (!process.HasExited)
    {
        if (TryGetSystemMemoryUsedPercent(out var usedPct) && usedPct >= limitPct)
        {
            KillTree(process);
        await File.AppendAllTextAsync(
            logPath,
            $"{DateTimeOffset.Now:u} WATCHDOG system RAM {usedPct:F1}% >= {limitPct}% — killed PID {process.Id}, peak child WS {peakWorkingSetMb:F0} MB{Environment.NewLine}");
        return 137;
    }

    try
    {
        process.Refresh();
        peakWorkingSetMb = Math.Max(peakWorkingSetMb, process.WorkingSet64 / (1024d * 1024d));
    }
    catch
    {
        // process exited
    }

    await Task.Delay(500);
}

await process.WaitForExitAsync();
await File.AppendAllTextAsync(
    logPath,
    $"{DateTimeOffset.Now:u} OK exit={process.ExitCode} peak WS {peakWorkingSetMb:F0} MB{Environment.NewLine}");

return process.ExitCode;

static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;

static bool TryGetSystemMemoryUsedPercent(out double percent)
{
    percent = 0;
    try
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            var totalKb = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
            var freeKb = Convert.ToUInt64(obj["FreePhysicalMemory"]);
            if (totalKb == 0)
            {
                return false;
            }

            percent = (totalKb - freeKb) * 100.0 / totalKb;
            return true;
        }
    }
    catch
    {
        return false;
    }

    return false;
}

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
