using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Resume.BlazorApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient<ResumeApiClient>(client =>
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"));

await builder.Build().RunAsync();
