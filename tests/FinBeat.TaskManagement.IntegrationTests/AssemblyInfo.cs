using Xunit;

// Serilog assigns the global Log.Logger per host, so two test classes starting hosts at once race
// and one loses the exception it was waiting for. Disabling parallelism here is the only fix.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
