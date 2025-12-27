using SidecarService.Core;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace SidecarService.Common;

public record ExecuteCommand(string Command);
public record ExecuteResult(string? Output, string? Error);

public class ProcessCommandHandler
{
    public static void Configure(HandlerChain chain, AppOptions appOptions)
    {
        var retries = appOptions.Process.Retries.Select(x => TimeSpan.FromMilliseconds(x)).ToArray();
        chain.OnAnyException().RetryWithCooldown(retries);
    }
    
    public static async Task<ExecuteResult> Handle(ExecuteCommand command, ILogger<ProcessCommandHandler> logger, AppOptions appOptions)
    {
        var exeCmd = command.Command;
        if (string.IsNullOrWhiteSpace(exeCmd))
        {
            return new(null, "Command is not provided");
        }
        
        exeCmd = exeCmd.Replace("[Name]", appOptions.Process.Name);
        
        logger.LogDebug("Before execute command: {exeCmd}", exeCmd);
        
        using var proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = "/bin/bash";
        proc.StartInfo.Arguments = $"-c \" {exeCmd} \"";
        proc.StartInfo.UseShellExecute = false; 
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.RedirectStandardOutput = true;

        if (!proc.Start())
        {
            return new(null, $"Unable to execute the command {exeCmd}");
        }
        
        var output = await proc.StandardOutput.ReadToEndAsync();
        var error = await proc.StandardError.ReadToEndAsync();
        
        logger.LogDebug("Executed command: {exeCmd}", exeCmd);
        
        return new(output, error);
    }
}