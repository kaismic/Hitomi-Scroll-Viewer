
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI {
    public class Program {
        private const string MAIN_DATABASE_PATH = "main.db";

        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddDbContext<HitomiContext>(optionsBuilder =>
                optionsBuilder
                .UseSqlite($"Data Source={MAIN_DATABASE_PATH}")
                .EnableSensitiveDataLogging()
            );
            builder.Services.AddOpenApi();
            builder.Services.AddSignalR();
            builder.Services.AddCors(policy => {
                policy.AddPolicy("AllowLocalhostOrigins", builder =>
                    builder.WithOrigins("https://localhost:5214")
                        .SetIsOriginAllowed(host => true) // this for using localhost address
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseCors("AllowLocalhostOrigins");
            app.MapControllers();
            app.MapHub<DbStatusHub>("/api/initialize");

            app.Run();
        }
    }
}
