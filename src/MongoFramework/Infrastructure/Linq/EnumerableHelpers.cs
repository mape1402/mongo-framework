using System.Collections.Generic;

namespace MongoFramework.Infrastructure.Linq
{
	internal static class EnumerableHelpers
	{
		public static int? Sum(IEnumerable<int?> source)
		{
			var hasValue = false;
			var sum = 0;
			foreach (var item in source)
			{
				if (item.HasValue)
				{
					hasValue = true;
					sum += item.Value;
				}
			}

			return hasValue ? sum : null;
		}

		public static decimal? Sum(IEnumerable<decimal?> source)
		{
			var hasValue = false;
			var sum = 0m;
			foreach (var item in source)
			{
				if (item.HasValue)
				{
					hasValue = true;
					sum += item.Value;
				}
			}

			return hasValue ? sum : null;
		}

		public static double? Sum(IEnumerable<double?> source)
		{
			var hasValue = false;
			var sum = 0d;
			foreach (var item in source)
			{
				if (item.HasValue)
				{
					hasValue = true;
					sum += item.Value;
				}
			}

			return hasValue ? sum : null;
		}

		public static float? Sum(IEnumerable<float?> source)
		{
			var hasValue = false;
			var sum = 0f;
			foreach (var item in source)
			{
				if (item.HasValue)
				{
					hasValue = true;
					sum += item.Value;
				}
			}

			return hasValue ? sum : null;
		}

		public static long? Sum(IEnumerable<long?> source)
		{
			var hasValue = false;
			var sum = 0L;
			foreach (var item in source)
			{
				if (item.HasValue)
				{
					hasValue = true;
					sum += item.Value;
				}
			}

			return hasValue ? sum : null;
		}
	}
}
