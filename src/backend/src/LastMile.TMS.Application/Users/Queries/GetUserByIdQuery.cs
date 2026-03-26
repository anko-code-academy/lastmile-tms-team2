using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using MediatR;

namespace LastMile.TMS.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserManagementUserDto>;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetUserByIdQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetUserByIdQuery, UserManagementUserDto>
{
    public Task<UserManagementUserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
        UserManagementReadModel.GetUserAsync(dbContext, request.Id, cancellationToken);
}
