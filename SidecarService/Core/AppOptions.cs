namespace SidecarService.Core;

public class AppOptions
{
    public required string DataFolderPath { get; init; }
    
    public required AppQueueOptions Queue { get; init; }
    
    public required AppProcessOptions Process { get; init; }
    
}
public class AppQueueOptions
{
    public int MaxParallelism { get; init; }
}

public class AppProcessOptions
{
    public required string Name { get; init; }
    
    public required string Command { get; init; }
    
    public required string VersionCommand { get; init; }
    
    public TimeSpan Timeout { get; init; }
    
    public required int[] Retries { get; init; }
}
