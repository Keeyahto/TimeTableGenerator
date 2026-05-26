using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;

namespace ScheduleSolver.DevHost;

[SupportedOSPlatform("windows")]
internal sealed class MemoryWatchdog
{
    private readonly int _systemLimitPct;
    private readonly long _childLimitBytes;
    private readonly int _pollMs;

    public MemoryWatchdog()
    {
        _systemLimitPct = ParseIntEnv("SCHED_MEM_LIMIT_PCT", 85, 50, 99);
        _pollMs = ParseIntEnv("SCHED_MEM_POLL_MS", 250, 100, 2000);
        _childLimitBytes = ResolveChildLimitBytes();
    }

    public int PollIntervalMs => _pollMs;

    public long ChildLimitBytes => _childLimitBytes;

    public int SystemLimitPercent => _systemLimitPct;

    public bool TryCheck(Process root, out string reason, out double peakChildMb, out double systemUsedPct)
    {
        peakChildMb = 0;
        systemUsedPct = 0;

        if (TryGetSystemMemoryUsedPercent(out systemUsedPct) && systemUsedPct >= _systemLimitPct)
        {
            peakChildMb = GetTreeMemoryMb(root);
            reason = $"system RAM {systemUsedPct:F1}% >= {_systemLimitPct}%";
            return true;
        }

        peakChildMb = GetTreeMemoryMb(root);
        if (peakChildMb * 1024 * 1024 >= _childLimitBytes)
        {
            reason = $"solver tree {peakChildMb:F0} MB >= {_childLimitBytes / (1024 * 1024)} MB cap";
            return true;
        }

        reason = "";
        return false;
    }

    private static long ResolveChildLimitBytes()
    {
        var totalBytes = TryGetTotalPhysicalBytes(out var total) ? total : 16L * 1024 * 1024 * 1024;
        var pct = ParseIntEnv("SCHED_MEM_CHILD_PCT", 70, 20, 95);
        var pctCap = (long)(totalBytes * (pct / 100.0));

        if (long.TryParse(Environment.GetEnvironmentVariable("SCHED_MEM_CHILD_MB"), out var mb) && mb > 0)
        {
            return Math.Min(mb * 1024 * 1024, pctCap);
        }

        var defaultMb = Math.Min(8 * 1024L, pctCap / (1024 * 1024));
        return Math.Max(512L * 1024 * 1024, defaultMb * 1024 * 1024);
    }

    private static int ParseIntEnv(string name, int defaultValue, int min, int max)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable(name), out var value))
        {
            value = defaultValue;
        }

        return Math.Clamp(value, min, max);
    }

    private static bool TryGetTotalPhysicalBytes(out long bytes)
    {
        bytes = 0;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var totalKb = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                bytes = (long)totalKb * 1024;
                return bytes > 0;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryGetSystemMemoryUsedPercent(out double percent)
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

    private static double GetTreeMemoryMb(Process root)
    {
        long bytes = 0;
        foreach (var pid in EnumerateTreeProcessIds(root.Id))
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                p.Refresh();
                bytes += Math.Max(p.PrivateMemorySize64, p.WorkingSet64);
            }
            catch
            {
                // exited between enumeration and open
            }
        }

        return bytes / (1024d * 1024d);
    }

    private static IEnumerable<int> EnumerateTreeProcessIds(int rootPid)
    {
        var queue = new Queue<int>();
        var seen = new HashSet<int>();
        queue.Enqueue(rootPid);
        while (queue.Count > 0)
        {
            var pid = queue.Dequeue();
            if (!seen.Add(pid))
            {
                continue;
            }

            yield return pid;
            foreach (var childPid in QueryChildProcessIds(pid))
            {
                queue.Enqueue(childPid);
            }
        }
    }

    private static List<int> QueryChildProcessIds(int parentPid)
    {
        var children = new List<int>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {parentPid}");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                children.Add(Convert.ToInt32(obj["ProcessId"]));
            }
        }
        catch
        {
            // ignore WMI errors
        }

        return children;
    }
}
