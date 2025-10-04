using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ApiAggregationService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("ApiAggregationService/appsettings.json", optional: false, reloadOnChange: true);
                });
                webBuilder.UseStartup<Startup>();
            });
}

