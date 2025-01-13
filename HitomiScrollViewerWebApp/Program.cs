using HitomiScrollViewerWebApp.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace HitomiScrollViewerWebApp {
    public class Program {
        private static readonly string[] SUPPORTED_CULTURES = ["en", "ko"];

        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddFluentUIComponents();
            builder.Services.AddLocalization();

            var app = builder.Build();

            app.UseRequestLocalization(new RequestLocalizationOptions() {
                SupportedCultures = SUPPORTED_CULTURES.Select(culture => new CultureInfo(culture)).ToList(),
                SupportedUICultures = SUPPORTED_CULTURES.Select(culture => new CultureInfo(culture)).ToList()
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            Task appTask = app.RunAsync();
            OpenBrowser(app.Urls.First());
            appTask.Wait();
        }
        private static void OpenBrowser(string url) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Process.Start("xdg-open", url);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Process.Start("open", url);
            }
        }
    }
}