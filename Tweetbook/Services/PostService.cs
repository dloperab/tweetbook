using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Tweetbook.Data;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
  public class PostService : IPostService
  {
    private readonly DataContext _dataContext;

    public PostService(DataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task<List<Post>> GetAllAsync()
    {
      return await _dataContext.Posts
        .Include(x => x.Tags)
        .ToListAsync();
    }

    public async Task<Post> GetByIdAsync(Guid id)
    {
      return await _dataContext.Posts
        .Include(x => x.Tags)
        .SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> CreateAsync(Post post)
    {
      post.Tags?.ForEach(x => x.TagName = x.TagName.ToLower());

      await AddNewTags(post);
      await _dataContext.AddAsync(post);
      var created = await _dataContext.SaveChangesAsync();

      return created > 0;
    }

    public async Task<bool> UpdateAsync(Post post)
    {
      post.Tags?.ForEach(x => x.TagName = x.TagName.ToLower());
      await AddNewTags(post);
      _dataContext.Posts.Update(post);
      var updated = await _dataContext.SaveChangesAsync();

      return updated > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
      var post = await GetByIdAsync(id);

      if (post == null)
        return false;

      _dataContext.Posts.Remove(post);
      var deleted = await _dataContext.SaveChangesAsync();

      return deleted > 0;
    }

    public async Task<bool> IsUserOwnPostAsync(Guid postId, string userId)
    {
      var userPost = await _dataContext.Posts.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == postId && x.UserId == userId);

      return (userPost is not null);
    }

    public async Task<List<Tag>> GetAllTagsAsync()
    {
      return await _dataContext.Tags.AsNoTracking().ToListAsync();
    }

    public async Task<bool> CreateTagAsync(Tag tag)
    {
      tag.Name = tag.Name.ToLower();
      var existingTag = await _dataContext.Tags.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Name == tag.Name);
      if (existingTag != null)
        return true;

      await _dataContext.Tags.AddAsync(tag);
      var created = await _dataContext.SaveChangesAsync();
      
      return created > 0;
    }

    public async Task<Tag> GetTagByNameAsync(string tagName)
    {
      return await _dataContext.Tags.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Name == tagName.ToLower());
    }

    public async Task<bool> DeleteTagAsync(string tagName)
    {
      var tag = await _dataContext.Tags.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Name == tagName.ToLower());

      if (tag == null)
        return true;

      var postTags = await _dataContext.PostTags.Where(x => x.TagName == tagName.ToLower())
        .ToListAsync();

      _dataContext.PostTags.RemoveRange(postTags);
      _dataContext.Tags.Remove(tag);

      return await _dataContext.SaveChangesAsync() > postTags.Count;
    }

    private async Task AddNewTags(Post post)
    {
      foreach (var tag in post.Tags)
      {
        var existingTag =
            await _dataContext.Tags.SingleOrDefaultAsync(x =>
                x.Name == tag.TagName);
        if (existingTag != null)
          continue;

        await _dataContext.Tags.AddAsync(new Tag
          { Name = tag.TagName, CreatedOn = DateTime.UtcNow, CreatorId = post.UserId });
      }
    }
  }
}
