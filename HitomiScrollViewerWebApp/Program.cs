using HitomiScrollViewerWebApp.Components;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HitomiScrollViewerWebApp {
    public static class Program {
        public static WebApplication CreateWebApplication() {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(["http://localhost:" + FreeTcpPort()]);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();


            return app;
        }

        public static void Main(string[] args) {
            var app = CreateWebApplication();
            Task runTask = app.RunAsync();
            OpenBrowser(app.Urls.First());
            runTask.Wait();
        }

        public static int FreeTcpPort() {
            TcpListener l = new(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static void OpenBrowser(string url) {
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

//string localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//Console.WriteLine(localPath);
//Directory.CreateDirectory(Path.Combine(localPath, "Hitomi-server-naniiii"));


//app.Run();
