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

        string apiUrl = builder.Configuration["apiUrl"]!;
        builder.Services.AddSingleton(new ApiUrlService(apiUrl));
        builder.Services.AddHttpClient<TagFilterService>(client => client.BaseAddress = new Uri(apiUrl));
        builder.Services.AddHttpClient<SearchFilterService>(client => client.BaseAddress = new Uri(apiUrl));
        builder.Services.AddHttpClient<TagService>(client => client.BaseAddress = new Uri(apiUrl));
        builder.Services.AddHttpClient<GalleryService>(client => client.BaseAddress = new Uri(apiUrl));
        builder.Services.AddHttpClient<SearchService>(client => client.BaseAddress = new Uri(apiUrl));
        builder.Services.AddSingleton<PageConfigurationService>();

        var app = builder.Build();
        await app.RunAsync();
    }
}
