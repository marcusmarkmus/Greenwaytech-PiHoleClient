using Greenwaytech.PiholeApiClient.Extensions;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Request;
using Greenwaytech.PiholeApiClient.Model.App.Response;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Unit tests for ResponseBuilderExtensions
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ResponseBuilderExtensionsTests
{
    #region ToFailureResponse Tests

    [Test]
    public void ToFailureResponse_ShouldCreateFailureResponse()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void ToFailureResponse_WithEmptyMessage_ShouldCreateResponse()
    {
        // Arrange
        var errorMessage = "";

        // Act
        var result = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Is.Empty);
    }

    [Test]
    public void ToFailureResponse_WithDifferentTypes_ShouldWork()
    {
        // Arrange
        var errorMessage = "Error";

        // Act
        var result1 = errorMessage.ToFailureResponse<string>();
        var result2 = errorMessage.ToFailureResponse<int>();
        var result3 = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(result1.IsSuccess, Is.False);
        Assert.That(result2.IsSuccess, Is.False);
        Assert.That(result3.IsSuccess, Is.False);
    }

    [Test]
    public void ToFailureResponse_WithLongMessage_ShouldCreateResponse()
    {
        // Arrange
        var errorMessage = new string('x', 1000);

        // Act
        var result = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
    }

    #endregion

    #region ToSuccessResponse Tests

    [Test]
    public void ToSuccessResponse_WithValidData_ShouldCreateSuccessResponse()
    {
        // Arrange
        var data = new EnsureLocalDnsRecordResponse
        {
            Message = "Success",
            DataOperation = DataOperation.Created
        };

        // Act
        var result = data.ToSuccessResponse();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(data));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ToSuccessResponse_WithNullData_ShouldCreateResponse()
    {
        // Arrange
        EnsureLocalDnsRecordResponse? data = null;

        // Act
        var result = data.ToSuccessResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void ToSuccessResponse_WithString_ShouldWork()
    {
        // Arrange
        var data = "Success message";

        // Act
        var result = data.ToSuccessResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(data));
    }

    [Test]
    public void ToSuccessResponse_WithInt_ShouldWork()
    {
        // Arrange
        var data = 42;

        // Act
        var result = data.ToSuccessResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(data));
    }

    #endregion

    #region ToAlreadyExistsResponse Tests

    [Test]
    public void ToAlreadyExistsResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Record already exists";

        // Act
        var result = message.ToAlreadyExistsResponse();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.EqualTo(message));
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ToAlreadyExistsResponse_WithEmptyMessage_ShouldCreateResponse()
    {
        // Arrange
        var message = "";

        // Act
        var result = message.ToAlreadyExistsResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.Empty);
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
    }

    #endregion

    #region ToCreatedResponse Tests

    [Test]
    public void ToCreatedResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Record created successfully";

        // Act
        var result = message.ToCreatedResponse();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.EqualTo(message));
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ToCreatedResponse_WithEmptyMessage_ShouldCreateResponse()
    {
        // Arrange
        var message = "";

        // Act
        var result = message.ToCreatedResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.Empty);
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.Created));
    }

    #endregion

    #region ToDeletedResponse Tests

    [Test]
    public void ToDeletedResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Record deleted successfully";

        // Act
        var result = message.ToDeletedResponse();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.EqualTo(message));
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.Deleted));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ToDeletedResponse_WithEmptyMessage_ShouldCreateResponse()
    {
        // Arrange
        var message = "";

        // Act
        var result = message.ToDeletedResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.Empty);
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.Deleted));
    }

    #endregion

    #region ToNotFoundResponse Tests

    [Test]
    public void ToNotFoundResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Record not found";

        // Act
        var result = message.ToNotFoundResponse();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.EqualTo(message));
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ToNotFoundResponse_WithEmptyMessage_ShouldCreateResponse()
    {
        // Arrange
        var message = "";

        // Act
        var result = message.ToNotFoundResponse();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Message, Is.Empty);
        Assert.That(result.Data.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
    }

    #endregion

    #region Integration Tests - Chaining

    [Test]
    public void ResponseBuilders_ShouldSupportMethodChaining()
    {
        // Arrange
        var errorMessage = "Validation failed";
        var successMessage = "Operation completed";

        // Act
        var failureResponse = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        var createdResponse = successMessage.ToCreatedResponse();
        var deletedResponse = successMessage.ToDeletedResponse();

        // Assert - All should create valid responses
        Assert.That(failureResponse.IsSuccess, Is.False);
        Assert.That(createdResponse.IsSuccess, Is.True);
        Assert.That(deletedResponse.IsSuccess, Is.True);
    }

    [Test]
    public void ResponseBuilders_EachOperationType_ShouldHaveCorrectDataOperation()
    {
        // Arrange
        var message = "Test message";

        // Act
        var created = message.ToCreatedResponse();
        var deleted = message.ToDeletedResponse();
        var alreadyExists = message.ToAlreadyExistsResponse();
        var notFound = message.ToNotFoundResponse();

        // Assert
        Assert.That(created.Data!.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(deleted.Data!.DataOperation, Is.EqualTo(DataOperation.Deleted));
        Assert.That(alreadyExists.Data!.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(notFound.Data!.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
    }

    [Test]
    public void ResponseBuilders_SuccessResponses_ShouldNotHaveErrorMessage()
    {
        // Arrange
        var message = "Test";

        // Act
        var responses = new[]
        {
            message.ToCreatedResponse(),
            message.ToDeletedResponse(),
            message.ToAlreadyExistsResponse(),
            message.ToNotFoundResponse()
        };

        // Assert
        foreach (var response in responses)
        {
            Assert.That(response.ErrorMessage, Is.Null.Or.Empty, 
                $"Success response should not have error message: {response.Data?.DataOperation}");
        }
    }

    [Test]
    public void ResponseBuilders_FailureResponse_ShouldNotHaveData()
    {
        // Arrange
        var errorMessage = "Error occurred";

        // Act
        var response = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(response.Data, Is.Null);
        Assert.That(response.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ToFailureResponse_WithSpecialCharacters_ShouldPreserveMessage()
    {
        // Arrange
        var errorMessage = "Error: <>&\"'@#$%^*()[]{}";

        // Act
        var result = errorMessage.ToFailureResponse<EnsureLocalDnsRecordResponse>();

        // Assert
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
    }

    [Test]
    public void ToSuccessResponse_WithComplexObject_ShouldPreserveData()
    {
        // Arrange
        var data = new EnsureLocalDnsRecordResponse
        {
            Message = "Complex response",
            DataOperation = DataOperation.Created,
            RemovedCount = 5,
            RemovedIpAddresses = new List<string> { "192.168.1.1", "192.168.1.2" },
            RemovedDomains = new List<string> { "test1.local", "test2.local" },
            ConflictingIpAddresses = new List<string> { "10.0.0.1" }
        };

        // Act
        var result = data.ToSuccessResponse();

        // Assert
        Assert.That(result.Data, Is.EqualTo(data));
        Assert.That(result.Data!.RemovedCount, Is.EqualTo(5));
        Assert.That(result.Data.RemovedIpAddresses, Has.Count.EqualTo(2));
        Assert.That(result.Data.RemovedDomains, Has.Count.EqualTo(2));
        Assert.That(result.Data.ConflictingIpAddresses, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResponseBuilders_WithUnicodeCharacters_ShouldWork()
    {
        // Arrange
        var message = "Erfolg! ??! ????! ?";

        // Act
        var result = message.ToCreatedResponse();

        // Assert
        Assert.That(result.Data!.Message, Is.EqualTo(message));
    }

    [Test]
    public void ResponseBuilders_WithNewlines_ShouldPreserveFormatting()
    {
        // Arrange
        var message = "Line 1\nLine 2\nLine 3";

        // Act
        var result = message.ToCreatedResponse();

        // Assert
        Assert.That(result.Data!.Message, Is.EqualTo(message));
    }

    #endregion
}
