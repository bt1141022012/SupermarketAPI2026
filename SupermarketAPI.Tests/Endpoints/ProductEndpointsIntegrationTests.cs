using System.Net;
using System.Net.Http.Json;
using SupermarketAPI.DTOs;

namespace SupermarketAPI.Tests;

public class ProductEndpointsIntegrationTests : IClassFixture<SupermarketApiFactory>
{
    private readonly SupermarketApiFactory _factory;

    public ProductEndpointsIntegrationTests(SupermarketApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListUpdateAndDeleteProduct_ReturnsExpectedResponses()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/products", new ProductRequest
        {
            UserId = 1,
            ProductName = "Milk",
            ProductPrice = 2.5m,
            ProductStatus = true
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var products = await listResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        var product = Assert.Single(products);
        Assert.Equal("Milk", product.ProductName);
        Assert.Equal("administrador", product.User.Username);

        var updateResponse = await client.PutAsJsonAsync("/api/products/1", new ProductRequest
        {
            UserId = 1,
            ProductName = "Oat Milk",
            ProductPrice = 3m,
            ProductStatus = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/products/1")).StatusCode);
    }
}