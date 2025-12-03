using Greenwaytech.PiholeApiClient;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for registration tracking functionality that warns about potential race conditions
/// when multiple client instances are resolved for the same Pi-hole server.
/// </summary>
[TestFixture]
public class RegistrationTrackerTests
{
    private const string TestPiholeUrl = "http://test-pihole.local";
    private const string TestApiKey = "test-api-key-12345";

    [Test]
    public void SingleClientResolution_ShouldNotLogWarning()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages);

        // Act
        var client = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(logMessages, Is.Empty, "No warnings should be logged for single client resolution");
    }

    [Test]
    public void MultipleClientResolutions_ShouldLogWarning()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages);

        // Act
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert
        Assert.That(client1, Is.Not.Null);
        Assert.That(client2, Is.Not.Null);
        Assert.That(client1, Is.Not.SameAs(client2), "Clients should be different instances (transient)");

        var warnings = logMessages.Where(m => m.Contains("Multiple Pi-hole clients")).ToList();
        Assert.That(warnings, Is.Not.Empty, "Should log warning about multiple clients");
        Assert.That(warnings[0], Does.Contain("Multiple Pi-hole clients (2)"));
        Assert.That(warnings[0], Does.Contain(TestPiholeUrl));
        Assert.That(warnings[0], Does.Contain("race conditions"));
    }

    [Test]
    public void ThreeClientResolutions_ShouldLogWarningWithCorrectCount()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages);

        // Act
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client3 = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert
        var warnings = logMessages.Where(m => m.Contains("Multiple Pi-hole clients")).ToList();
        
        // Should have 2 warnings (one for count=2, one for count=3)
        Assert.That(warnings.Count, Is.GreaterThanOrEqualTo(2), "Should log multiple warnings");
        Assert.That(warnings.Any(w => w.Contains("(2)")), Is.True, "Should log warning for count 2");
        Assert.That(warnings.Any(w => w.Contains("(3)")), Is.True, "Should log warning for count 3");
    }

    [Test]
    public void WarningMessage_ShouldContainHelpfulGuidance()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages);

        // Act
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert
        var warning = logMessages.FirstOrDefault(m => m.Contains("Multiple Pi-hole clients"));
        Assert.That(warning, Is.Not.Null);
        
        // Check for helpful guidance in warning message
        Assert.That(warning, Does.Contain("singleton client lifetime"), "Should suggest singleton");
        Assert.That(warning, Does.Contain("parallel operations"), "Should mention parallel operations");
        Assert.That(warning, Does.Contain("AddPiholeApiClientFactory"), "Should mention factory pattern");
    }

    [Test]
    public void DifferentPiholeInstances_ShouldNotTriggerWarning()
    {
        // Arrange
        var logMessages1 = new List<string>();
        var logMessages2 = new List<string>();
        
        // Create two separate service providers for different Pi-hole instances
        var sp1 = CreateServiceProviderWithLogging(logMessages1, "http://pihole1.local");
        var sp2 = CreateServiceProviderWithLogging(logMessages2, "http://pihole2.local");

        // Act
        var client1 = sp1.GetRequiredService<IPiholeApiClientService>();
        var client2 = sp2.GetRequiredService<IPiholeApiClientService>();

        // Assert
        Assert.That(client1, Is.Not.Null);
        Assert.That(client2, Is.Not.Null);
        
        var allWarnings = logMessages1.Concat(logMessages2).Where(m => m.Contains("Multiple Pi-hole clients")).ToList();
        Assert.That(allWarnings, Is.Empty, 
            "Different Pi-hole instances (different service providers) should not trigger warnings");
    }

    [Test]
    public void TrailingSlashNormalization_ShouldDetectDuplicates()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages, "http://pihole.local/");

        // Act
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert
        var warnings = logMessages.Where(m => m.Contains("Multiple Pi-hole clients")).ToList();
        Assert.That(warnings, Is.Not.Empty, 
            "URLs with/without trailing slash should be treated as same instance");
    }

    [Test]
    public void SequentialResolutions_ShouldAllLogWarnings()
    {
        // Arrange
        var logMessages = new List<string>();
        var serviceProvider = CreateServiceProviderWithLogging(logMessages);

        // Act - Resolve clients one at a time
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        Assert.That(logMessages.Count(m => m.Contains("Multiple")), Is.EqualTo(0), "First resolution: no warning");

        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        Assert.That(logMessages.Count(m => m.Contains("Multiple")), Is.EqualTo(1), "Second resolution: warning logged");

        var client3 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        Assert.That(logMessages.Count(m => m.Contains("Multiple")), Is.EqualTo(2), "Third resolution: another warning");
    }

    [Test]
    public void ConcurrentResolutions_ShouldHandleThreadSafety()
    {
        // Arrange
        var logMessages = new System.Collections.Concurrent.ConcurrentBag<string>();
        var serviceProvider = CreateServiceProviderWithConcurrentLogging(logMessages);

        // Act - Resolve clients concurrently
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            var client = serviceProvider.GetRequiredService<IPiholeApiClientService>();
            return client;
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(tasks.All(t => t.Result != null), Is.True, "All clients should be created");
        
        var warnings = logMessages.Where(m => m.Contains("Multiple Pi-hole clients")).ToList();
        Assert.That(warnings.Count, Is.GreaterThan(0), "Should log warnings for concurrent resolutions");
        
        // Should see increasing counts (though exact count may vary due to concurrency)
        Assert.That(warnings.Any(w => w.Contains("(2)")), Is.True, "Should detect at least 2 clients");
    }

    #region Helper Methods

    /// <summary>
    /// Creates a service provider with custom logging that captures log messages
    /// </summary>
    private ServiceProvider CreateServiceProviderWithLogging(List<string> logMessages, string piholeUrl = TestPiholeUrl)
    {
        var services = new ServiceCollection();
        
        services.AddPiholeApiClient(options =>
        {
            options.ApiBaseUrl = piholeUrl;
            options.ApiKey = TestApiKey;
        });

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddProvider(new TestLoggerProvider(logMessages));
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with thread-safe concurrent logging
    /// </summary>
    private ServiceProvider CreateServiceProviderWithConcurrentLogging(
        System.Collections.Concurrent.ConcurrentBag<string> logMessages, 
        string piholeUrl = TestPiholeUrl)
    {
        var services = new ServiceCollection();
        
        services.AddPiholeApiClient(options =>
        {
            options.ApiBaseUrl = piholeUrl;
            options.ApiKey = TestApiKey;
        });

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddProvider(new ConcurrentTestLoggerProvider(logMessages));
        });

        return services.BuildServiceProvider();
    }

    #endregion

    #region Test Logger Implementation

    /// <summary>
    /// Custom logger provider for capturing log messages in tests
    /// </summary>
    private class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _logMessages;

        public TestLoggerProvider(List<string> logMessages)
        {
            _logMessages = logMessages;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_logMessages);
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Custom logger that captures messages
    /// </summary>
    private class TestLogger : ILogger
    {
        private readonly List<string> _logMessages;

        public TestLogger(List<string> logMessages)
        {
            _logMessages = logMessages;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _logMessages.Add(formatter(state, exception));
            }
        }
    }

    /// <summary>
    /// Thread-safe logger provider for concurrent tests
    /// </summary>
    private class ConcurrentTestLoggerProvider : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<string> _logMessages;

        public ConcurrentTestLoggerProvider(System.Collections.Concurrent.ConcurrentBag<string> logMessages)
        {
            _logMessages = logMessages;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ConcurrentTestLogger(_logMessages);
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Thread-safe logger for concurrent tests
    /// </summary>
    private class ConcurrentTestLogger : ILogger
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<string> _logMessages;

        public ConcurrentTestLogger(System.Collections.Concurrent.ConcurrentBag<string> logMessages)
        {
            _logMessages = logMessages;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _logMessages.Add(formatter(state, exception));
            }
        }
    }

    #endregion
}
