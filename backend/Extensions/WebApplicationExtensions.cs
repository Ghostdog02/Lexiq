namespace Backend.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseSwaggerWithUI(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lexiq API V1");
            c.RoutePrefix = string.Empty;
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            c.EnableDeepLinking();
            c.DisplayRequestDuration();
        });

        return app;
    }

    public static WebApplication ConfigureHttpPort(this WebApplication app)
    {
        var url = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrEmpty(url))
        {
            app.Urls.Add(url);
        }

        return app;
    }

    public static WebApplication ConfigureStaticFiles(this WebApplication app)
    {
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                ),
                RequestPath = "/uploads",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
                    ctx.Context.Response.Headers.AccessControlAllowOrigin =
                        Environment.GetEnvironmentVariable("ANGULAR_PORT");
                },
            }
        );

        return app;
    }
}
