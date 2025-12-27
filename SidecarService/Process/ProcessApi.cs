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
    
    /// <summary>
    /// Override the default timeout
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

public record ExecuteCommandRequest(string Command, TimeSpan? Timeout);

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

    private static async Task<IResult> GetVersion(IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        var result = await messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(appOptions.Process.VersionCommand, appOptions.Process.Timeout));
        return Results.Ok(result);
    }
    
    private static async Task<IResult> ProcessSingleFile(
        [FromQuery(Name = "file")] string file,
        IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return Results.BadRequest(new { error = "File is required" });
        }

        var result = await messageBus.InvokeAsync<ProcessFileResult>(new ProcessFileCommand(file, null, null), CancellationToken.None, appOptions.Process.Timeout);
        return Results.Ok(result);
    }

    private static async Task<IResult> ProcessFile(ProcessRequest request, IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        var results = new ProcessFileResult[request.Files.Count];

        var parallelism = request.MaxParallelism;
        if (!parallelism.HasValue)
        {
            parallelism = appOptions.Queue.MaxParallelism;
            if (parallelism > MaxParallelism.Value)
            {
                parallelism = MaxParallelism.Value;
            }
        }
        if (parallelism.Value < 1) parallelism = 1;
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallelism.Value };
        
        await Parallel.ForAsync(0, request.Files.Count, options, async (index, token) =>
        {
            try
            {
                var file = request.Files[index];
                var result = await messageBus.InvokeAsync<ProcessFileResult>(new ProcessFileCommand(file, request.Command, request.Timeout), token);
                results[index] = result;
            }
            catch (Exception ex)
            {
                results[index] = new ProcessFileResult(false, Error: $"Error processing file: {ex.Message}");
            }
        });
            
        return Results.Ok(results);
    }

    private static async Task<IResult> ExecuteCommand(string command, IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        var result = await messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(command));
        return Results.Ok(result);
    }
    
    private static async Task<IResult> ExecuteCommandPost([FromBody] ExecuteCommandRequest command, IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        var result = await messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(command.Command, command.Timeout));
        return Results.Ok(result);
    }
}