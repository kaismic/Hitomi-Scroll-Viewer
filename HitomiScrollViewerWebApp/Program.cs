using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

namespace HitomiScrollViewerWebApp {
    public class Program {
        public static async Task Main(string[] args) {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddFluentUIComponents();
            builder.Services.AddSingleton<DatabaseInitializer>();



            var app = builder.Build();

            Task appTask = app.RunAsync();
            DatabaseInitializer dbInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
            dbInitializer.StartAsync();
            await appTask;
        }
    }
}
