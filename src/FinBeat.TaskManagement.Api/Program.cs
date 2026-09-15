using FinBeat.TaskManagement.Api;
using Serilog;

Log.Logger = Bootstrap.CreateBootstrapLogger();

try
{
    WebApplication
        .CreateBuilder(args)
        .AddApiHost()
        .Build()
        .UseApiPipeline()
        .Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Api host terminated unexpectedly");

    // Non-zero, or a host that could not start reports success and no restart policy fires.
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
