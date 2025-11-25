using NUnit.Framework;

// Enable parallel test execution at assembly level
[assembly: Parallelizable(ParallelScope.Fixtures)]

// Use 2 parallel workers - conservative setting for CI/CD stability
// GitHub Actions runners have 2-3 CPU cores and 7GB RAM
// Each Pi-hole container needs ~400MB RAM and significant CPU during startup
[assembly: LevelOfParallelism(2)]
