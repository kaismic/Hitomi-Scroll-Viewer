using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace HitomiScrollViewerWebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddMudServices();

        // TODO change uri to the api address in appsettings.json
        // TODO or add service and use dependency injection
        //builder.Configuration.GetValue<string>("applicationUrl");
        builder.Services.AddHttpClient<TagFilterService>(client => client.BaseAddress = new Uri("https://localhost:7076/"));
        builder.Services.AddHttpClient<TagService>(client => client.BaseAddress = new Uri("https://localhost:7076/"));
        var app = builder.Build();
        await app.RunAsync();
    }
}
