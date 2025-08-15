using IdeaCenterExamPrep.Models;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using RestSharp;
using RestSharp.Authenticators;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;

namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private static RestClient? _client;
        public const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84/";
        const string email = "examprep@lqto1234.com";
        const string password = "test1234";
        public required string staticToken;
        public string? lastCreatedIdeaId;

        [OneTimeSetUp]
        public void Setup()
        {
            _client = new RestClient(baseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = _client.Execute(request);
            var token = GetAuthToken();

            staticToken = token;

            if (string.IsNullOrEmpty(staticToken))
            {
                throw new Exception("Authentication failed, token is null or empty.");
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(staticToken),
            };
            _client = new RestClient(options);
            //Option 2. _client = new RestClient("baseUrl", "email", "password");
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _client?.Dispose();
        }

        [Test, Order(1)]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            //Create a test to send a POST request to add a new idea.
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "Description2234",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = _client.Execute(request);
            var createResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //Assert that the response status code is OK (200)
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            //Assert that the response message indicates the idea was "Successfully created!".
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void Get_All_Ideas()
        {
            //Create a test to send a GET request to list all ideas.
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = _client.Execute(request);
            var responsItems = System.Text.Json.JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            //Assert that the response status code is OK(200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            //Assert that the response contains a non-empty array.
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);

            //Store the id of the last created idea in a static member of the test class to maintain its value between test runs.
            lastCreatedIdeaId = responsItems.LastOrDefault()?.Id;

        }
        [Test, Order(3)]
        public void Edit_LastIdea()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "Description: This is an updated test idea",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = _client.Execute(request);
            var editResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //Assert that the response status code is OK (200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            //Assert that the response message indicates the idea was "Successfully edited".
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));

        }
        [Test, Order(4)]
        public void Delete_LastIdea_ShouldReturnSuccess()
        {
            //Create a test that sends a DELETE request.
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);

            //Use the id that you stored in the "Get All Ideas" request as a query parameter.
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = _client.Execute(request);

            
             //Assert that the response status code is OK (200).
            //Confirm that the response contains "The idea is deleted!". 
            //Keep in mind that the response in not a json object, but a string!
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Test, Order(5)]
        public void CreateIdea_Without_RequiredFields()
        {
            //Write a test that attempts to create a idea with missing required fields (Title, Description).
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = "",
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = _client.Execute(request);

            //Assert that the response status code is BadRequest (400).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Test, Order(6)]
        public void Edit_Non_existingIdea()
        {
            //Write a test to send a PUT request to edit an Idea with a ideaId that does not exist.
            string nonExistingideaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Non_existing Idea",
                Description = "Description:Edit Non_existing Idea",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingideaId);
            request.AddJsonBody(editRequest);
            var response = _client.Execute(request);

            //Assert that the response status code is BadRequest(400).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            //Assert that the response contains "There is no such idea!".
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Test, Order(7)]
        public void Delete_Non_existingIdea()
        {
            string nonExistingideaId = "123";
            //Write a test to send a DELETE request to edit an Idea with a ideaId that does not exist.
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingideaId);
            var response = _client.Execute(request);

            //Assert that the response contains "There is no such idea!".
            Assert.That(response.Content, Does.Contain("There is no such idea!"));

            //Assert that the response status code is BadRequest (400).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }
        
        private static string? GetAuthToken()
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(response.Content))
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(response.Content);
                if (json.TryGetProperty("accessToken", out var tokenElement) &&
                    tokenElement.ValueKind == JsonValueKind.String)
                {
                    return tokenElement.GetString();
                }
            }

            return null;
        }
    }
    }