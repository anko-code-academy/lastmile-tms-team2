using DotNetEnv;
using Hangfire;
using LastMile.TMS.Api.Configuration;
using LastMile.TMS.Application;
using LastMile.TMS.Infrastructure;
using LastMile.TMS.Persistence;
using Serilog;

// Use CreateLogger - Serilog's CreateBootstrapLogger breaks WebApplicationFactory (serilog-aspnetcore#289).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Env.NoClobber().TraversePath().Load();

    var builder = WebApplication.CreateBuilder(args);
    var disableExternalInfrastructure = builder.Configuration.GetValue("Testing:DisableExternalInfrastructure", false);
    var enableTestSupport = builder.Configuration.GetValue("Testing:EnableTestSupport", false);

    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPersistence(builder.Configuration, builder.Environment)
        .AddOpenIddictJwtAuthenticationDefaults()
        .AddLastMileApi(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    if (!disableExternalInfrastructure && !enableTestSupport)
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    if (!disableExternalInfrastructure)
    {
        app.UseHangfireDashboard("/hangfire", HangfireDashboardConfiguration.CreateOptions(builder.Configuration));
    }

    app.MapControllers();
    app.MapGraphQL("/api/graphql");
    app.MapGraphQL("/graphql");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}

// Exposes entry point to WebApplicationFactory<Program> (integration tests).
namespace LastMile.TMS.Api
{
    public partial class Program;
}
