using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Request;

namespace Greenwaytech.PiholeApiClient.Extensions;

/// <summary>
/// Extension methods for building API responses
/// </summary>
internal static class ResponseBuilderExtensions
{
    /// <summary>
    /// Creates a failure response with the specified error message
    /// </summary>
    internal static PiholeClientApiResponse<T> ToFailureResponse<T>(this string errorMessage)
    {
        return new PiholeClientApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a success response with the specified data
    /// </summary>
    internal static PiholeClientApiResponse<T> ToSuccessResponse<T>(this T data)
    {
        return new PiholeClientApiResponse<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates an EnsureLocalDnsRecordResponse for when a record already exists
    /// </summary>
    internal static PiholeClientApiResponse<EnsureLocalDnsRecordResponse> ToAlreadyExistsResponse(
        this string message)
    {
        return new EnsureLocalDnsRecordResponse
        {
            Message = message,
            DataOperation = DataOperation.AlreadyExists
        }.ToSuccessResponse();
    }

    /// <summary>
    /// Creates an EnsureLocalDnsRecordResponse for when a record is created
    /// </summary>
    internal static PiholeClientApiResponse<EnsureLocalDnsRecordResponse> ToCreatedResponse(
        this string message)
    {
        return new EnsureLocalDnsRecordResponse
        {
            Message = message,
            DataOperation = DataOperation.Created
        }.ToSuccessResponse();
    }

    /// <summary>
    /// Creates an EnsureLocalDnsRecordResponse for when a record is deleted
    /// </summary>
    internal static PiholeClientApiResponse<EnsureLocalDnsRecordResponse> ToDeletedResponse(
        this string message)
    {
        return new EnsureLocalDnsRecordResponse
        {
            Message = message,
            DataOperation = DataOperation.Deleted
        }.ToSuccessResponse();
    }

    /// <summary>
    /// Creates an EnsureLocalDnsRecordResponse for when no records exist
    /// </summary>
    internal static PiholeClientApiResponse<EnsureLocalDnsRecordResponse> ToNotFoundResponse(
        this string message)
    {
        return new EnsureLocalDnsRecordResponse
        {
            Message = message,
            DataOperation = DataOperation.AlreadyExists
        }.ToSuccessResponse();
    }
}
