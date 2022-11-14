using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Sqlite.Query.ExpressionTranslators
{
    internal class SqliteHierarchyIdMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {
        public SqliteHierarchyIdMethodCallTranslatorPlugin(
            IRelationalTypeMappingSource typeMappingSource,
            ISqlExpressionFactory sqlExpressionFactory)
        {
            Translators = new IMethodCallTranslator[]
            {
                new SqliteHierarchyIdMethodTranslator(typeMappingSource, sqlExpressionFactory)
            };
        }

        public virtual IEnumerable<IMethodCallTranslator> Translators { get; }
    }
}