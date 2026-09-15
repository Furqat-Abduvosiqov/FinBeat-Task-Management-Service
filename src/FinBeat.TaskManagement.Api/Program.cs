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
}
finally
{
    Log.CloseAndFlush();
}
