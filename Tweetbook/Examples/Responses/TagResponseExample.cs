using Swashbuckle.AspNetCore.Filters;

using Tweetbook.Contracts.V1.Responses;

namespace Tweetbook.Examples.Responses
{
  public class TagResponseExample : IExamplesProvider<TagResponse>
  {
    public TagResponse GetExamples()
    {
      return new TagResponse
      {
        Name = "new tag"
      };
    }
  }
}
