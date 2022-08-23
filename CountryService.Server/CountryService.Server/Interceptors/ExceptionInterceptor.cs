using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CountryService.Server.Interceptors;

/// <summary>
/// An interceptor is a class that must inherit the Interceptor abstract class, and can
///be applied globally or specifically on a particular service.Several Interceptors can be
///defined and are executed in the order in which they are declared.A custom Interceptor
///allows you to define specific behavior for each kind of gRPC service.
/// Register custom ExceptionInterceptor interceptor
/// builder.Services.AddGrpc(options => {
///     options.Interceptors.Add<CustomeInterceptor>();
/// });
///</summary>
public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;
    private readonly Guid _correlationId;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger)
    {
        _logger = logger;
        _correlationId = Guid.NewGuid();
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }
}