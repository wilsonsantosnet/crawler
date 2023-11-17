using Crawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

//https://csandunblogs.com/byte-sized-01-console-app-with-dependency-injection/

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostCtx, services) =>
    {
        var config = hostCtx.Configuration.GetSection("WorkerConfig");
        services.Configure<WorkerConfig>(config);
        services.AddSingleton<CrawlerService>();
        services.AddHttpClient();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
await services.GetRequiredService<CrawlerService>().Run(args);
