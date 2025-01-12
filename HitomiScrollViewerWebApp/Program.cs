using HitomiScrollViewerWebApp.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HitomiScrollViewerWebApp {
    public class Program {
        public static void Main(string[] args) {
            Task.Delay(1000).Wait();
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddFluentUIComponents();
            

            Task.Delay(1000).Wait();
            var app = builder.Build();

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