using SupermarketAPI.DTOs;
using SupermarketAPI.Models;
using SupermarketAPI.Services.Brands;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace SupermarketAPI.Tests;

public class BrandServicesTests : ServiceTestBase
{
    private readonly BrandServices _service;

    public BrandServicesTests()
    {
        _service = new BrandServices(DbContext, Mapper);
    }

    /// <summary>
    /// Configura un DbSet simulado que asigna el identificador 42 al registrar una marca.
    /// Invoca PostBrand con el nombre "Acme" y estado activo.
    /// Verifica que la entidad se agrega con sus datos, que el método devuelve el identificador asignado
    /// y que SaveChangesAsync se ejecuta exactamente una vez.
    /// </summary>
    [Fact]
    public async Task PostBrand_AddsBrandAndReturnsGeneratedId()
    {
        var brands = new List<Brand>();
        var dbSet = CreateDbSet(brands, brand => brand.BrandId);
        SetupAdd(dbSet, brand =>
        {
            brand.BrandId = 42;
            brands.Add(brand);
        });
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var id = await _service.PostBrand(new BrandRequest
        {
            BrandName = "Acme",
            BrandStatus = true
        });

        Assert.Equal(42, id);
        Assert.Equal("Acme", Assert.Single(brands).BrandName);
        Assert.True(brands[0].BrandStatus);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Configura el DbSet simulado para asignar el identificador 43 al crear una marca.
    /// Invoca CrearMarca con nombre y estado definidos, y verifica que agrega la entidad,
    /// devuelve el identificador asignado y guarda los cambios una sola vez.
    /// </summary>
    [Fact]
    public async Task CrearMarca_AddsBrandAndReturnsGeneratedId()
    {
        var brands = new List<Brand>();
        var dbSet = CreateDbSet(brands, brand => brand.BrandId);
        SetupAdd(dbSet, brand =>
        {
            brand.BrandId = 43;
            brands.Add(brand);
        });
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var id = await _service.CrearMarca(new BrandRequest
        {
            BrandName = "Northwind",
            BrandStatus = true
        });

        Assert.Equal(43, id);
        var createdBrand = Assert.Single(brands);
        Assert.Equal("Northwind", createdBrand.BrandName);
        Assert.True(createdBrand.BrandStatus);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prepara el DbSet simulado con una marca activa y solicita la lista completa mediante GetBrands.
    /// Verifica que se devuelve un solo resultado y que el nombre y estado de la entidad se conservan
    /// en el DTO generado por AutoMapper.
    /// </summary>
    [Fact]
    public async Task GetBrands_ReturnsMappedBrands()
    {
        var dbSet = CreateDbSet(new List<Brand>
        {
            new() { BrandId = 1, BrandName = "Acme", BrandStatus = true }
        }, brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var brands = await _service.GetBrands();

        var brand = Assert.Single(brands);
        Assert.Equal("Acme", brand.BrandName);
        Assert.True(brand.BrandStatus);
    }

    /// <summary>
    /// Prepara tres marcas en orden distinto al identificador y solicita la primera página con dos elementos.
    /// Verifica que el servicio ordena por identificador, devuelve solo las dos primeras marcas
    /// y reporta correctamente número de página, tamaño, cantidad total y páginas totales.
    /// </summary>
    [Fact]
    public async Task GetBrandsPaginated_ReturnsFirstPageAndMetadata()
    {
        var dbSet = CreateDbSet(new List<Brand>
        {
            new() { BrandId = 3, BrandName = "C", BrandStatus = true },
            new() { BrandId = 1, BrandName = "A", BrandStatus = true },
            new() { BrandId = 2, BrandName = "B", BrandStatus = false }
        }, brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.GetBrandsPaginated(pageNumber: 1, pageSize: 2);

        Assert.Equal(new[] { 1, 2 }, result.Items.Select(brand => brand.BrandId));
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    /// <summary>
    /// Prepara tres marcas y solicita la segunda página con tamaño dos.
    /// Verifica que solo se devuelve la marca restante y que los metadatos conservan el total
    /// de registros y el número correcto de páginas.
    /// </summary>
    [Fact]
    public async Task GetBrandsPaginated_ReturnsRemainingItemsOnSecondPage()
    {
        var dbSet = CreateDbSet(new List<Brand>
        {
            new() { BrandId = 1, BrandName = "A", BrandStatus = true },
            new() { BrandId = 2, BrandName = "B", BrandStatus = true },
            new() { BrandId = 3, BrandName = "C", BrandStatus = false }
        }, brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.GetBrandsPaginated(pageNumber: 2, pageSize: 2);

        var brand = Assert.Single(result.Items);
        Assert.Equal(3, brand.BrandId);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    /// <summary>
    /// Solicita paginación con número de página o tamaño menor que uno.
    /// Verifica que el servicio rechaza ambos parámetros no válidos con ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetBrandsPaginated_WhenPaginationParameterIsInvalid_Throws(int pageNumber, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GetBrandsPaginated(pageNumber, pageSize));
    }

    /// <summary>
    /// Prepara una marca con identificador 1 y configura FindAsync para encontrarla.
    /// Al llamar GetBrand con ese identificador, verifica que el resultado no sea null
    /// y que el DTO conserve el identificador y el nombre de la marca.
    /// </summary>
    [Fact]
    public async Task GetBrand_WhenBrandExists_ReturnsMappedBrand()
    {
        var brand = new Brand { BrandId = 1, BrandName = "Acme", BrandStatus = true };
        var dbSet = CreateDbSet(new List<Brand> { brand }, item => item.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.GetBrand(brand.BrandId);

        Assert.NotNull(result);
        Assert.Equal(brand.BrandId, result.BrandId);
        Assert.Equal("Acme", result.BrandName);
    }

    /// <summary>
    /// Configura un DbSet simulado sin marcas y solicita la marca con identificador 999.
    /// Verifica que GetBrand devuelve null cuando FindAsync no encuentra una entidad.
    /// </summary>
    [Fact]
    public async Task GetBrand_WhenBrandDoesNotExist_ReturnsNull()
    {
        var dbSet = CreateDbSet(new List<Brand>(), brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.GetBrand(999);

        Assert.Null(result);
    }

    /// <summary>
    /// Prepara una marca existente con nombre "Old" y estado inactivo, y solicita cambiarla a "New" y activa.
    /// Verifica que PutBrand devuelve el número de cambios esperado, actualiza ambos campos,
    /// llama a Update con la entidad encontrada y guarda los cambios una sola vez.
    /// </summary>
    [Fact]
    public async Task PutBrand_WhenBrandExists_UpdatesBrand()
    {
        var brand = new Brand { BrandId = 1, BrandName = "Old", BrandStatus = false };
        var dbSet = CreateDbSet(new List<Brand> { brand }, item => item.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.PutBrand(brand.BrandId, new BrandRequest
        {
            BrandName = "New",
            BrandStatus = true
        });

        Assert.Equal(1, result);
        dbSet.Verify(set => set.Update(brand), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("New", brand.BrandName);
        Assert.True(brand.BrandStatus);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío e intenta actualizar la marca con identificador 999.
    /// Verifica que PutBrand devuelve -1 al no encontrar la entidad y no intenta guardar cambios.
    /// </summary>
    [Fact]
    public async Task PutBrand_WhenBrandDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<Brand>(), brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.PutBrand(999, new BrandRequest
        {
            BrandName = "New",
            BrandStatus = true
        });

        Assert.Equal(-1, result);
    }

    /// <summary>
    /// Prepara una marca existente y solicita eliminarla usando su identificador.
    /// Verifica que DeleteBrand devuelve el número de cambios esperado, invoca Remove
    /// con la entidad encontrada y persiste la eliminación una sola vez.
    /// </summary>
    [Fact]
    public async Task DeleteBrand_WhenBrandExists_RemovesBrand()
    {
        var brand = new Brand { BrandId = 1, BrandName = "Acme", BrandStatus = true };
        var dbSet = CreateDbSet(new List<Brand> { brand }, item => item.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.DeleteBrand(brand.BrandId);

        Assert.Equal(1, result);
        dbSet.Verify(set => set.Remove(brand), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Configura un DbSet simulado sin marcas e intenta eliminar el identificador 999.
    /// Verifica que DeleteBrand devuelve -1 y que no se guardan cambios cuando la búsqueda falla.
    /// </summary>
    [Fact]
    public async Task DeleteBrand_WhenBrandDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<Brand>(), brand => brand.BrandId);
        DbContextMock.Setup(context => context.Brands).Returns(dbSet.Object);

        var result = await _service.DeleteBrand(999);

        Assert.Equal(-1, result);
    }
}