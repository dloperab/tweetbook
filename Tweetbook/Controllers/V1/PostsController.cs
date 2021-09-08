using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using AutoMapper;

using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extensions;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
  /// <summary>
  /// Posts API
  /// </summary>
  /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  public class PostsController : Controller
  {
    private readonly IPostService _postService;
    private readonly IMapper _mapper;

    public PostsController(IPostService postService, IMapper mapper)
    {
      _postService = postService;
      _mapper = mapper;
    }

    [HttpGet(ApiRoutes.Posts.GetAll)]
    public async Task<IActionResult> GetAll()
    {
      var posts = await _postService.GetAllAsync();

      return Ok(_mapper.Map<List<PostResponse>>(posts));
    }

    [HttpGet(ApiRoutes.Posts.Get)]
    public async Task<IActionResult> GetById([FromRoute] Guid postId)
    {
      var post = await _postService.GetByIdAsync(postId);
      if (post == null)
        return NotFound();

      return Ok(_mapper.Map<PostResponse>(post));
    }

    [HttpPost(ApiRoutes.Posts.GetAll)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
      var newPostId = Guid.NewGuid();
      var post = new Post
      {
        Id = newPostId,
        Name = request.Name,
        UserId = HttpContext.GetUserId(),
        Tags = request.Tags.Select(x => new PostTag { PostId = newPostId, TagName = x }).ToList()
      };

      var created = await _postService.CreateAsync(post);

      var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
      var locationUri = baseUrl + "/" + ApiRoutes.Posts.Get.Replace("{postId}", post.Id.ToString());

      return Created(locationUri, _mapper.Map<PostResponse>(post));
    }

    [HttpPut(ApiRoutes.Posts.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostRequest request)
    {
      var userOwnPost = await _postService.IsUserOwnPostAsync(postId, HttpContext.GetUserId());
      if (!userOwnPost)
        return BadRequest(new { Errors = "You do not own this post" });

      var post = await _postService.GetByIdAsync(postId);
      post.Name = request.Name;
      var updated = await _postService.UpdateAsync(post);
      if (updated)
        return Ok(_mapper.Map<PostResponse>(post));

      return NotFound();
    }

    [HttpDelete(ApiRoutes.Posts.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid postId)
    {
      var userOwnPost = await _postService.IsUserOwnPostAsync(postId, HttpContext.GetUserId());
      if (!userOwnPost)
        return BadRequest(new { Errors = "You do not own this post" });

      var deleted = await _postService.DeleteAsync(postId);
      if (deleted)
        return NoContent();

      return NotFound();
    }
  }
}
