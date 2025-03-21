using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData.DbContexts;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddDbContext<HitomiContext>();
            //builder.Services.AddDbContext<ApplicationDbContext>();
            builder.Services.AddSignalR();
            string webAppUrl = builder.Configuration["webAppUrl"]!;
            builder.Services.AddCors(options => {
                options.AddPolicy("AllowLocalhostOrigins", builder =>
                    builder.WithOrigins(webAppUrl)
                        .SetIsOriginAllowed(host => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        //.AllowCredentials()
                        );
            });
            //builder.Services.AddAuthorization();
            //builder.Services.AddIdentityApiEndpoints<IdentityUser>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>();

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            } else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //app.UseStaticFiles();
            // app.UseCookiePolicy();

            app.UseRouting();
            // app.UseRateLimiter();
            // app.UseRequestLocalization();
            app.UseCors("AllowLocalhostOrigins");

            //app.UseAuthentication();
            //app.UseAuthorization();
            // app.UseSession();
            // app.UseResponseCompression(

            app.MapHub<DbStatusHub>("/api/initialize");
            app.MapControllers();

            //app.MapIdentityApi<IdentityUser>();

            //if (app.Environment.IsDevelopment()) {
            //    app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
            //        string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
            //}

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
