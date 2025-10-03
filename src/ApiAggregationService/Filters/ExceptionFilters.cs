using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ApiAggregationService.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(new
            {
                Error = "An unexpected error occurred.",
                Details = context.Exception.Message
            })
            {
                StatusCode = 500
            };

            context.ExceptionHandled = true;
        }
    }
}