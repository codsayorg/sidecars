using SidecarService.Common;
using SidecarService.Core;
using Wolverine;

namespace SidecarService.Process;

public record ProcessFileCommand(string File, string? Command = null);
public record ProcessFileResult(bool Status, string? Content = null, string? Error = null);

public class ProcessFileHandler
{
    public static async Task<ProcessFileResult> Handle(ProcessFileCommand command, ILogger<ProcessFileHandler> logger,
        AppOptions appOptions, IMessageBus messageBus)
    {
        var filePath = Path.Combine(appOptions.DataFolderPath, command.File);
        if (!File.Exists(filePath)) return new(false, Error: $"File not found: {filePath}");
        
        var cmd = command.Command;
        if (string.IsNullOrWhiteSpace(cmd)) cmd = appOptions.Process.Command;
        if (string.IsNullOrWhiteSpace(cmd)) return new(false, Error: $"Command is not provided");
        cmd = cmd.Replace("[FilePath]", filePath);

        var result = await messageBus.InvokeAsync<Common.ExecuteResult>(new ExecuteCommand(cmd));
        return new(true, result.Output, result.Error);
    }
}