using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace AzureProxy
{
    // Azure Function for Photon Services user authentication via Unity Authentication Service
    public class Auth
    {
        // Logger instance
        private readonly ILogger _logger;

        // Constructor to initialize logger
        public Auth(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Auth>();
        }

        // HTTP trigger function
        [Function("Auth")]
        // Handles GET requests for Photon user authentication
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve token and id from query parameters
            var queryParameters = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            // Get token and id from query parameters
            string? token = queryParameters?["token"];
            string? id = queryParameters?["id"];
            _logger.LogInformation($"Inpud data: Token: {token}, Id: {id}");

            // Validate input parameters
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(id))
            {
                _logger.LogError("Token and Id are both required");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                // Return the expected error response if token or id is missing
                badRequestResponse.WriteAsJsonAsync(new { ResultCode = 2, Message = "Token and Id are both required" });
                return badRequestResponse;
            }

            // Authenticate using Unity Authentication Service
            bool isAuthenticated = AuthenticateWithUnity(token, id);
            // Create response based on authentication result
            var response = req.CreateResponse();
            if (isAuthenticated)
            {
                _logger.LogInformation("User authenticated successfully");
                response.StatusCode = HttpStatusCode.OK;
                // Return success response with user id
                // and ResultCode 1 as per Photon requirements
                response.WriteAsJsonAsync(new { ResultCode = 1, UserId = id });
            }
            else
            {
                _logger.LogError("User authentication failed");
                // Set status code to OK even on failure as per Photon requirements
                response.StatusCode = HttpStatusCode.OK;
                // Return failure response with ResultCode 2
                response.WriteAsJsonAsync(new { ResultCode = 2 });
            }

            return response;
        }

        // Method to authenticate with Unity Authentication Service
        private bool AuthenticateWithUnity(string token, string playerId)
        {
            // Create REST client and request
            var client = new RestClient($"https://social.services.api.unity.com/v1/names/{playerId}");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", "Bearer " + token);

            try
            {
                var response = client.Execute(request);
                return response.IsSuccessful;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error contacting Unity Authentication Service: {ex.Message}");
                return false;
            }
        }
    }
}
