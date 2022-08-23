using Grpc.Core;
using Microsoft.Data.SqlClient;

namespace CountryService.Server.Interceptors;

public static class ExceptionHelpers
{
    public static RpcException Handle<T>(this Exception exception,
        ServerCallContext context,
        ILogger<T> logger, Guid correlationId) =>
        exception switch
        {
            TimeoutException timeoutException =>
                HandleTimeoutException(timeoutException, context, logger, correlationId),

            SqlException sqlException =>
                HandleSqlException(sqlException, context, logger, correlationId),

            RpcException rpcException =>
                HandleRpcException(rpcException, context, logger, correlationId),
            _ => HandleDefault(exception, context, logger, correlationId)
        };

    private static RpcException HandleTimeoutException<T>(TimeoutException exception,
        ServerCallContext context, ILogger<T> logger,
        Guid correlationId)
    {
        logger.LogError(exception, $"CorrelationId: {correlationId} - A timeout occurred");
        Status status;
        status = new Status(StatusCode.Internal, "An external resource did not answer within the time limit");
        return new RpcException(status, CreateTrailers(correlationId));
    }

    private static RpcException HandleSqlException<T>(SqlException exception,
        ServerCallContext context, ILogger<T> logger,
        Guid correlationId)
    {
        logger.LogError(exception, $"CorrelationId: {correlationId} - An SQL error occurred");
        Status status;
        status = exception.Number == -2 ?
            new Status(StatusCode.DeadlineExceeded, "SQL timeout") :
            new Status(StatusCode.Internal, "SQL error");

        return new RpcException(status, CreateTrailers(correlationId));
    }

    private static RpcException HandleDefault<T>(Exception exception,
        ServerCallContext context,
        ILogger<T> logger, Guid correlationId)
    {
        logger.LogError(exception, $"CorrelationId: {correlationId} - An error occurred");
        return new RpcException(
            new Status(StatusCode.Internal, exception.Message),
            CreateTrailers(correlationId));
    }

    private static RpcException HandleRpcException<T>(RpcException exception,
        ServerCallContext context, ILogger<T> logger,
        Guid correlationId)
    {
        logger.LogError(exception, $"CorrelationId:{ correlationId}-An error occurred");
        var trailers = exception.Trailers;
        trailers.Add(CreateTrailers(correlationId)[0]);
        return new RpcException(
            new Status(exception.StatusCode, exception.Message),
            trailers);
    }

    /// <summary>
    /// Adding the correlation to Response Trailers
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    private static Metadata CreateTrailers(Guid correlationId) =>
        new Metadata
        {
            {"CorrelationId", correlationId.ToString()}
        };
}