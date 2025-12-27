using SidecarService.Common;
using SidecarService.Core;
using Wolverine;

namespace SidecarService.Process;

public record ProcessFileCommand(string File, string? Command, TimeSpan? Timeout);
public record ProcessFileResult(bool Status, string? Content = null, string? Error = null);

public class ProcessFileHandler
{
    public static async Task<ProcessFileResult> Handle(ProcessFileCommand command, ILogger<ProcessFileHandler> logger,
        AppOptions appOptions, IMessageBus messageBus)
    {
        if (string.IsNullOrWhiteSpace(command.File)) return new(false, Error: "File name is required");
        if (command.File.Contains("..")) return new(false, Error: "Invalid file: path traversal detected");

        var filePath = Path.Combine(appOptions.DataFolderPath, command.File);
        
        var fullPath = Path.GetFullPath(filePath);
        var dataFolderFullPath = Path.GetFullPath(appOptions.DataFolderPath);
        if (!fullPath.StartsWith(dataFolderFullPath, StringComparison.Ordinal))
        {
            logger.LogWarning("Path traversal attempt detected: {File}", command.File);
            return new(false, Error: "Invalid file: path traversal detected");
        }
        if (!File.Exists(fullPath))
        {
            return new(false, Error: $"File not found: {command.File}");
        }
        
        var cmd = command.Command;
        if (string.IsNullOrWhiteSpace(cmd)) cmd = appOptions.Process.Command;
        if (string.IsNullOrWhiteSpace(cmd)) return new(false, Error: $"Command is not provided");
        
        cmd = cmd.Replace("[FilePath]", fullPath);

        var result = await messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(cmd, command.Timeout));
        return new(true, result.Output, result.Error);
    }
}