using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StageTrack.Web;
using StageTrack.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ApiBaseUrl (wwwroot/appsettings.json) : adresse de l'API StageTrack.
// Vide → mode démonstration en mémoire, utilisé pour la page publique sur GitHub Pages.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    builder.Services.AddSingleton<IStageTrackClient>(_ => new DemoStageTrackClient());
}
else
{
    var adresse = apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/";
    builder.Services.AddScoped<IStageTrackClient>(_ =>
        new HttpStageTrackClient(new HttpClient { BaseAddress = new Uri(adresse) }));
}

await builder.Build().RunAsync();
