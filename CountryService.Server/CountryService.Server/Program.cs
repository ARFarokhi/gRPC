using System.IO.Compression;
using Calzolari.Grpc.AspNetCore.Validation;
using CountryService.Server.ApplicationService;
using CountryService.Server.Helper;
using CountryService.Server.Interceptors;
using CountryService.Server.Services;
using CountryService.Server.Services.v1;
using CountryService.Server.Validations;
using Grpc.Core;
using Grpc.Net.Compression;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS,
// visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

// Message Size and Compression Options
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true; // Enabling error details
    //Enabling the IgnoreUnknownServices option will allow the request to go
    //through the next middleware declared after the gRPC endpoint middleware
    //declaration.Without that option set to true, when an unknown service or method
    //is invoked, the server returns the gRPC response immediately
    //with the UNIMPLEMENTED gRPC status
    options.IgnoreUnknownServices = true;
    options.MaxSendMessageSize = 6291456; // 6 MB
    options.MaxReceiveMessageSize = 6291456; // 6 MB
    options.CompressionProviders = new List<ICompressionProvider>
    {
        new BrotliCompressionProvider(CompressionLevel.Optimal) // br
    };

    options.ResponseCompressionAlgorithm = "br"; // grpc accept-encoding, and must match the compression
                                                 // provider declared in CompressionProviders collection
    options.ResponseCompressionLevel = CompressionLevel.Optimal; //compression level used if not set on the provider

    options.Interceptors.Add<ExceptionInterceptor>(); // Register custom interceptor

    options.EnableMessageValidation(); //Enable validation
});

//Register Validation for gRpc
builder.Services.AddGrpcValidation();
//Add custom gRPC validation
builder.Services.AddValidator<CountryCreateRequestValidator>();

// for Minimal-Api
builder.Services.AddSingleton<ProtoService>();
//


//for gRPCurl
builder.Services.AddGrpcReflection();
//Singleton lifetime:means only one instance of the service will be created
builder.Services.AddSingleton<CountryManagementService>();

var app = builder.Build();

app.MapGrpcReflectionService();
//Configure the HTTP request pipeline.
//add your Grpc Services
app.MapGrpcService<CountryGrpcService>();
app.MapGrpcService<CountryService.Server.Services.v2.CountryGrpcService>();

//#######################  Minimal-Api  ################################
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapGet("/protos", (ProtoService protoService) => Results.Ok(protoService.GetAll()));
app.MapGet("/protos/v{version:int}/{protoName}",
    (ProtoService protoService, int version, string protoName) =>
    {
        var filePath = protoService.Get(version, protoName);
        return string.IsNullOrEmpty(filePath) ? Results.File(filePath) : Results.NotFound();
    });

app.MapGet("/protos/v{version:int}/{protoName}/view",
    async (ProtoService protoService, int version, string protoName) =>
    {
        var text = await protoService.ViewAsync(version, protoName);
        return !string.IsNullOrEmpty(text) ? Results.Text(text) : Results.NotFound();
    });

//####################################################################

app.Use(async (context, next) =>
{
    context.Response.ContentType = "application/grpc";
    context.Response.Headers.Add("grpc-status", ((int)StatusCode.NotFound).ToString());
    await next();
});

app.Run();
