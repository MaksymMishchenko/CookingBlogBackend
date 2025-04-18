﻿using PostApiService.Models;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Filters
{
    public class ModelValidationIntegrationTests : IClassFixture<FilterFixture>
    {
        private readonly HttpClient? _client;      

        public ModelValidationIntegrationTests(FilterFixture fixture)
        {
            _client = fixture.Client;           
        }

        [Fact]
        public async Task ValidateModelAttribute_ReturnsBadRequest_WhenModelIsInvalid()
        {
            // Arrange           
            var invalidLogin = new LoginUser { UserName = "test" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", invalidLogin);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
