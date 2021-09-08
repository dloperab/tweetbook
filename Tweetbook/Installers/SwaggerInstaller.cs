using System;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Filters;

namespace Tweetbook.Installers
{
  public class SwaggerInstaller : IInstaller
  {
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
      services.AddSwaggerGen(options =>
      {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tweetbook API", Version = "v1" });

        options.ExampleFilters();

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
          Name = "Authorization",
          Description = "JWT Authorization header using the bearer scheme",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
              }
            },
            Array.Empty<string>()
          }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
      });

      services.AddSwaggerExamplesFromAssemblyOf<Startup>();
    }
  }
}
