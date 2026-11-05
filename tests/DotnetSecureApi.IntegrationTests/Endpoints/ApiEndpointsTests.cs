using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetSecureApi.Api.Controllers;
using DotnetSecureApi.IntegrationTests.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace DotnetSecureApi.IntegrationTests.Endpoints;

public class ApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var request = new LoginRequest("admin", "AdminPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseBody);
        json.RootElement.TryGetProperty("token", out var tokenElement).Should().BeTrue();
        tokenElement.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("admin", "wrong-password");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("admin", "");
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problemDetails.Errors.Should().ContainKey("Password");
        problemDetails.Errors["Password"].Should().Contain("'Password' must not be empty.");
    }

    [Fact]
    public async Task ProtectedData_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/protected/data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedData_WithValidUserToken_ReturnsOk()
    {
        // Arrange
        var token = TokenGenerator.GenerateJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/protected/data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This is protected data for any authenticated user.");
    }

    [Fact]
    public async Task AdminData_WithAdminToken_ReturnsOk()
    {
        // Arrange
        var token = TokenGenerator.GenerateJwtToken("adminuser", new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/protected/admin-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This is sensitive data only for users with the 'Admin' role.");
    }

    [Fact]
    public async Task AdminData_WithUserToken_ReturnsForbidden()
    {
        // Arrange
        var token = TokenGenerator.GenerateJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/protected/admin-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserAccessibleData_WithUserToken_ReturnsOk()
    {
        // Arrange
        var token = TokenGenerator.GenerateJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/protected/user-accessible-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This data is accessible by users with 'Admin' or 'User' roles.");
    }
    
    [Fact]
    public async Task UserAccessibleData_WithAdminToken_ReturnsOk()
    {
        // Arrange
        var token = TokenGenerator.GenerateJwtToken("adminuser", new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/protected/user-accessible-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This data is accessible by users with 'Admin' or 'User' roles.");
    }
}
