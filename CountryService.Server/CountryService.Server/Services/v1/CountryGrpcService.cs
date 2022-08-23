using CountryService.Server.ApplicationService;
using CountryService.Web.Protos.model;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CountryService.Server.Services.v1
{
    public class CountryGrpcService : CountryService.Web.Protos.v1.CountryService.CountryServiceBase
    {
        private readonly CountryManagementService _countryManagementService;
        private readonly ILogger<CountryGrpcService> _logger;

        public CountryGrpcService(CountryManagementService countryManagementService, ILogger<CountryGrpcService> logger)
        {
            _countryManagementService = countryManagementService;
            _logger = logger;
        }

        public override async Task GetAll(Empty request, IServerStreamWriter<CountryReply> responseStream,
            ServerCallContext context)
        {
            // Streams all found countries to the client
            var countries = await _countryManagementService.GetAllAsync();
            foreach (var country in countries)
                await responseStream.WriteAsync(country);

            await Task.CompletedTask;
        }

        public override async Task<CountryReply> Get(CountryIdRequest request, ServerCallContext context)
        {
            // Send a single country to the client in the gRPC response
            return await _countryManagementService.GetAsync(request);
        }

        public override async Task<Empty> Delete(IAsyncStreamReader<CountryIdRequest> requestStream,
            ServerCallContext context)
        {
            // Read and store all streamed input messages
            var countryIdRequestList = new List<CountryIdRequest>();

            await foreach (var countryIdRequest in requestStream.ReadAllAsync())
                countryIdRequestList.Add(countryIdRequest);

            // Delete in all streamed countries
            await _countryManagementService.DeleteAsync(countryIdRequestList);

            return new Empty();
        }

        public override async Task<Empty> Update(CountryUpdateRequest request, ServerCallContext context)
        {
            await _countryManagementService.UpdateAsync(request);

            //Notic: in grpc funciton must have return type.
            //if funciton doesnt have any thing to return(it is void),it must return empty object
            return new Empty();
        }

        public override async Task Create(IAsyncStreamReader<CountryCreationRequest> requestStream,
            IServerStreamWriter<CountryCreationReply> responseStream, ServerCallContext context)
        {
            //Read and store all streamed input messages before performing any action
            var countryCreationRequestList = new List<CountryCreationRequest>();
            await foreach (var countryCreationRequest in requestStream.ReadAllAsync())
                countryCreationRequestList.Add(countryCreationRequest);

            //create all coutries 
            var createdCountries = await _countryManagementService.CreateAsync(countryCreationRequestList);

            //Stream all created countries to the client
            foreach (var country in createdCountries)
                await responseStream.WriteAsync(country);
        }
    }
}
