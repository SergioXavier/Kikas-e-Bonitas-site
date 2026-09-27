using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KikasEBonitas.Web;
using KikasEBonitas.Web.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Registar o handler
builder.Services.AddScoped<JwtAuthenticationHandler>();

// 2. Configurar o HttpClient para usar o handler (apontando para a porta da API, ex: 5287)
builder.Services.AddHttpClient("KikasApi", client => client.BaseAddress = new Uri("http://localhost:5287/"))
    .AddHttpMessageHandler<JwtAuthenticationHandler>();

// 3. Fazer com que o HttpClient padrão utilize também esta configuração
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("KikasApi"));

await builder.Build().RunAsync();
