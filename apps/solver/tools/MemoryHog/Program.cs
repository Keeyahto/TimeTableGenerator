// Synthetic RAM consumer for DevHost watchdog tests (dev only).
var chunkMb = 64;
if (args.Length > 0 && int.TryParse(args[0], out var parsed) && parsed > 0)
{
    chunkMb = parsed;
}

Console.WriteLine($"MemoryHog: allocating {chunkMb} MB chunks until killed...");
var chunks = new List<byte[]>();
var rnd = new Random(42);

while (true)
{
    var buffer = new byte[chunkMb * 1024L * 1024L];
    rnd.NextBytes(buffer.AsSpan(0, Math.Min(buffer.Length, 4096)));
    chunks.Add(buffer);
    var totalMb = chunks.Count * (long)chunkMb;
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] total ~{totalMb} MB ({chunks.Count} chunks)");
    Thread.Sleep(300);
}
