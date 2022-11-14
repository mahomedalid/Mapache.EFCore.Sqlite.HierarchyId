using System.Data.SqlTypes;
using System.IO;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.EntityFrameworkCore.Sqlite.Storage
{
    internal class SqlliteHierarchyIdValueConverter : ValueConverter<HierarchyId, string>
    {
        public SqlliteHierarchyIdValueConverter()
            : base(h => toProvider(h), b => fromProvider(b))
        {
        }

        private static string toProvider(HierarchyId hid)
        {
            return hid.ToString();
        }

        private static HierarchyId fromProvider(SqlString input)
        {
            return HierarchyId.Parse(input.Value);
        }

        private static HierarchyId fromProvider(string input)
        {
            return HierarchyId.Parse(input);
        }
    }
}