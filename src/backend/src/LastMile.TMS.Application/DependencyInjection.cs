using FluentValidation;
using LastMile.TMS.Application.Common.Behaviors;
using LastMile.TMS.Application.Depots.Reads;
using LastMile.TMS.Application.Drivers.Reads;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Routes.Reads;
using LastMile.TMS.Application.Routes.Services;
using LastMile.TMS.Application.Users.Reads;
using LastMile.TMS.Application.Vehicles.Reads;
using LastMile.TMS.Application.Zones.Reads;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IDriverReadService, DriverReadService>();
        services.AddScoped<IDepotReadService, DepotReadService>();
        services.AddScoped<IZoneReadService, ZoneReadService>();
        services.AddScoped<IParcelReadService, ParcelReadService>();
        services.AddScoped<IParcelRegistrationService, ParcelRegistrationService>();
        services.AddScoped<ParcelImportProcessor>();
        services.AddScoped<IRouteReadService, RouteReadService>();
        services.AddScoped<IRoutePlanningService, RoutePlanningService>();
        services.AddScoped<IVehicleReadService, VehicleReadService>();
        services.AddScoped<IUserReadService, UserReadService>();

        return services;
    }
}
