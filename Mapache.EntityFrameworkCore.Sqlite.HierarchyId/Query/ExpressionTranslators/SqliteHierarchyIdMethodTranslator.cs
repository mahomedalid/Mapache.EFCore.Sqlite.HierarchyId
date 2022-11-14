using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

namespace Microsoft.EntityFrameworkCore.Sqlite.Query.ExpressionTranslators
{
    internal class SqliteHierarchyIdMethodTranslator : IMethodCallTranslator
    {
        private const char LikeEscapeChar = '\\';

        private static readonly MethodInfo _concat
            = typeof(string).GetRuntimeMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });

        private static readonly IDictionary<MethodInfo, string> _methodToFunctionName = new Dictionary<MethodInfo, string>
        {
            // instance methods
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.GetAncestor), new[] { typeof(int) }), "GetAncestor" },
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.GetDescendant), new[] { typeof(HierarchyId), typeof(HierarchyId) }), "GetDescendant" },
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.GetLevel), Type.EmptyTypes), "GetLevel" },
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.GetReparentedValue), new[] { typeof(HierarchyId), typeof(HierarchyId) }), "GetReparentedValue" },
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.IsDescendantOf), new[] { typeof(HierarchyId) }), "IsDescendantOf" },
            { typeof(object).GetRuntimeMethod(nameof(HierarchyId.ToString), Type.EmptyTypes), "ToString" },

            // static methods
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.GetRoot), Type.EmptyTypes), "hierarchyid::GetRoot" },
            { typeof(HierarchyId).GetRuntimeMethod(nameof(HierarchyId.Parse), new[] { typeof(string) }), "hierarchyid::Parse" },
        };

        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public SqliteHierarchyIdMethodTranslator(
            IRelationalTypeMappingSource typeMappingSource,
            ISqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public SqlExpression Translate(
            SqlExpression instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            // instance is null for static methods like Parse
            const string storeType = SqliteHierarchyIdTypeMappingSourcePlugin.SqliteTypeName;
            var callingType = instance?.Type ?? method.DeclaringType;

            if (typeof(HierarchyId).IsAssignableFrom(callingType)
                && method.Name == "IsDescendantOf")
            {
                var pattern = _sqlExpressionFactory.ApplyTypeMapping(arguments[0], new SqliteHierarchyIdTypeMapping("text", callingType, "IsDescendantOf"));

                return _sqlExpressionFactory.Like(instance, pattern);
            }

            if (typeof(HierarchyId).IsAssignableFrom(callingType)
                && _methodToFunctionName.TryGetValue(method, out var functionName))
            {
                var typeMappedArguments = new List<SqlExpression>();
                foreach (var argument in arguments)
                {
                    var argumentTypeMapping = typeof(HierarchyId).IsAssignableFrom(argument.Type)
                        ? _typeMappingSource.FindMapping(argument.Type, storeType)
                        : _typeMappingSource.FindMapping(argument.Type);
                    var mappedArgument = _sqlExpressionFactory.ApplyTypeMapping(argument, argumentTypeMapping);
                    typeMappedArguments.Add(mappedArgument);
                }

                var resultTypeMapping = typeof(HierarchyId).IsAssignableFrom(method.ReturnType)
                    ? _typeMappingSource.FindMapping(method.ReturnType, storeType)
                    : _typeMappingSource.FindMapping(method.ReturnType);

                if (instance != null)
                {
                    var instanceMapping = _typeMappingSource.FindMapping(instance.Type, storeType);
                    instance = _sqlExpressionFactory.ApplyTypeMapping(instance, instanceMapping);

                    return _sqlExpressionFactory.Function(
                        instance,
                        functionName,
                        simplify(arguments),
                        nullable: true,
                        instancePropagatesNullability: true,
                        argumentsPropagateNullability: arguments.Select(a => true),
                        method.ReturnType,
                        resultTypeMapping);
                }

                return _sqlExpressionFactory.Function(
                    functionName,
                    simplify(arguments),
                    nullable: true,
                    argumentsPropagateNullability: arguments.Select(a => true),
                    method.ReturnType,
                    resultTypeMapping);
            }

            return null;
        }

        private IEnumerable<SqlExpression> simplify(IEnumerable<SqlExpression> arguments)
        {
            foreach (var argument in arguments)
            {
                if (argument is SqlConstantExpression constant
                    && constant.Value is HierarchyId hierarchyId)
                {
                    yield return _sqlExpressionFactory.Fragment($"'{hierarchyId}'");
                }
                else
                {
                    yield return argument;
                }
            }
        }


        private SqlExpression TranslateStartsEndsWith(SqlExpression instance, SqlExpression pattern, bool startsWith)
        {
            var stringTypeMapping = ExpressionExtensions.InferTypeMapping(instance, pattern);

            instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);
            pattern = _sqlExpressionFactory.ApplyTypeMapping(pattern, stringTypeMapping);

            if (pattern is SqlConstantExpression constantExpression)
            {
                // The pattern is constant. Aside from null or empty, we escape all special characters (%, _, \)
                // in C# and send a simple LIKE
                if (!(constantExpression.Value is string constantString))
                {
                    return _sqlExpressionFactory.Like(instance, _sqlExpressionFactory.Constant(null, stringTypeMapping));
                }

                if (constantString.Length == 0)
                {
                    return _sqlExpressionFactory.Constant(true);
                }

                return constantString.Any(c => IsLikeWildChar(c))
                    ? _sqlExpressionFactory.Like(
                        instance,
                        _sqlExpressionFactory.Constant(
                            startsWith
                                ? EscapeLikePattern(constantString) + '%'
                                : '%' + EscapeLikePattern(constantString)),
                        _sqlExpressionFactory.Constant(
                            LikeEscapeChar.ToString())) // SQL Server has no char mapping, avoid value conversion warning)
                    : _sqlExpressionFactory.Like(
                        instance,
                        _sqlExpressionFactory.Constant(startsWith ? constantString + '%' : '%' + constantString));
            }

            // The pattern is non-constant, we use LEFT or RIGHT to extract substring and compare.
            // For StartsWith we also first run a LIKE to quickly filter out most non-matching results (sargable, but imprecise
            // because of wildcards).
            if (startsWith)
            {
                return _sqlExpressionFactory.OrElse(
                    _sqlExpressionFactory.AndAlso(
                        _sqlExpressionFactory.Like(
                            instance,
                            _sqlExpressionFactory.Add(
                                pattern,
                                _sqlExpressionFactory.Constant("%"))),
                        _sqlExpressionFactory.Equal(
                            _sqlExpressionFactory.Function(
                                "substr",
                                new[]
                                {
                                instance,
                                _sqlExpressionFactory.Constant(1),
                                _sqlExpressionFactory.Function(
                                    "length",
                                    new[] { pattern },
                                    nullable: true,
                                    argumentsPropagateNullability: new[] { true },
                                    typeof(int))
                                },
                                nullable: true,
                                argumentsPropagateNullability: new[] { true, false, true },
                                typeof(string),
                                stringTypeMapping),
                            pattern)),
                    _sqlExpressionFactory.Equal(
                        pattern,
                        _sqlExpressionFactory.Constant(string.Empty)));
            }

            return _sqlExpressionFactory.OrElse(
                _sqlExpressionFactory.Equal(
                    _sqlExpressionFactory.Function(
                        "substr",
                        new[]
                        {
                        instance,
                        _sqlExpressionFactory.Negate(
                            _sqlExpressionFactory.Function(
                                "length",
                                new[] { pattern },
                                nullable: true,
                                argumentsPropagateNullability: new[] { true },
                                typeof(int)))
                        },
                        nullable: true,
                        argumentsPropagateNullability: new[] { true, true },
                        typeof(string),
                        stringTypeMapping),
                    pattern),
                _sqlExpressionFactory.Equal(
                    pattern,
                    _sqlExpressionFactory.Constant(string.Empty)));
        }

        // See https://www.sqlite.org/lang_expr.html
        private static bool IsLikeWildChar(char c)
            => c == '%' || c == '_';

        private static string EscapeLikePattern(string pattern)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                if (IsLikeWildChar(c)
                    || c == LikeEscapeChar)
                {
                    builder.Append(LikeEscapeChar);
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private SqlExpression? ProcessTrimMethod(SqlExpression instance, IReadOnlyList<SqlExpression> arguments, string functionName)
        {
            var typeMapping = instance.TypeMapping;
            if (typeMapping == null)
            {
                return null;
            }

            var sqlArguments = new List<SqlExpression> { instance };
            if (arguments.Count == 1)
            {
                var constantValue = (arguments[0] as SqlConstantExpression)?.Value;
                var charactersToTrim = new List<char>();

                if (constantValue is char singleChar)
                {
                    charactersToTrim.Add(singleChar);
                }
                else if (constantValue is char[] charArray)
                {
                    charactersToTrim.AddRange(charArray);
                }
                else
                {
                    return null;
                }

                if (charactersToTrim.Count > 0)
                {
                    sqlArguments.Add(_sqlExpressionFactory.Constant(new string(charactersToTrim.ToArray()), typeMapping));
                }
            }

            return _sqlExpressionFactory.Function(
                functionName,
                sqlArguments,
                nullable: true,
                argumentsPropagateNullability: sqlArguments.Select(_ => true).ToList(),
                typeof(string),
                typeMapping);
        }
    }
}