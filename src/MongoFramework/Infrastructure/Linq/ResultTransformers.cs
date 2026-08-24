using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace MongoFramework.Infrastructure.Linq
{
	public static class ResultTransformers
	{
		public static Expression Transform(Expression expression, Type sourceType, bool isAsync)
		{
			if (expression is MethodCallExpression methodCallExpression)
			{
				var transformName = methodCallExpression.Method.Name;
				if (isAsync)
				{
					var sourceParameter = Expression.Parameter(
						typeof(IAsyncEnumerable<>).MakeGenericType(sourceType),
						"source"
					);
					var cancellationTokenParameter = Expression.Parameter(
						typeof(CancellationToken),
						"cancellationToken"
					);

					var methodInfo = (transformName switch
					{
						nameof(Queryable.First) => MethodInfoCache.AsyncEnumerable.First_1,
						nameof(Queryable.FirstOrDefault) => MethodInfoCache.AsyncEnumerable.FirstOrDefault_1,

						nameof(Queryable.Single) => MethodInfoCache.AsyncEnumerable.Single_1,
						nameof(Queryable.SingleOrDefault) => MethodInfoCache.AsyncEnumerable.SingleOrDefault_1,

						nameof(Queryable.Count) => MethodInfoCache.AsyncEnumerable.Count_1,
						nameof(Queryable.Max) => MethodInfoCache.AsyncEnumerable.Max_1,
						nameof(Queryable.Min) => MethodInfoCache.AsyncEnumerable.Min_1,
						nameof(Queryable.Sum) => GetAsyncSumMethod(sourceType),

						nameof(Queryable.Any) => MethodInfoCache.AsyncEnumerable.Any_1,

						_ => throw new InvalidOperationException($"No transform available for {transformName}")
					});

					methodInfo = ConstructMethod(methodInfo, sourceType);

					return Expression.Lambda(
						Expression.Call(
							null,
							methodInfo,
							sourceParameter,
							cancellationTokenParameter
						),
						sourceParameter,
						cancellationTokenParameter
					);
				}
				else
				{
					var sourceParameter = Expression.Parameter(
						typeof(IEnumerable<>).MakeGenericType(sourceType),
						"source"
					);

					var methodInfo = (transformName switch
					{
						nameof(Queryable.First) => MethodInfoCache.Enumerable.First_1,
						nameof(Queryable.FirstOrDefault) => MethodInfoCache.Enumerable.FirstOrDefault_1,

						nameof(Queryable.Single) => MethodInfoCache.Enumerable.Single_1,
						nameof(Queryable.SingleOrDefault) => MethodInfoCache.Enumerable.SingleOrDefault_1,

						nameof(Queryable.Count) => MethodInfoCache.Enumerable.Count_1,
						nameof(Queryable.Max) => MethodInfoCache.Enumerable.Max_1,
						nameof(Queryable.Min) => MethodInfoCache.Enumerable.Min_1,
						nameof(Queryable.Sum) => GetSumMethod(sourceType),

						nameof(Queryable.Any) => MethodInfoCache.Enumerable.Any_1,

						_ => throw new InvalidOperationException($"No transform available for {transformName}")
					});

					methodInfo = ConstructMethod(methodInfo, sourceType);

					return Expression.Lambda(
						Expression.Call(
							null,
							methodInfo,
							sourceParameter
						),
						sourceParameter
					);
				}
			}

			throw new InvalidOperationException($"Result transformation unavailable for expression type {expression.NodeType}");
		}

		private static MethodInfo GetSumMethod(Type sourceType)
		{
			if (sourceType == typeof(int)) return MethodInfoCache.Enumerable.Sum_Int32_1;
			if (sourceType == typeof(int?)) return MethodInfoCache.Enumerable.Sum_NullableInt32_1;
			if (sourceType == typeof(decimal)) return MethodInfoCache.Enumerable.Sum_Decimal_1;
			if (sourceType == typeof(decimal?)) return MethodInfoCache.Enumerable.Sum_NullableDecimal_1;
			if (sourceType == typeof(double)) return MethodInfoCache.Enumerable.Sum_Double_1;
			if (sourceType == typeof(double?)) return MethodInfoCache.Enumerable.Sum_NullableDouble_1;
			if (sourceType == typeof(float)) return MethodInfoCache.Enumerable.Sum_Float_1;
			if (sourceType == typeof(float?)) return MethodInfoCache.Enumerable.Sum_NullableFloat_1;
			if (sourceType == typeof(long)) return MethodInfoCache.Enumerable.Sum_Long_1;
			if (sourceType == typeof(long?)) return MethodInfoCache.Enumerable.Sum_NullableLong_1;

			throw new InvalidOperationException($"No sum transform available for {sourceType}");
		}

		private static MethodInfo GetAsyncSumMethod(Type sourceType)
		{
			if (sourceType == typeof(int)) return MethodInfoCache.AsyncEnumerable.Sum_Int32_1;
			if (sourceType == typeof(int?)) return MethodInfoCache.AsyncEnumerable.Sum_NullableInt32_1;
			if (sourceType == typeof(decimal)) return MethodInfoCache.AsyncEnumerable.Sum_Decimal_1;
			if (sourceType == typeof(decimal?)) return MethodInfoCache.AsyncEnumerable.Sum_NullableDecimal_1;
			if (sourceType == typeof(double)) return MethodInfoCache.AsyncEnumerable.Sum_Double_1;
			if (sourceType == typeof(double?)) return MethodInfoCache.AsyncEnumerable.Sum_NullableDouble_1;
			if (sourceType == typeof(float)) return MethodInfoCache.AsyncEnumerable.Sum_Float_1;
			if (sourceType == typeof(float?)) return MethodInfoCache.AsyncEnumerable.Sum_NullableFloat_1;
			if (sourceType == typeof(long)) return MethodInfoCache.AsyncEnumerable.Sum_Long_1;
			if (sourceType == typeof(long?)) return MethodInfoCache.AsyncEnumerable.Sum_NullableLong_1;

			throw new InvalidOperationException($"No async sum transform available for {sourceType}");
		}

		private static MethodInfo ConstructMethod(MethodInfo methodInfo, Type sourceType)
		{
			return methodInfo.IsGenericMethodDefinition ? methodInfo.MakeGenericMethod(sourceType) : methodInfo;
		}
	}
}
