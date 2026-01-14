using Microsoft.AspNetCore.Mvc;
using SidecarService.Common;
using SidecarService.Core;
using Wolverine;

namespace SidecarService.Process;

public class ProcessRequest
{
    public required string Process { get; init; }

    public required string Command { get; init; }
    
    public IList<string>? Files { get; init; }
    
    /// <summary>
    /// Override the default max parallelism (75% of CPU)
    /// </summary>
    public int? MaxParallelism { get; set; }
    
    public TimeSpan? Timeout { get; set; }
}

public static class Process
{
    private static readonly Lazy<int> MaxParallelism = new(() => Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0)));
    
    public static void MapTesseractApis(this IEndpointRouteBuilder app)
    {
        app.MapGet("/execute", ProcessFileGet);
        app.MapPost("/execute", ProcessFile);
    }

    private static Task<IResult> ProcessFileGet(
        [FromQuery] string Process, [FromQuery] string Command,
        IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        return ProcessFile(new()
        {
            Process = Process,
            Command = Command
        }, messageBus, appOptions, loggerFactory);
    }

    private static async Task<IResult> ProcessFile(
        [FromBody] ProcessRequest request,
        IMessageBus messageBus, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        var files = request.Files;
        if (files is not null)
        {
            files = files.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
        
        if (files == null || files.Count == 0)
        {
            var result = await messageBus.InvokeAsync<ExecuteResult>(new ExecuteCommand(request.Process, request.Command));
            return Results.Ok(result);
        }
        
        var results = new ProcessFileResult[files.Count];

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
        
        await Parallel.ForAsync(0, files.Count, options, async (index, token) =>
        {
            try
            {
                var file = files[index];
                var result = await messageBus.InvokeAsync<ProcessFileResult>(new ProcessFileCommand(request.Process, request.Command, file, request.Timeout), token);
                results[index] = result;
            }
            catch (Exception ex)
            {
                results[index] = new ProcessFileResult(false, Error: $"Error processing file: {ex.Message}");
            }
        });
            
        return Results.Ok(results);
    }
}