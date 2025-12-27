namespace SidecarService.Core;

public static class CoreApi
{
    public static void MapCoreApis(this IEndpointRouteBuilder app)
    {
        app.Map("/", GetIndex);
        app.Map("/ping", () => "pong");
        app.Map("/options", GetAppOptions);
    }

    private static IResult GetIndex(AppOptions appOptions)
    {
        return Results.Content($"""
                                <!DOCTYPE html>
                                   <html lang=""en"">
                                   <head>
                                       <meta charset=""utf-8"">
                                       <title>{appOptions.Process.Name}</title>
                                   </head>
                                   <body>
                                       <h1>Sidecar service: {appOptions.Process.Name}</h1>

                                       <p>- Explore apis at <a href="/apis">/apis</a></p>
                                       <p>- Ping: <a href="/ping">/ping</a></p>
                                       <p>- Check version: <a href="/version">/version</a></p>
                                   </body>
                                   </html>
                                """, "text/html");
    }

    private static AppOptions GetAppOptions(AppOptions options) => options;
}