using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;
using SupermarketAPI.Mappings;
using SupermarketAPI.Models;

namespace SupermarketAPI.Tests;

public abstract class ServiceTestBase
{
    protected readonly Mock<SupermarketDbContext> DbContextMock;
    protected readonly IMapper Mapper;

    protected ServiceTestBase()
    {
        DbContextMock = new Mock<SupermarketDbContext>();
        DbContextMock.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        Mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
    }

    protected SupermarketDbContext DbContext => DbContextMock.Object;

    protected Mock<DbSet<TEntity>> CreateDbSet<TEntity>(
        List<TEntity> entities,
        Func<TEntity, int> getId) where TEntity : class
    {
        var dbSet = entities.AsQueryable().BuildMockDbSet();
        dbSet.Setup(set => set.FindAsync(It.IsAny<object?[]>()))
            .Returns((object?[] keys) => new ValueTask<TEntity?>(
                keys.Length == 0 ? null : entities.FirstOrDefault(entity => getId(entity) == (int)keys[0]!)));
        return dbSet;
    }

    protected static void SetupAdd<TEntity>(Mock<DbSet<TEntity>> dbSet, Action<TEntity> onAdd)
        where TEntity : class
    {
        dbSet.Setup(set => set.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TEntity, CancellationToken>((entity, _) => onAdd(entity));
    }
}