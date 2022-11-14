using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// HierarchyId specific extension methods for <see cref="SqliteDbContextOptionsBuilder"/>.
    /// </summary>
    public static class SqliteHierarchyIdDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Enable HierarchyId mappings.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure in-memory database.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static SqliteDbContextOptionsBuilder UseHierarchyId(
           this SqliteDbContextOptionsBuilder optionsBuilder)
        {
            var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;
            var infrastructure = (IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder;

            var extension = coreOptionsBuilder.Options.FindExtension<SqliteHierarchyIdOptionsExtension>()
                ?? new SqliteHierarchyIdOptionsExtension();

            infrastructure.AddOrUpdateExtension(extension);

            return optionsBuilder;
        }
    }
}
