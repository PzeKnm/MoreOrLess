using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoreOrLess
{
  public class VisualisationSignalRServer
  {
    public void RunServer(CancellationToken ct)
    {
      string[] args = new string[0];
      CreateWebHostBuilder(args).Build().RunAsync(ct);
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>();

    public void PublishVisualisationData(VisualisationData vd)
    {
      theWorker.PublishVisualisationData(vd);
    }

    public static Worker theWorker;
  }



  public class Startup
  {
      public void ConfigureServices(IServiceCollection services)
      {
          services.AddCors();
          services.AddSignalR();
          services.AddHostedService<Worker>(); 
      }

      public void Configure(IApplicationBuilder app, 
        Microsoft.AspNetCore.Hosting.IHostingEnvironment env, IHostedService wkr)
      {
          if (env.IsDevelopment())
          {
              app.UseDeveloperExceptionPage();
          }

          app.UseCors(builder =>
          {
            // TODO add other origins here
            builder.WithOrigins(
              "http://localhost:4200",
              "https://sandgatetreasurehunt.azurewebsites.net")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
          });

          app.UseSignalR((routes) =>
          {
              routes.MapHub<VisualisationHub>("/hubs/visualisation");
          });
                
 
          VisualisationSignalRServer.theWorker = (Worker)wkr;
      }
  }

  public class Worker : BackgroundService
  {
      private readonly ILogger<Worker> _logger;
      private readonly IHubContext<VisualisationHub, IVisualisation> _visualisationHub;

      public Worker(ILogger<Worker> logger, IHubContext<VisualisationHub, IVisualisation> visualisationHub)
      {
          _logger = logger;
          _visualisationHub = visualisationHub;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
          while (!stoppingToken.IsCancellationRequested)
          {
            //  _logger.LogInformation("Worker running at: {Time}", DateTime.Now);
            //  await _visualisationHub.Clients.All.ShowTime(DateTime.Now);
            //  await _visualisationHub.Clients.All.SendFoobar("Foo and Bar");
              await Task.Delay(1000);
          }
      }

      public void PublishVisualisationData(VisualisationData vd)
      {
        _visualisationHub.Clients.All.SendVisualisation(vd);
      }
  }
  
  public class VisualisationHub : Hub<IVisualisation>
  {
  }
  
  public interface IVisualisation
  {
      Task SendVisualisation(VisualisationData vd);
  }

}
