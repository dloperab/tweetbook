using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

using Tweetbook.Domain;
using Tweetbook.Infrastructure;
using Tweetbook.Data;

namespace Tweetbook.Services
{
  public class IdentityService : IIdentityService
  {
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenValidationParameters _validationParameters;
    private readonly DataContext _dataContext;

    public IdentityService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
      JwtSettings jwtSettings, TokenValidationParameters validationParameters, 
      DataContext dataContext)
    {
      _userManager = userManager;
      _roleManager = roleManager;
      _jwtSettings = jwtSettings;
      _validationParameters = validationParameters;
      _dataContext = dataContext;
    }

    public async Task<AuthenticationResult> RegisterAsync(string email, string password)
    {
      var existingUser = await _userManager.FindByEmailAsync(email);

      if (existingUser is not null)
      {
        return new AuthenticationResult
        {
          Errors = new[] { "User with this email address already exists" }
        };
      }

      var newUser = new IdentityUser
      {
        Email = email,
        UserName = email
      };
      var createdUser = await _userManager.CreateAsync(newUser, password);

      if (!createdUser.Succeeded)
      {
        return new AuthenticationResult
        {
          Errors = createdUser.Errors.Select(x => x.Description)
        };
      }

      return await GenerateAuthResponseAsync(newUser);
    }

    public async Task<AuthenticationResult> LoginAsync(string email, string password)
    {
      var user = await _userManager.FindByEmailAsync(email);
      if (user is null)
      {
        return new AuthenticationResult
        {
          Errors = new[] { "User does not exists" }
        };
      }

      var userHasValidPwd = await _userManager.CheckPasswordAsync(user, password);
      if (!userHasValidPwd)
      {
        return new AuthenticationResult
        {
          Errors = new[] { "User or password incorrect" }
        };
      }

      return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
    {
      var validatedToken = GetPrincipalFromToken(token);
      if (validatedToken is null)
      {
        return new AuthenticationResult { Errors = new[] { "Invalid token" } };
      }

      var expiryDateUnix = long.Parse(validatedToken.Claims.Single(
        x => x.Type == JwtRegisteredClaimNames.Exp).Value);
      var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        .AddSeconds(expiryDateUnix);
      if (expiryDateTimeUtc > DateTime.UtcNow)
      {
        return new AuthenticationResult { Errors = new[] { "This token hasn't expired yet" } };
      }

      var storedRefreshToken = await _dataContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);
      if (storedRefreshToken is null)
      {
        return new AuthenticationResult { Errors = new[] { "This refresh token doesn't exist" } };
      }
      if (DateTime.UtcNow > storedRefreshToken.ExpirationDate)
      {
        return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };
      }
      if (storedRefreshToken.Invalidated)
      {
        return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };
      }
      if (storedRefreshToken.IsUsed)
      {
        return new AuthenticationResult { Errors = new[] { "This refresh token has been used" } };
      }

      var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
      if (storedRefreshToken.JwtId != jti)
      {
        return new AuthenticationResult { Errors = new[] { "This refresh token doesn't match this JWT" } };
      }

      storedRefreshToken.IsUsed = true;
      _dataContext.RefreshTokens.Update(storedRefreshToken);
      await _dataContext.SaveChangesAsync();

      var userId = validatedToken.Claims.Single(x => x.Type == "id").Value;
      var user = await _userManager.FindByIdAsync(userId);

      return await GenerateAuthResponseAsync(user);
    }

    private async Task<AuthenticationResult> GenerateAuthResponseAsync(IdentityUser user)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

      var claims = new List<Claim>
      {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("id", user.Id)
      };

      var userRoles = await _userManager.GetRolesAsync(user);
      foreach (var userRole in userRoles)
      {
        claims.Add(new Claim(ClaimTypes.Role, userRole));
        var role = await _roleManager.FindByNameAsync(userRole);
        if (role == null) continue;
        var roleClaims = await _roleManager.GetClaimsAsync(role);

        foreach (var roleClaim in roleClaims)
        {
          if (claims.Contains(roleClaim))
            continue;

          claims.Add(roleClaim);
        }
      }

      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.Add(_jwtSettings.Lifetime),
        SigningCredentials = new SigningCredentials(
          new SymmetricSecurityKey(key),
          SecurityAlgorithms.HmacSha256Signature
        )
      };

      var token = tokenHandler.CreateToken(tokenDescriptor);

      var refreshToken = new RefreshToken
      {
        JwtId = token.Id,
        UserId = user.Id,
        CreationDate = DateTime.UtcNow,
        ExpirationDate = DateTime.UtcNow.AddMonths(6),
      };
      await _dataContext.RefreshTokens.AddAsync(refreshToken);
      await _dataContext.SaveChangesAsync();

      return new AuthenticationResult
      {
        Success = true,
        Token = tokenHandler.WriteToken(token),
        RefreshToken = refreshToken.Token
      };
    }

    private ClaimsPrincipal GetPrincipalFromToken(string token)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      try
      {
        var principal = tokenHandler.ValidateToken(token,
          _validationParameters, out var validatedToken);
        if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
          return null;

        return principal;
      }
      catch
      {
        return null;
      }
    }

    private bool IsJwtWithValidSecurityAlgorithm(SecurityToken token)
    {
      return (token is JwtSecurityToken jwtSecurityToken) &&
        jwtSecurityToken.Header.Alg.Equals(
          SecurityAlgorithms.HmacSha256,
          StringComparison.InvariantCultureIgnoreCase);
    }
  }
}