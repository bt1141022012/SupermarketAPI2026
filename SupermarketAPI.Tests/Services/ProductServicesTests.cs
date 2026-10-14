using SupermarketAPI.DTOs;
using SupermarketAPI.Models;
using SupermarketAPI.Services.Products;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace SupermarketAPI.Tests;

public class ProductServicesTests : ServiceTestBase
{
    private readonly ProductServices _service;

    public ProductServicesTests()
    {
        _service = new ProductServices(DbContext, Mapper);
    }

    /// <summary>
    /// Configura el DbSet simulado para asignar el identificador 42 al nuevo producto.
    /// Invoca PostProduct con usuario, nombre, precio y estado definidos.
    /// Verifica que el producto se agrega con sus datos principales, que se devuelve el identificador asignado
    /// y que SaveChangesAsync se ejecuta exactamente una vez.
    /// </summary>
    [Fact]
    public async Task PostProduct_AddsProductAndReturnsGeneratedId()
    {
        var productsData = new List<Product>();
        var dbSet = CreateDbSet(productsData, product => product.ProductId);
        SetupAdd(dbSet, product =>
        {
            product.ProductId = 42;
            productsData.Add(product);
        });
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var id = await _service.PostProduct(new ProductRequest
        {
            UserId = 1,
            ProductName = "Milk",
            ProductPrice = 2.5m,
            ProductStatus = true
        });

        Assert.Equal(42, id);
        var product = Assert.Single(productsData);
        Assert.Equal("Milk", product.ProductName);
        Assert.Equal(2.5m, product.ProductPrice);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prepara un producto con su usuario relacionado y solicita todos los productos.
    /// Verifica que GetProducts devuelve un solo DTO y que el nombre del producto
    /// y el nombre del usuario asociado se mapean correctamente.
    /// </summary>
    [Fact]
    public async Task GetProducts_ReturnsMappedProductsWithUser()
    {
        var dbSet = CreateDbSet(new List<Product>
        {
            new()
            {
                ProductId = 1,
                UserId = 1,
                ProductName = "Milk",
                ProductPrice = 2.5m,
                ProductStatus = true,
                User = new User { UserId = 1, Username = "administrador", UserPassword = "secret", UserRole = "Admin" }
            }
        }, product => product.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var products = await _service.GetProducts();

        var product = Assert.Single(products);
        Assert.Equal("Milk", product.ProductName);
        Assert.Equal("administrador", product.User.Username);
    }

    /// <summary>
    /// Prepara un producto existente con identificador 1 y configura FindAsync para localizarlo.
    /// Verifica que GetProduct devuelve un DTO no nulo con el mismo identificador y nombre que la entidad.
    /// </summary>
    [Fact]
    public async Task GetProduct_WhenProductExists_ReturnsMappedProduct()
    {
        var product = new Product
        {
            ProductId = 1,
            UserId = 1,
            ProductName = "Milk",
            ProductPrice = 2.5m,
            ProductStatus = true
        };
        var dbSet = CreateDbSet(new List<Product> { product }, item => item.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.GetProduct(product.ProductId);

        Assert.NotNull(result);
        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal("Milk", result.ProductName);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío y solicita el producto con identificador 999.
    /// Verifica que GetProduct devuelve null cuando no existe una entidad que coincida.
    /// </summary>
    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsNull()
    {
        var dbSet = CreateDbSet(new List<Product>(), product => product.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.GetProduct(999);

        Assert.Null(result);
    }

    /// <summary>
    /// Prepara un producto con valores iniciales y solicita cambiar nombre, precio, estado y usuario.
    /// Verifica que PutProduct devuelve el número de cambios esperado, actualiza las propiedades,
    /// llama a Update con el producto localizado y persiste los cambios una sola vez.
    /// </summary>
    [Fact]
    public async Task PutProduct_WhenProductExists_UpdatesProduct()
    {
        var product = new Product
        {
            ProductId = 1,
            UserId = 1,
            ProductName = "Old",
            ProductPrice = 1m,
            ProductStatus = false
        };
        var dbSet = CreateDbSet(new List<Product> { product }, item => item.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.PutProduct(product.ProductId, new ProductRequest
        {
            UserId = 1,
            ProductName = "New",
            ProductPrice = 3m,
            ProductStatus = true
        });

        Assert.Equal(1, result);
        Assert.Equal("New", product.ProductName);
        Assert.Equal(3m, product.ProductPrice);
        Assert.True(product.ProductStatus);
        dbSet.Verify(set => set.Update(product), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío e intenta actualizar el producto con identificador 999.
    /// Verifica que PutProduct devuelve -1 y que no intenta persistir cambios si no encuentra el producto.
    /// </summary>
    [Fact]
    public async Task PutProduct_WhenProductDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<Product>(), product => product.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.PutProduct(999, new ProductRequest
        {
            UserId = 1,
            ProductName = "New",
            ProductPrice = 3m,
            ProductStatus = true
        });

        Assert.Equal(-1, result);
    }

    /// <summary>
    /// Prepara un producto existente y solicita eliminarlo usando su identificador.
    /// Verifica que DeleteProduct devuelve el número de cambios esperado, llama a Remove
    /// con la entidad localizada y guarda los cambios una sola vez.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_WhenProductExists_RemovesProduct()
    {
        var product = new Product
        {
            ProductId = 1,
            UserId = 1,
            ProductName = "Milk",
            ProductPrice = 2.5m
        };
        var dbSet = CreateDbSet(new List<Product> { product }, item => item.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.DeleteProduct(product.ProductId);

        Assert.Equal(1, result);
        dbSet.Verify(set => set.Remove(product), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío e intenta eliminar el producto con identificador 999.
    /// Verifica que DeleteProduct devuelve -1 y no guarda cambios cuando no encuentra la entidad.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_WhenProductDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<Product>(), product => product.ProductId);
        DbContextMock.Setup(context => context.Products).Returns(dbSet.Object);

        var result = await _service.DeleteProduct(999);

        Assert.Equal(-1, result);
    }
}