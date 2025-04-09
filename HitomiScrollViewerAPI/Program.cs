using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData.DbContexts;

namespace HitomiScrollViewerAPI {
    public class Program {
        private const int MIN_CONSOLE_WIDTH = 80;
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton(new HitomiUrlService() {
                HitomiMainDomain = builder.Configuration["HitomiMainDomain"]!,
                HitomiServerInfoDomain = builder.Configuration["HitomiServerInfoDomain"]!
            });
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
            builder.Services.AddHostedService<DownloadManagerService>();
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

            app.MapHub<DbStatusHub>("/api/initialize");
            app.MapHub<DownloadHub>("/api/download");
            app.MapControllers();

            //app.MapIdentityApi<IdentityUser>();

            //if (app.Environment.IsDevelopment()) {
            //    app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
            //        string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
            //}
            if (OperatingSystem.IsWindows()) {
                // only set console width when the console is Command Prompt because setting it in powershell throws IOException
                if (Console.Title.Contains("Command Prompt", StringComparison.InvariantCultureIgnoreCase)) {
                    if (Console.BufferWidth < MIN_CONSOLE_WIDTH) {
                        Console.BufferWidth = 80;
                    }
                    if (Console.WindowWidth < MIN_CONSOLE_WIDTH) {
                        Console.WindowWidth = 80;
                    }
                }
            }
            app.Run();
        }
    }
}
