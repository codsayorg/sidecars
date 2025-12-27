using Microsoft.AspNetCore.Mvc;
using SidecarService.Common;
using SidecarService.Core;
using Wolverine;

namespace SidecarService.Process;

public class ProcessRequest
{
    public IList<string> Files { get; set; } = [];
    
    /// <summary>
    /// Override the default max parallelism (75% of CPU)
    /// </summary>
    public int? MaxParallelism { get; set; }

    /// <summary>
    /// Override the default command
    /// </summary>
    public string? Command { get; set; }
}

public record ExecuteCommandRequest(string Command);

public static class Process
{
    public static void MapTesseractApis(this IEndpointRouteBuilder app)
    {
        app.MapGet("/version", GetVersion);
        app.MapGet("/process", ProcessSingleFile);
        app.MapPost("/process", ProcessFile);
        
        app.MapGet("/execute", ExecuteCommand);
        app.MapPost("/execute", ExecuteCommandPost);
    }
    
    private static readonly Lazy<int> MaxParallelism = new(() => Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0)));

    private static Task<ExecuteResult> GetVersion(IMessageBus messageBus, AppOptions appOptions)
    {
        return messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(appOptions.Process.VersionCommand));
    }
    
    private static async Task<ProcessFileResult> ProcessSingleFile([FromQuery(Name = "file")] string file, IMessageBus messageBus, AppOptions appOptions)
    {
        var result = await messageBus.InvokeAsync<ProcessFileResult>(new ProcessFileCommand(file), CancellationToken.None, appOptions.Process.Timeout);
        return result;
    }

    private static async Task<ProcessFileResult[]> ProcessFile(ProcessRequest request, IMessageBus messageBus, AppOptions appOptions)
    {
        var results = new ProcessFileResult[request.Files.Count];

        var parallelism =  request.MaxParallelism;
        if (!parallelism.HasValue)
        {
            parallelism = appOptions.Queue.MaxParallelism;
            if (parallelism > MaxParallelism.Value)
            {
                parallelism = MaxParallelism.Value;
            }
        }
        
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallelism.Value };
        await Parallel.ForAsync(0, request.Files.Count, options, async (index, token) =>
        {
            var file = request.Files[index];
            var result = await messageBus.InvokeAsync<ProcessFileResult>(new ProcessFileCommand(file, request.Command), token, appOptions.Process.Timeout);
            results[index] = result;
        });
        
        return results;
    }

    private static Task<ExecuteResult> ExecuteCommand(string command, IMessageBus messageBus, AppOptions appOptions)
    {
        return messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(command));
    }
    
    private static Task<ExecuteResult> ExecuteCommandPost([FromBody] ExecuteCommandRequest command, IMessageBus messageBus, AppOptions appOptions)
    {
        return messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(command.Command));
    }
}