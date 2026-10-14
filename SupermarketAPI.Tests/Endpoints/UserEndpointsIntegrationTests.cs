using System.Net;
using System.Net.Http.Json;
using SupermarketAPI.DTOs;

namespace SupermarketAPI.Tests;

public class UserEndpointsIntegrationTests : IClassFixture<SupermarketApiFactory>
{
    private readonly SupermarketApiFactory _factory;

    public UserEndpointsIntegrationTests(SupermarketApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUpdateDeleteAndAuthenticateUser_ReturnsExpectedResponses()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var request = new UserRequest
        {
            Username = "integration-user",
            UserPassword = "password123",
            UserRole = "User"
        };

        var createResponse = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.NotNull(users);
        Assert.Contains(users, user => user.Username == "integration-user");

        var loginResponse = await client.PostAsJsonAsync("/api/users/login", request);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var token = await loginResponse.Content.ReadFromJsonAsync<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var updateResponse = await client.PutAsJsonAsync("/api/users/2", new UserRequest
        {
            Username = "updated-integration-user",
            UserPassword = "password456",
            UserRole = "Manager"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync("/api/users/2");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users/2")).StatusCode);
    }
}