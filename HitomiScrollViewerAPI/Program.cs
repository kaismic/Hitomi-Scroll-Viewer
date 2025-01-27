
using HitomiScrollViewerData.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerAPI {
    public class Program {
        private const string MAIN_DATABASE_PATH = "full-main.db";

        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            //string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<HitomiContext>(optionsBuilder =>
                optionsBuilder
                .UseSqlite($"Data Source={MAIN_DATABASE_PATH}")
                .EnableSensitiveDataLogging()
            );
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
