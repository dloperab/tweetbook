using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using FluentValidation.AspNetCore;

using Tweetbook.Infrastructure;
using Tweetbook.Services;
using Tweetbook.Filters;

namespace Tweetbook.Installers
{
  public class MvcInstaller : IInstaller
  {
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
      var jwtSettings = new JwtSettings();
      configuration.Bind(nameof(jwtSettings), jwtSettings);
      services.AddSingleton(jwtSettings);

      services.AddScoped<IIdentityService, IdentityService>();

      services
        .AddMvc(options => 
          { 
            options.EnableEndpointRouting = false;
            options.Filters.Add<ValidationFilter>();
          })
        .AddFluentValidation(config => config.RegisterValidatorsFromAssemblyContaining<Startup>());

      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = false,
        ValidateLifetime = true
      };

      services.AddSingleton(tokenValidationParameters);

      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
        .AddJwtBearer(options =>
        {
          options.SaveToken = true;
          options.TokenValidationParameters = tokenValidationParameters;
        });

      services.AddAuthorization();
    }
  }
}