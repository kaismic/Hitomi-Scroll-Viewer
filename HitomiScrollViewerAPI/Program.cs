using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData.DbContexts;

namespace HitomiScrollViewerAPI {
    public class Program {
        private const int MIN_CONSOLE_WIDTH = 80;
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddDbContext<HitomiContext>();
            //builder.Services.AddDbContext<ApplicationDbContext>();
            builder.Services.AddSignalR();
            string webAppUrl = builder.Configuration["WebAppUrl"]!;
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

            builder.Services.AddHttpClient();
            builder.Services.AddHostedService<DbInitializeService>();
            //builder.Services.AddHostedService<DownloadManagerService>();
            builder.Services.AddSingleton<DownloadManagerService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<DownloadManagerService>());
            builder.Services.AddSingleton<IEventBus<DownloadEventArgs>, DownloadEventBus>();

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

            app.MapHub<DbInitializeHub>("api/db-initialize-hub");
            app.MapHub<DownloadHub>("api/download-hub");
            app.MapControllers();

            //app.MapIdentityApi<IdentityUser>();

            //if (app.Environment.IsDevelopment()) {
            //    app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
            //        string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
            //}
            if (OperatingSystem.IsWindows()) {
                try {
                    if (Console.WindowWidth < MIN_CONSOLE_WIDTH) {
                        Console.WindowWidth = MIN_CONSOLE_WIDTH;
                    }
                } catch (IOException) {
                    // setting console width in powershell throws IOException
                }
            }
            app.Run();
        }
    }
}
