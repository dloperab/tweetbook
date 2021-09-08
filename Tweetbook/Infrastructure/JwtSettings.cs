using System;

namespace Tweetbook.Infrastructure
{
  public class JwtSettings
  {
    public string Secret { get; set; }
    public TimeSpan Lifetime { get; set; }
  }
}
