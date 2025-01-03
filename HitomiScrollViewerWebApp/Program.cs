using HitomiScrollViewerWebApp.Components;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Task runTask = app.RunAsync();
OpenBrowser(app.Urls.First());
runTask.Wait();

static int FreeTcpPort() {
    TcpListener l = new(IPAddress.Loopback, 0);
    l.Start();
    int port = ((IPEndPoint)l.LocalEndpoint).Port;
    l.Stop();
    return port;
}

static void OpenBrowser(string url) {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        Process.Start("xdg-open", url);
    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        Process.Start("open", url);
    }
}