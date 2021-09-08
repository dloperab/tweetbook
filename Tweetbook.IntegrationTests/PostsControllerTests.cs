using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using FluentAssertions;

using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Domain;
using Tweetbook.Contracts.V1.Responses;

namespace Tweetbook.IntegrationTests
{
  public class PostsControllerTests : IntegrationTest
  {
    [Fact]
    public async Task GetAll_WithoutAnyPost_ReturnsEmptyResponse()
    {
      // Arrange
      await AuthenticateAsync();

      // Act
      var response = await TestClient.GetAsync(ApiRoutes.Posts.GetAll);

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      var returnedPosts = await response.Content.ReadAsAsync<List<Post>>();
      returnedPosts.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsPost_WhenPostExistsInDatabase()
    {
      // Arrange
      await AuthenticateAsync();
      var postName = "Test Post";
      var postTag = "testtag";
      var createdPost = await CreatePostAsync(new CreatePostRequest 
      { 
        Name = postName,
        Tags = new[] { postTag }
      });

      // Act
      var response = await TestClient.GetAsync(
        ApiRoutes.Posts.Get.Replace("{postId}", createdPost.Id.ToString()));

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
      var returnedPost = await response.Content.ReadAsAsync<PostResponse>();
      returnedPost.Id.Should().Be(createdPost.Id);
      returnedPost.Name.Should().Be(postName);
      returnedPost.Tags.Single().Name.Should().Be(postTag);
    }
  }
}
