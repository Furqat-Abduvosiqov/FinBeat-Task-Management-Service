using Xunit;

// Serilog's UseSerilog defaults to preserveStaticLogger: false, so every host this suite starts
// assigns the global Log.Logger and the per-host logger factory resolves through it. Two test
// classes starting hosts at once therefore race, and the one asserting on its own capturing sink
// loses the exception it was waiting for. xUnit runs classes without a shared collection in
// parallel, so the only place to settle it is here.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
