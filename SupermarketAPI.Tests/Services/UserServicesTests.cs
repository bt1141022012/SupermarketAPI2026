using SupermarketAPI.DTOs;
using SupermarketAPI.Models;
using SupermarketAPI.Services.Users;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace SupermarketAPI.Tests;

public class UserServicesTests : ServiceTestBase
{
    private readonly UserServices _service;

    public UserServicesTests()
    {
        _service = new UserServices(DbContext, Mapper);
    }

    /// <summary>
    /// Configura el DbSet simulado para registrar un usuario nuevo con nombre, contraseña y rol.
    /// Verifica que PostUser devuelve el número de cambios esperado, que la entidad agregada
    /// contiene el nombre enviado y que SaveChangesAsync se ejecuta una sola vez.
    /// </summary>
    [Fact]
    public async Task PostUser_AddsUser()
    {
        var usersData = new List<User>();
        var dbSet = CreateDbSet(usersData, user => user.UserId);
        SetupAdd(dbSet, user => usersData.Add(user));
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.PostUser(new UserRequest
        {
            Username = "new-user",
            UserPassword = "password",
            UserRole = "User"
        });

        Assert.Equal(1, result);
        Assert.Equal("new-user", Assert.Single(usersData).Username);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prepara un usuario en el DbSet simulado y solicita la lista completa mediante GetUsers.
    /// Verifica que se devuelve un único DTO y que mantiene el nombre del usuario.
    /// </summary>
    [Fact]
    public async Task GetUsers_ReturnsMappedUsers()
    {
        var dbSet = CreateDbSet(new List<User>
        {
            new() { UserId = 1, Username = "administrador", UserPassword = "admin2026", UserRole = "Administrador" }
        }, user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var users = await _service.GetUsers();

        var user = Assert.Single(users);
        Assert.Equal("administrador", user.Username);
    }

    /// <summary>
    /// Prepara el usuario con identificador 1 y configura FindAsync para encontrarlo.
    /// Verifica que GetUser devuelve un DTO no nulo cuyo nombre coincide con la entidad encontrada.
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsMappedUser()
    {
        var dbSet = CreateDbSet(new List<User>
        {
            new() { UserId = 1, Username = "administrador", UserPassword = "admin2026", UserRole = "Administrador" }
        }, user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.GetUser(1);

        Assert.NotNull(result);
        Assert.Equal("administrador", result.Username);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío y solicita el usuario con identificador 999.
    /// Verifica que GetUser devuelve null cuando no hay una entidad con ese identificador.
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ReturnsNull()
    {
        var dbSet = CreateDbSet(new List<User>(), user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.GetUser(999);

        Assert.Null(result);
    }

    /// <summary>
    /// Prepara un usuario con credenciales conocidas y llama a Login usando el mismo nombre y contraseña.
    /// Verifica que las credenciales válidas producen un DTO no nulo con el identificador del usuario.
    /// </summary>
    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsUser()
    {
        var dbSet = CreateDbSet(new List<User>
        {
            new() { UserId = 1, Username = "administrador", UserPassword = "admin2026", UserRole = "Administrador" }
        }, user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.Login(new UserRequest
        {
            Username = "administrador",
            UserPassword = "admin2026",
            UserRole = "Administrador"
        });

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
    }

    /// <summary>
    /// Configura un DbSet sin coincidencias para las credenciales enviadas a Login.
    /// Verifica que una contraseña incorrecta no autentica al usuario y que el resultado es null.
    /// </summary>
    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsNull()
    {
        var dbSet = CreateDbSet(new List<User>(), user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.Login(new UserRequest
        {
            Username = "administrador",
            UserPassword = "wrong-password",
            UserRole = "Administrador"
        });

        Assert.Null(result);
    }

    /// <summary>
    /// Prepara un usuario existente y solicita cambiar su nombre, contraseña y rol.
    /// Verifica que PutUser devuelve el número de cambios esperado, modifica los tres campos,
    /// llama a Update con el usuario localizado y persiste los cambios una sola vez.
    /// </summary>
    [Fact]
    public async Task PutUser_WhenUserExists_UpdatesUser()
    {
        var user = new User { UserId = 1, Username = "administrador", UserPassword = "admin2026", UserRole = "Administrador" };
        var dbSet = CreateDbSet(new List<User> { user }, item => item.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.PutUser(1, new UserRequest
        {
            Username = "updated-admin",
            UserPassword = "new-password",
            UserRole = "Manager"
        });

        Assert.Equal(1, result);
        dbSet.Verify(set => set.Update(user), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("updated-admin", user.Username);
        Assert.Equal("new-password", user.UserPassword);
        Assert.Equal("Manager", user.UserRole);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío e intenta actualizar el usuario con identificador 999.
    /// Verifica que PutUser devuelve -1 y no guarda cambios si la búsqueda no encuentra al usuario.
    /// </summary>
    [Fact]
    public async Task PutUser_WhenUserDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<User>(), user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.PutUser(999, new UserRequest
        {
            Username = "new-user",
            UserPassword = "password",
            UserRole = "User"
        });

        Assert.Equal(-1, result);
    }

    /// <summary>
    /// Prepara un usuario existente y solicita eliminarlo mediante su identificador.
    /// Verifica que DeleteUser devuelve el número de cambios esperado, llama a Remove
    /// con la entidad localizada y ejecuta SaveChangesAsync una sola vez.
    /// </summary>
    [Fact]
    public async Task DeleteUser_WhenUserExists_RemovesUser()
    {
        var user = new User { UserId = 1, Username = "administrador", UserPassword = "admin2026", UserRole = "Administrador" };
        var dbSet = CreateDbSet(new List<User> { user }, item => item.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.DeleteUser(1);

        Assert.Equal(1, result);
        dbSet.Verify(set => set.Remove(user), Times.Once);
        DbContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Configura un DbSet simulado vacío e intenta eliminar el usuario con identificador 999.
    /// Verifica que DeleteUser devuelve -1 y no intenta guardar cambios al no encontrar la entidad.
    /// </summary>
    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ReturnsMinusOne()
    {
        var dbSet = CreateDbSet(new List<User>(), user => user.UserId);
        DbContextMock.Setup(context => context.Users).Returns(dbSet.Object);

        var result = await _service.DeleteUser(999);

        Assert.Equal(-1, result);
    }
}