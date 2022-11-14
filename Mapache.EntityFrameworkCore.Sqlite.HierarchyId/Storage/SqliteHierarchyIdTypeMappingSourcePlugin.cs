using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Sqlite.Storage
{
    internal class SqliteHierarchyIdTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
    {
        public const string SqliteTypeName = "text";

        public virtual RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            var storeTypeName = mappingInfo.StoreTypeName;

            return typeof(HierarchyId).IsAssignableFrom(clrType)
                ? new SqliteHierarchyIdTypeMapping(SqliteTypeName, clrType ?? typeof(HierarchyId))
                : null;
        }
    }
}