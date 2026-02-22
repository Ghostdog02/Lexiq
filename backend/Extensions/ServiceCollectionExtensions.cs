using System.Reflection;
using System.Text;
using Backend.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProtectionKeys(this IServiceCollection services)
    {
        var keyPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/app/dataprotection-keys";

        services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

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
            options => options
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
        services
            .AddControllers();

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
