﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;

namespace Tweetbook.Domain
{
  public class RefreshToken
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Token { get; set; }

    public string JwtId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public bool IsUsed { get; set; }

    public bool Invalidated { get; set; }

    public string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; }
  }
}
