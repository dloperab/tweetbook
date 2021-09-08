using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Tweetbook.Extensions
{
  public static class HttpExtensions
  {
    public static string GetUserId(this HttpContext httpContext)
    {
      if (httpContext.User is null)
      {
        return string.Empty;
      }

      return httpContext.User.Claims.Single(x => x.Type == "id").Value;
    }
  }
}
