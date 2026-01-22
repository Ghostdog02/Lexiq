using System.Reflection;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200", "https://localhost:5000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }

    public static IServiceCollection AddDatabaseContext(this IServiceCollection services)
    {
        var connectionString = BuildConnectionString();

        services.AddDbContext<BackendDbContext>(
            options => options.UseSqlServer(connectionString),
            ServiceLifetime.Scoped
        );

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<CourseService>();
        services.AddScoped<LessonService>();
        services.AddScoped<LanguageService>();
        services.AddScoped<ExerciseService>();
        services.AddScoped<UserLanguageService>();
        services.AddScoped<IProgressService, ProgressService>();
        return services;
    }

    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(
                Microsoft
                    .AspNetCore
                    .Authentication
                    .Cookies
                    .CookieAuthenticationDefaults
                    .AuthenticationScheme
            )
            .AddCookie(options =>
            {
                options.Cookie.Name = "LexiqAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;
            });

        return services;
    }

    public static IServiceCollection AddControllersWithOptions(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

        return services;
    }

    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services
            .AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BackendDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services)
    {
        var googleClientId =
            Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? throw new InvalidOperationException(
                "GOOGLE_CLIENT_ID not found in environment variables"
            );

        var googleClientSecret =
            Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? throw new InvalidOperationException(
                "GOOGLE_CLIENT_SECRET not found in environment variables"
            );

        services
            .AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
            });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Lexiq API",
                    Version = "v1",
                    Description = "Interactive API documentation for Lexiq",
                    Contact = new OpenApiContact
                    {
                        Name = "Lexiq Team",
                        Email = "support@lexiq.com",
                    },
                }
            );

            AddXmlDocumentation(c);
            AddBearerAuthentication(c);
        });

        return services;
    }

    private static string BuildConnectionString()
    {
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        return $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
            + $"Encrypt=True;TrustServerCertificate=True;Connection Timeout=30";
    }

    private static void AddXmlDocumentation(SwaggerGenOptions options)
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }

    private static void AddBearerAuthentication(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. "
                    + "Enter 'Bearer' [space] and then your token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            }
        );
    }
}
