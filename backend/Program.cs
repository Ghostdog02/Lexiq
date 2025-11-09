using System.Reflection;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.ExtensionClasses;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Backend.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "lexiq";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID") ?? "sa";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString =
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            builder.Services.AddDbContext<BackendDbContext>(
                options => options.UseSqlServer(connectionString),
                ServiceLifetime.Scoped
            );

            builder
                .Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

            builder
                .Services.AddIdentity<User, IdentityRole<int>>(options =>
                    options.SignIn.RequireConfirmedAccount = true
                )
                .AddRoles<IdentityRole<int>>()
                .AddEntityFrameworkStores<BackendDbContext>()
                .AddDefaultTokenProviders();

            builder
                .Services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId =
                        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                        ?? throw new Exception("GOOGLE_CLIENT_ID not found");
                    googleOptions.ClientSecret =
                        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                        ?? throw new Exception("GOOGLE_CLIENT_SECRET not found");
                    googleOptions.CallbackPath = "/signin-google"; // Default callback path
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
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

                // Enable XML comments for better documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add JWT Authentication to Swagger UI
                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description =
                            "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer",
                                },
                            },
                            Array.Empty<string>()
                        },
                    }
                );
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Serves /swagger/v1/swagger.json
                app.UseSwagger();

                // Hosts Swagger UI at /swagger
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lexiq API V1");
                    c.RoutePrefix = string.Empty;

                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all by default
                    c.EnableDeepLinking();
                    c.DisplayRequestDuration();
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            var seeder = new SeedData();

            await app.Services.MigrateDbAsync();
            await SeedData.InitializeAsync(app.Services);

            app.Run();
        }
    }
}
