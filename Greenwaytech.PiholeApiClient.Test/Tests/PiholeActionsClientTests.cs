using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for Pi-hole Actions client functionality including gravity updates, DNS restarts, and cache flushing.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PiholeActionsClientTests
{
    [SetUp]
    public async Task Setup()
    {
        await PiholeTestContainerFixture.RestoreBaselineIfNeeded();
    }

    [TearDown]
    public async Task Teardown()
    {
        await PiholeTestContainerFixture.CleanupAfterModifyingTest();
    }

    [Test, Order(100)]
    public void ActionsClient_Property_ShouldNotBeNull()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Assert
        Assert.That(cut.Actions, Is.Not.Null);
        Assert.That(cut.Actions, Is.InstanceOf<Greenwaytech.PiholeApiClient.ApiClient.SubClients.Actions.IActionsClient>());
    }

    [Test, Order(101)]
    public async Task ActionsClient_FlushLogs_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
        var result = await cut.Actions.FlushLogsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test, Order(102)]
    public async Task ActionsClient_FlushArpCache_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = await cut.Actions.FlushArpCacheAsync();
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test, Order(103)]
    public void ActionsClient_FlushLogs_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var ex = Assert.Throws<AggregateException>(() =>
        {
            var task = cut.Actions.FlushLogsAsync(cts.Token);
            task.Wait();
        });
        Assert.That(ex.InnerException, Is.InstanceOf<TaskCanceledException>());
    }

    [Test, Order(104)]
    public void ActionsClient_FlushArpCache_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
#pragma warning disable CS0618 // Type or member is obsolete
        var ex = Assert.Throws<AggregateException>(() =>
        {
            var task = cut.Actions.FlushArpCacheAsync(cts.Token);
            task.Wait();
        });
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.That(ex.InnerException, Is.InstanceOf<TaskCanceledException>());
    }

    [Test, Order(105)]
    public async Task ActionsClient_ParallelActions_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act: Execute multiple actions in parallel (though they'll be serialized by the server)
#pragma warning disable CS0618 // Type or member is obsolete
        var tasks = new[]
        {
            cut.Actions.FlushLogsAsync(),
            cut.Actions.FlushArpCacheAsync(),
        };
#pragma warning restore CS0618 // Type or member is obsolete

        var results = await Task.WhenAll(tasks);

        // Assert: All operations should complete successfully
        Assert.That(results, Has.Length.EqualTo(2));
        foreach (var result in results)
        {
            Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
            Assert.That(result.Data, Is.Not.Null);
        }
    }

    [Test, Order(106)]
    public async Task ActionsClient_RestartDns_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
        var result = await cut.Actions.RestartDnsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test, Order(107)]
    public void ActionsClient_RestartDns_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var ex = Assert.Throws<AggregateException>(() =>
        {
            var task = cut.Actions.RestartDnsAsync(cts.Token);
            task.Wait();
        });
        Assert.That(ex.InnerException, Is.InstanceOf<TaskCanceledException>());
    }

    [Test, Order(108)]
    public async Task ActionsClient_FlushLogs_ThenRestartDns_ShouldWorkInSequence()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act: Flush logs then restart DNS (common maintenance workflow)
        var flushResult = await cut.Actions.FlushLogsAsync();
        Assert.That(flushResult.IsSuccess, Is.True, "Flush logs should succeed before DNS restart");

        var restartResult = await cut.Actions.RestartDnsAsync();

        // Assert
        Assert.That(restartResult.IsSuccess, Is.True, restartResult.ErrorMessage);
        Assert.That(restartResult.Data, Is.Not.Null);
    }

    [Test, Order(109), Explicit("Gravity update is a long-running operation (30+ seconds) that can fail in containerized tests. Run manually if needed.")]
    public async Task ActionsClient_UpdateGravity_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
        var result = await cut.Actions.UpdateGravityAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test, Order(110)]
    public void ActionsClient_UpdateGravity_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var ex = Assert.Throws<AggregateException>(() =>
        {
            var task = cut.Actions.UpdateGravityAsync(cts.Token);
            task.Wait();
        });
        Assert.That(ex.InnerException, Is.InstanceOf<TaskCanceledException>());
    }

    [Test, Order(111), Ignore("This test runs after previous gravity update and can fail due to container stress. Run individually if needed.")]
    public async Task ActionsClient_UpdateGravity_ThenRestartDns_ShouldWorkInSequence()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act: Update gravity then restart DNS (common update workflow)
        var gravityResult = await cut.Actions.UpdateGravityAsync();
        Assert.That(gravityResult.IsSuccess, Is.True, "Gravity update should succeed before DNS restart");

        var restartResult = await cut.Actions.RestartDnsAsync();

        // Assert
        Assert.That(restartResult.IsSuccess, Is.True, restartResult.ErrorMessage);
        Assert.That(restartResult.Data, Is.Not.Null);
    }

    [Test, Order(112), Ignore("This test can overwhelm the container with multiple gravity updates. Run individually if needed.")]
    public async Task ActionsClient_MultipleActions_ShouldAllSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act: Execute multiple actions in sequence
        var flushLogsResult = await cut.Actions.FlushLogsAsync();
#pragma warning disable CS0618 // Type or member is obsolete
        var flushArpResult = await cut.Actions.FlushArpCacheAsync();
#pragma warning restore CS0618 // Type or member is obsolete
        var restartDnsResult = await cut.Actions.RestartDnsAsync();
        var updateGravityResult = await cut.Actions.UpdateGravityAsync();

        // Assert: All operations should succeed
        using (Assert.EnterMultipleScope())
        {
            Assert.That(flushLogsResult.IsSuccess, Is.True, $"Flush logs failed: {flushLogsResult.ErrorMessage}");
            Assert.That(flushArpResult.IsSuccess, Is.True, $"Flush ARP failed: {flushArpResult.ErrorMessage}");
            Assert.That(restartDnsResult.IsSuccess, Is.True, $"Restart DNS failed: {restartDnsResult.ErrorMessage}");
            Assert.That(updateGravityResult.IsSuccess, Is.True, $"Update gravity failed: {updateGravityResult.ErrorMessage}");
        }
    }

    [Test, Order(113), Ignore("This test can overwhelm the container with gravity update. Run individually if needed.")]
    public async Task ActionsClient_CompleteMaintenanceWorkflow_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act: Execute a complete maintenance workflow
        // 1. Flush old logs
        var flushLogsResult = await cut.Actions.FlushLogsAsync();
        Assert.That(flushLogsResult.IsSuccess, Is.True, "Step 1: Flush logs failed");

        // 2. Flush ARP cache
#pragma warning disable CS0618 // Type or member is obsolete
        var flushArpResult = await cut.Actions.FlushArpCacheAsync();
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.That(flushArpResult.IsSuccess, Is.True, "Step 2: Flush ARP cache failed");

        // 3. Update gravity database
        var gravityResult = await cut.Actions.UpdateGravityAsync();
        Assert.That(gravityResult.IsSuccess, Is.True, "Step 3: Update gravity failed");

        // 4. Restart DNS service to apply all changes
        var restartResult = await cut.Actions.RestartDnsAsync();

        // Assert: Final restart should succeed
        Assert.That(restartResult.IsSuccess, Is.True, restartResult.ErrorMessage);
        Assert.That(restartResult.Data, Is.Not.Null);
    }

    private IPiholeApiClientService GeneratePiholeApiClientService()
    {
        return PiholeTestContainerFixture.ServiceProvider.GetRequiredService<IPiholeApiClientService>();
    }
}
