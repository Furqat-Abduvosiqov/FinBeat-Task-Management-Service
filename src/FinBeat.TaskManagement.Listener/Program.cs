using FinBeat.TaskManagement.Listener;
using Serilog;

Log.Logger = Bootstrap.CreateBootstrapLogger();

try
{
    Host.CreateApplicationBuilder(args)
        .AddListenerHost()
        .Build()
        .Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Listener host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
