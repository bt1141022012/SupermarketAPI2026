using System.Net;
using System.Net.Http.Json;
using SupermarketAPI.DTOs;

namespace SupermarketAPI.Tests;

public class BrandEndpointsIntegrationTests : IClassFixture<SupermarketApiFactory>
{
    private readonly SupermarketApiFactory _factory;

    public BrandEndpointsIntegrationTests(SupermarketApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUpdatePaginateAndDeleteBrand_ReturnsExpectedResponses()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/brands", new BrandRequest
        {
            BrandName = "Northwind",
            BrandStatus = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("api/brands/1", createResponse.Headers.Location?.OriginalString);

        var getResponse = await client.GetAsync("/api/brands/1");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var brand = await getResponse.Content.ReadFromJsonAsync<BrandResponse>();
        Assert.NotNull(brand);
        Assert.Equal("Northwind", brand.BrandName);

        var updateResponse = await client.PutAsJsonAsync("/api/brands/1", new BrandRequest
        {
            BrandName = "Northwind Updated",
            BrandStatus = false
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var paginationResponse = await client.GetAsync("/api/brands/paginated?pageNumber=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, paginationResponse.StatusCode);
        var page = await paginationResponse.Content.ReadFromJsonAsync<PaginationDTO<BrandResponse>>();
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Northwind Updated", Assert.Single(page.Items).BrandName);

        var deleteResponse = await client.DeleteAsync("/api/brands/1");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/brands/1")).StatusCode);
    }

    [Fact]
    public async Task GetBrands_RejectsAnonymousRequests()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/brands");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}