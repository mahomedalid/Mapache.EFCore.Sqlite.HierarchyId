using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Storage;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// EntityFrameworkCore.Sqlite.HierarchyId extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class SqliteHierarchyIdServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the services required for HierarchyId support in the in-memory database provider for Entity Framework.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddEntityFrameworkSqliteHierarchyId(
            this IServiceCollection serviceCollection)
        {
            new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<IMethodCallTranslatorPlugin, SqliteHierarchyIdMethodCallTranslatorPlugin>()
                .TryAdd<IRelationalTypeMappingSourcePlugin, SqliteHierarchyIdTypeMappingSourcePlugin>();
                
            return serviceCollection;
        }
    }
}
