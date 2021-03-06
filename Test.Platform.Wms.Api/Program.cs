using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Test.Platform.Wms.Orleans.Grains.Implementations;
using Test.Platform.Wms.Sql.Contexts;

namespace Test.Platform.Wms.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            CreateEfDb<InventoryContext>(host);
            host.Run();
        }

        public static void CreateEfDb<TContext>(IHost host)
            where TContext: DbContext
        {
            using var scope = host.Services.CreateScope();
            
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<TContext>();
                    
                context.Database.EnsureCreated();

                if (context is InventoryContext inventoryContext)
                {
                    InventoryContextSeeder.Init(inventoryContext);
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                    
                logger.LogError(ex, "An error occurred creating the DB.");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration( (context, builder) => {
                    if(context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets("Test.Platform.Wms", true);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseOrleans((context, siloBuilder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        siloBuilder.UseLocalhostClustering();
                    }

                    siloBuilder.Configure<ClusterOptions>(opt =>
                    {
                        opt.ClusterId = context.HostingEnvironment.IsDevelopment() ? "dev" : "prod";
                        opt.ServiceId = "Test.Platform.Wms.Inventory";
                    });

                    siloBuilder.AddAzureBlobGrainStorage("inventoryStorage", opt => {
                        opt.ContainerName = "inventory";
                        opt.UseJson = true;
                        opt.ConnectionString = context.Configuration.GetConnectionString("Blob");
                    });

                    siloBuilder.ConfigureApplicationParts(manager =>
                    {
                        manager.AddApplicationPart(typeof(InventoryGrain).Assembly).WithReferences();
                    });

                });
    }
}