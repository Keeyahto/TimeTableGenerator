using System.CommandLine;
using ScheduleSolver.Core;

var inputOption = new Option<FileInfo>(
    aliases: ["--input", "-i"],
    description: "Path to solver input JSON")
{ IsRequired = true };

var outputOption = new Option<FileInfo>(
    aliases: ["--output", "-o"],
    description: "Path to solver output JSON")
{ IsRequired = true };

var modeOption = new Option<string>(
    aliases: ["--mode", "-m"],
    getDefaultValue: () => "validate",
    description: "validate | profile | diagnostic | solve");

var timeLimitOption = new Option<int>(
    aliases: ["--time-limit"],
    getDefaultValue: () => 30,
    description: "CP-SAT time limit (seconds)");

var exportDebugOption = new Option<DirectoryInfo?>(
    aliases: ["--export-debug"],
    description: "Optional debug export directory");

var variantOption = new Option<string?>(
    aliases: ["--dataset-variant"],
    description: "Metadata: A or B");

var allowLargeOption = new Option<bool>(
    aliases: ["--allow-large-model"],
    description: "Opt in to large handoff-sized CP-SAT models (high RAM)");

var root = new RootCommand("ScheduleSolver — JSON in/out timetable solver");
root.AddOption(inputOption);
root.AddOption(outputOption);
root.AddOption(modeOption);
root.AddOption(timeLimitOption);
root.AddOption(exportDebugOption);
root.AddOption(variantOption);
root.AddOption(allowLargeOption);

root.SetHandler(async (input, output, mode, timeLimit, exportDebug, variant, allowLarge) =>
{
    if (!Enum.TryParse<SolverMode>(mode, ignoreCase: true, out var solverMode))
    {
        Console.Error.WriteLine($"Unknown mode: {mode}");
        Environment.ExitCode = 2;
        return;
    }

    var options = new SolverRunOptions
    {
        InputPath = input.FullName,
        OutputPath = output.FullName,
        Mode = solverMode,
        TimeLimitSec = timeLimit,
        ExportDebugDir = exportDebug?.FullName,
        DatasetVariant = variant,
        AllowLargeModel = allowLarge,
    };

    var result = await SolverApplication.RunAsync(options);
    Environment.ExitCode = result.ExitCode;
}, inputOption, outputOption, modeOption, timeLimitOption, exportDebugOption, variantOption, allowLargeOption);

return await root.InvokeAsync(args);
