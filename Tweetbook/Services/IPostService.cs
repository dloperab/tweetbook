using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Tweetbook.Domain;

namespace Tweetbook.Services
{
  public interface IPostService
  {
    Task<List<Post>> GetAllAsync();
    Task<Post> GetByIdAsync(Guid id);
    Task<bool> CreateAsync(Post post);
    Task<bool> UpdateAsync(Post post);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> IsUserOwnPostAsync(Guid postId, string userId);
    Task<List<Tag>> GetAllTagsAsync();
    Task<bool> CreateTagAsync(Tag tag);
    Task<Tag> GetTagByNameAsync(string tagName);
    Task<bool> DeleteTagAsync(string tagName);
  }
}
