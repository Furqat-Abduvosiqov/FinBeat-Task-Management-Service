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
    
    // Non-zero, or a worker that could not reach the broker reports a clean shutdown and no restart policy fires.
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
