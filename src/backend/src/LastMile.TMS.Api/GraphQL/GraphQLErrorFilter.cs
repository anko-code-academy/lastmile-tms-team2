using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;

namespace LastMile.TMS.Api.GraphQL;

public sealed class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            ValidationException exception => error
                .WithMessage(string.Join("; ", exception.Errors.Select(x => x.ErrorMessage)))
                .WithCode("VALIDATION_ERROR"),
            System.Collections.Generic.KeyNotFoundException exception => error
                .WithMessage(exception.Message)
                .WithCode("NOT_FOUND"),
            InvalidOperationException exception => error
                .WithMessage(exception.Message)
                .WithCode("INVALID_OPERATION"),
            _ => error
        };
    }
}
