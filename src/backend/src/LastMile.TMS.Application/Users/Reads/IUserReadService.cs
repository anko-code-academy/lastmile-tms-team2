using LastMile.TMS.Application.Users.Common;

namespace LastMile.TMS.Application.Users.Reads;

public interface IUserReadService
{
    IQueryable<UserManagementUserDto> GetUsers(
        string? search = null,
        bool? isActive = null,
        Guid? depotId = null,
        Guid? zoneId = null);
}
