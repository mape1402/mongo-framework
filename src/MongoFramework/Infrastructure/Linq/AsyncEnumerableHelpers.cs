using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoFramework.Infrastructure.Linq
{
	internal static class AsyncEnumerableHelpers
	{
		public static async ValueTask<TSource> FirstAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				return item;
			}

			throw new InvalidOperationException("Sequence contains no elements");
		}

		public static async ValueTask<TSource> FirstOrDefaultAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				return item;
			}

			return default;
		}

		public static async ValueTask<TSource> SingleAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			var hasValue = false;
			var result = default(TSource);
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (hasValue)
				{
					throw new InvalidOperationException("Sequence contains more than one element");
				}

				hasValue = true;
				result = item;
			}

			if (!hasValue)
			{
				throw new InvalidOperationException("Sequence contains no elements");
			}

			return result;
		}

		public static async ValueTask<TSource> SingleOrDefaultAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			var hasValue = false;
			var result = default(TSource);
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (hasValue)
				{
					throw new InvalidOperationException("Sequence contains more than one element");
				}

				hasValue = true;
				result = item;
			}

			return result;
		}

		public static async ValueTask<bool> AnyAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			await foreach (var _ in source.WithCancellation(cancellationToken))
			{
				return true;
			}

			return false;
		}

		public static async ValueTask<int> CountAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			var count = 0;
			await foreach (var _ in source.WithCancellation(cancellationToken))
			{
				count++;
			}

			return count;
		}

		public static async ValueTask<TSource> MaxAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Max();
		}

		public static async ValueTask<TSource> MinAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Min();
		}

		public static async ValueTask<int> SumAsync(IAsyncEnumerable<int> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Sum();
		}

		public static async ValueTask<int?> SumAsync(IAsyncEnumerable<int?> source, CancellationToken cancellationToken)
		{
			return EnumerableHelpers.Sum(await ToListAsync(source, cancellationToken));
		}

		public static async ValueTask<decimal> SumAsync(IAsyncEnumerable<decimal> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Sum();
		}

		public static async ValueTask<decimal?> SumAsync(IAsyncEnumerable<decimal?> source, CancellationToken cancellationToken)
		{
			return EnumerableHelpers.Sum(await ToListAsync(source, cancellationToken));
		}

		public static async ValueTask<double> SumAsync(IAsyncEnumerable<double> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Sum();
		}

		public static async ValueTask<double?> SumAsync(IAsyncEnumerable<double?> source, CancellationToken cancellationToken)
		{
			return EnumerableHelpers.Sum(await ToListAsync(source, cancellationToken));
		}

		public static async ValueTask<float> SumAsync(IAsyncEnumerable<float> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Sum();
		}

		public static async ValueTask<float?> SumAsync(IAsyncEnumerable<float?> source, CancellationToken cancellationToken)
		{
			return EnumerableHelpers.Sum(await ToListAsync(source, cancellationToken));
		}

		public static async ValueTask<long> SumAsync(IAsyncEnumerable<long> source, CancellationToken cancellationToken)
		{
			return (await ToListAsync(source, cancellationToken)).Sum();
		}

		public static async ValueTask<long?> SumAsync(IAsyncEnumerable<long?> source, CancellationToken cancellationToken)
		{
			return EnumerableHelpers.Sum(await ToListAsync(source, cancellationToken));
		}

		private static async Task<List<TSource>> ToListAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			var list = new List<TSource>();
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				list.Add(item);
			}

			return list;
		}
	}
}
