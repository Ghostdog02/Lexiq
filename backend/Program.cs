using DevGuard.Database;
using DevGuard.Database.Entities;
using DevGuard.Database.ExtensionClasses;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace DevGuard.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "lexiqdatabase";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID") ?? "sa";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString =
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;";

            builder.Services.AddDbContext<LexiqDbContext>(
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
                .AddEntityFrameworkStores<LexiqDbContext>()
                .AddDefaultTokenProviders();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "My API",
                        Version = "v1",
                        Description = "Interactive API docs",
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    c.RoutePrefix = string.Empty; // Serve UI at root (optional)
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            var seeder = new SeedData();

            //await app.Services.MigrateDbAsync();
            await SeedData.InitializeAsync(app.Services);

            app.Run();
        }
    }
}
