using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

using Tweetbook.Data;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;

namespace Tweetbook.IntegrationTests
{
  public class IntegrationTest : IDisposable
  {
    protected readonly HttpClient TestClient;
    private readonly IServiceProvider _serviceProvider;

    protected IntegrationTest()
    {
      var appFactory = new WebApplicationFactory<Tweetbook.Startup>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureServices(services =>
          {
            services.RemoveAll(typeof(DbContextOptions<DataContext>));
            services.AddDbContext<DataContext>(options => { options.UseInMemoryDatabase("TestDb"); });
          });
        });

      _serviceProvider = appFactory.Services;
      TestClient = appFactory.CreateClient();
    }

    public void Dispose()
    {
      using var serviceScope = _serviceProvider.CreateScope();
      var context = serviceScope.ServiceProvider.GetService<DataContext>();
      context.Database.EnsureDeleted();
    }

    protected async Task AuthenticateAsync()
    {
      TestClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("bearer", await GetJwtAsync());
    }

    protected async Task<PostResponse> CreatePostAsync(CreatePostRequest request)
    {
      var response = await TestClient.PostAsJsonAsync(ApiRoutes.Posts.Create, request);

      return await response.Content.ReadAsAsync<PostResponse>();
    }

    private async Task<string> GetJwtAsync()
    {
      var response = await TestClient.PostAsJsonAsync(
        ApiRoutes.Identity.Register,
        new UserRegistrationRequest
        {
          Email = "test@integration.com",
          Password = "SomePass1234!"
        }
      );

      var registrationResponse = await response.Content.ReadAsAsync<AuthSuccessResponse>();

      return registrationResponse.Token;
    }
  }
}
