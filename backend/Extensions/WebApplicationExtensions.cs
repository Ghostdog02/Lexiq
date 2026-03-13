using Backend.Api.Middleware;
using DotNetEnv;
using Microsoft.OpenApi;

namespace Backend.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseOpenApiEndpoint(this WebApplication app)
    {
        app.UseOutputCache();

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            // Maps the OpenAPI JSON endpoint at /openapi/v1.json
            app.MapOpenApi();
        }

        return app;
    }

    public static WebApplication ConfigureStaticFiles(this WebApplication app)
    {
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")
                ),
                RequestPath = "/static/uploads",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append(
                        "Access-Control-Allow-Methods",
                        "GET, OPTIONS"
                    );
                    ctx.Context.Response.Headers.Append(
                        "Cross-Origin-Resource-Policy",
                        "cross-origin"
                    );
                    ctx.Context.Response.Headers.Append(
                        "Cache-Control",
                        "public, max-age=31536000"
                    );
                },
            }
        );

        return app;
    }

    public static WebApplication UseUserContext(this WebApplication app)
    {
        app.UseMiddleware<UserContextMiddleware>();
        return app;
    }
}
