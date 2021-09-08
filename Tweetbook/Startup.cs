using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Tweetbook.Installers;
using Tweetbook.Infrastructure;

namespace Tweetbook
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.InstallServicesInAssembly(Configuration);
      services.AddAutoMapper(typeof(Startup));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseAuthentication();

      var swaggerSettings = new SwaggerSettings();
      Configuration.GetSection(nameof(SwaggerSettings)).Bind(swaggerSettings);
      app.UseSwagger(options => { options.RouteTemplate = swaggerSettings.JsonRoute; });
      app.UseSwaggerUI(options => 
      { 
        options.SwaggerEndpoint(swaggerSettings.UIEndpoint, swaggerSettings.Description); 
      });

      app.UseMvc();
    }
  }
}
