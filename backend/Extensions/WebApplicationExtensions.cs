using Backend.Database;
using Backend.Database.Extensions;
using Microsoft.EntityFrameworkCore;

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

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                context.Response.Headers.AccessControlAllowOrigin = "*";
                context.Response.Headers.ContentSecurityPolicy =
                    "default-src 'self'; "
                    + "connect-src 'self' https://localhost:4200 ws://localhost:4200; "
                    + "script-src 'self'; "
                    + "style-src 'self' 'unsafe-inline'";

                await next();
            }
        );

        return app;
    }
}
