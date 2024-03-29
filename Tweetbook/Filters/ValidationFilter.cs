﻿using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Tweetbook.Contracts.V1.Responses;

namespace Tweetbook.Filters
{
  public class ValidationFilter : IAsyncActionFilter
  {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
      if (!context.ModelState.IsValid)
      {
        var errors = context.ModelState
          .Where(x => x.Value.Errors.Any())
          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(x => x.ErrorMessage)).ToArray();

        var errorResponse = new ErrorResponse();
        foreach (var error in errors)
        {
          foreach (var subError in error.Value)
          {
            var errorModel = new ErrorModel
            {
              FieldName = error.Key,
              Message = subError
            };

            errorResponse.Errors.Add(errorModel);
          }
        }

        context.Result = new BadRequestObjectResult(errorResponse);
        return;
      }

      await next();
    }
  }
}
