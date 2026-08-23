using Resume.BlazorApp.Client.Services;
using Resume.BlazorApp.Components;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient<ResumeApiClient>(client =>
    client.BaseAddress = new Uri("https+http://api/api/"));

builder.Services.AddReverseProxy()
    .AddServiceDiscoveryDestinationResolver()
    .LoadFromMemory(
        [
            new RouteConfig
            {
                RouteId = "api",
                ClusterId = "api-cluster",
                Match = new RouteMatch { Path = "/api/{**catch-all}" }
            }
        ],
        [
            new ClusterConfig
            {
                ClusterId = "api-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["destination1"] = new DestinationConfig { Address = "https+http://api" }
                }
            }
        ]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Resume.BlazorApp.Client._Imports).Assembly);

app.Run();
