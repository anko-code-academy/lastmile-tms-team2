using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.Logging;

namespace LastMile.TMS.Api.GraphQL.Common;

public sealed class GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger) : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is not null)
        {
            logger.LogError(error.Exception,
                "GraphQL error on path {Path}: {Message}",
                error.Path, error.Exception.Message);
        }

        return error.Exception switch
        {
            ValidationException exception => error
                .WithMessage(string.Join("; ", exception.Errors.Select(x => x.ErrorMessage)))
                .WithCode("VALIDATION_ERROR"),
            System.Collections.Generic.KeyNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("NOT_FOUND"),
            ArgumentException exception => error
                .WithMessage(exception.Message)
                .WithCode("INVALID_ARGUMENT"),
            InvalidOperationException exception => error
                .WithMessage(exception.Message)
                .WithCode("INVALID_OPERATION"),
            _ => error
        };
    }
}
