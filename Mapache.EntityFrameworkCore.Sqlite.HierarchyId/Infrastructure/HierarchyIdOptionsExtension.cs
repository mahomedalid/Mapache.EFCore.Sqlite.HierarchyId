using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Sqlite.Infrastructure
{
    internal class SqliteHierarchyIdOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        public virtual void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkSqliteHierarchyId();
        }

        public virtual void Validate(IDbContextOptions options)
        {
            var internalServiceProvider = options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider;
            if (internalServiceProvider != null)
            {
                using var scope = internalServiceProvider.CreateScope();
                if (scope.ServiceProvider.GetService<IEnumerable<ITypeMappingSourcePlugin>>()
                       ?.Any(s => s is SqliteHierarchyIdTypeMappingSourcePlugin) != true)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            private new SqliteHierarchyIdOptionsExtension Extension
                => (SqliteHierarchyIdOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
                => other is ExtensionInfo;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Sqlite:" + nameof(SqliteHierarchyIdDbContextOptionsBuilderExtensions.UseHierarchyId)] = "1";

            public override string LogFragment => "using HierarchyId ";
        }
    }
}
