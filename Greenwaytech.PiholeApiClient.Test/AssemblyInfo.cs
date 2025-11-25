using NUnit.Framework;

// All tests must run sequentially because they share a single Pi-hole container
// This matches the original working configuration where a single test class had [Parallelizable(ParallelScope.None)]
[assembly: Parallelizable(ParallelScope.None)]
