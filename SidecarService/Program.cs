using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using SidecarService;
using SidecarService.Core;
using SidecarService.Process;
using Wolverine;

var builder = WebApplication.CreateSlimBuilder(args);

var configuration = builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(prefix: "Sidecar_")
    .AddCommandLine(args)
    .Build();

var appOptions = configuration.Get<AppOptions>()!;
builder.Services.AddSingleton(appOptions);

builder.Services.AddOpenApi(); 

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolver = AppJsonSerializerContext.Default;
});

builder.Host.UseWolverine(options =>
{
    options.LocalQueueFor<ProcessFileCommand>()
        .MaximumParallelMessages(appOptions.Queue.MaxParallelism)
        .UseDurableInbox();
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference("/apis", (options) =>
{
    options.WithTitle($"{appOptions.Process.Name} service")
        .HideSearch();
});

app.MapCoreApis();
app.MapTesseractApis();
app.Run();

namespace SidecarService
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ProcessRequest))]
    [JsonSerializable(typeof(ProcessFileResult[]))]
    [JsonSerializable(typeof(AppOptions))]
    [JsonSerializable(typeof(Common.ExecuteResult))]
    [JsonSerializable(typeof(ExecuteCommandRequest))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
