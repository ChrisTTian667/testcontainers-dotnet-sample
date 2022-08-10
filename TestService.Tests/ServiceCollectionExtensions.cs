using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TestService.Tests;

public static class ServiceCollectionExtensions
{
    public static void RevertDbContextFactoryRegistration<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<DbContextOptions<TDbContext>>();
        services.RemoveAll<IDbContextFactory<TDbContext>>();
        services.RemoveAll<IDbContextFactorySource<TDbContext>>();
    }
}