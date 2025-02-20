
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData.DbContexts;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddDbContext<HitomiContext>();
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

            Task appTask = app.RunAsync();
            Task.Run(() => {
                if (OperatingSystem.IsWindows()) {
                    Console.BufferWidth = 80;
                    Console.WindowWidth = 80;
                }
                DatabaseInitializer dbInitializer = new(app.Services.GetRequiredService<IHubContext<DbStatusHub, IStatusClient>>());
                dbInitializer.Start();
            });
            appTask.Wait();
        }
    }
}
