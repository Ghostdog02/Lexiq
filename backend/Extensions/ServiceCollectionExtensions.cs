using System.Text;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Backend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProtectionKeys(this IServiceCollection services)
    {
        var keyPath =
            Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH")
            ?? "/app/dataprotection-keys";

        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));

        return services;
    }

    public static IServiceCollection LimitFileUploads(this IServiceCollection services)
    {
        long maxFileSizeInBytes = 100 * 1024 * 1024; // 100 MB

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxFileSizeInBytes;
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        var angularOrigin =
            Environment.GetEnvironmentVariable("ANGULAR_PORT") ?? "http://localhost:4200";

        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAngular",
                policy =>
                {
                    policy
                        .WithOrigins(angularOrigin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
            );
        });

        return services;
    }

    public static IServiceCollection AddDatabaseContext(this IServiceCollection services)
    {
        var connectionString = BuildConnectionString();

        services.AddDbContext<BackendDbContext>(
            options =>
                options
                    .UseSqlServer(connectionString)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)),
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
        services.AddScoped<FileUploadsService>();
        services.AddScoped<IFileUploadsService>(sp => sp.GetRequiredService<FileUploadsService>());
        services.AddScoped<ExerciseProgressService>();
        services.AddScoped<UserXpService>();
        services.AddScoped<UserService>();
        services.AddScoped<LeaderboardService>();
        services.AddScoped<AvatarService>();
        services.AddScoped<AchievementService>();
        services.AddHttpClient(
            "GoogleAvatar",
            client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );

        services.AddOutputCache(options =>
        {
            options.AddPolicy(
                "OpenApiDocument",
                builder =>
                    builder
                        .Expire(TimeSpan.FromHours(1))
                        .SetVaryByHeader("Accept")
                        .SetVaryByQuery("version")
            );
            options.AddPolicy("Query", builder => builder.SetVaryByQuery("culture"));
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        var secretKey =
            Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("JWT_SECRET not found in environment variables");
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "lexiq-api";
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "lexiq-frontend";

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["AuthToken"];
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddScoped<IJwtService, JwtService>();

        return services;
    }

    public static IServiceCollection AddControllersWithOptions(this IServiceCollection services)
    {
        services.AddControllers();

        return services;
    }

    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services
            .AddIdentityCore<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BackendDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(
                (document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo()
                    {
                        Title = "Lexiq API",
                        Version = "v1",
                        Description =
                            "RESTful API for the Lexiq language learning platform. "
                            + "Supports Italian lessons for Bulgarian speakers with progress tracking, "
                            + "gamification (XP, levels, streaks), and leaderboards. "
                            + "\n\nAuthentication: JWT token in HttpOnly cookie (AuthToken). "
                            + "Login via Google OAuth at POST /api/auth/google-login. "
                            + "\n\nAll endpoints return standardized error responses: "
                            + "{ \"message\": \"...\", \"statusCode\": 400, \"detail\": null }. "
                            + "\n\nDocumentation: https://github.com/lexiq/docs",
                        Contact = new OpenApiContact()
                        {
                            Name = "Lexiq Team",
                            Email = "support@lexiq.com",
                        },
                        License = new OpenApiLicense()
                        {
                            Name = "MIT",
                            Url = new Uri("https://opensource.org/licenses/MIT"),
                        },
                    };

                    // Add servers
                    document.Servers =
                    [
                        new OpenApiServer
                        {
                            Url = "http://localhost:8080",
                            Description = "Development server",
                        },
                        new OpenApiServer
                        {
                            Url = "https://api.lexiqlanguage.eu",
                            Description = "Production server",
                        },
                    ];

                    // Add Cookie authentication scheme (actual auth method)
                    document.Components ??= new();

                    if (document.Components.SecuritySchemes == null)
                    {
                        document.Components.SecuritySchemes =
                            new Dictionary<string, IOpenApiSecurityScheme>();
                    }

                    document.Components.SecuritySchemes["CookieAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie,
                        Name = "AuthToken",
                        Description =
                            "JWT token stored in HttpOnly cookie. "
                            + "Obtain by calling POST /api/auth/google-login with Google OAuth token. "
                            + "Cookie is automatically sent with requests when using credentials: 'include' (fetch) "
                            + "or withCredentials: true (axios).",
                    };

                    // Add Bearer scheme for documentation purposes (though actual auth uses cookie)
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description =
                            "Alternative: Manually extract JWT from AuthToken cookie and send as Bearer token. "
                            + "Not recommended - use cookie authentication instead.",
                    };

                    // Add global security requirement (cookie auth)
                    document.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [
                                new OpenApiSecuritySchemeReference("CookieAuth")
                                {
                                    Description = "JWT token in HttpOnly cookie",
                                }
                            ] = [],
                        },
                    ];

                    // Add common tags
                    document.Tags = new HashSet<OpenApiTag>
                    {
                        new OpenApiTag
                        {
                            Name = "Authentication",
                            Description = "Google OAuth login, logout, and auth status",
                        },
                        new OpenApiTag
                        {
                            Name = "Languages",
                            Description = "Language management (Italian, etc.)",
                        },
                        new OpenApiTag { Name = "Courses", Description = "Course CRUD operations" },
                        new OpenApiTag { Name = "Lessons", Description = "Lesson management" },
                        new OpenApiTag
                        {
                            Name = "Exercises",
                            Description =
                                "Exercise management (MultipleChoice, FillInBlank, Listening, Translation)",
                        },
                        new OpenApiTag
                        {
                            Name = "Progress",
                            Description = "User progress tracking, XP, and submissions",
                        },
                        new OpenApiTag
                        {
                            Name = "Leaderboard",
                            Description = "Leaderboard rankings, streaks, levels",
                        },
                        new OpenApiTag
                        {
                            Name = "User Management",
                            Description = "Admin user and role management",
                        },
                        new OpenApiTag { Name = "Uploads", Description = "File upload operations" },
                        new OpenApiTag { Name = "Avatars", Description = "User avatar management" },
                    };

                    return Task.CompletedTask;
                }
            );
        });

        return services;
    }

    private static string BuildConnectionString()
    {
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return environment switch
        {
            "development" =>
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                    + $"Encrypt=False;TrustServerCertificate=True;Connection Timeout=30",

            "production" =>
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                    + $"Encrypt=True;TrustServerCertificate=True;Connection Timeout=30",

            _ => $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                + $"Encrypt=False;TrustServerCertificate=True;Connection Timeout=30",
        };
    }
}
