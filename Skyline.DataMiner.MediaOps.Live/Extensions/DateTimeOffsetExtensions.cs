namespace Skyline.DataMiner.Solutions.MediaOps.Live.Extensions
{
	using System;

	internal static class DateTimeOffsetExtensions
	{
		/// <summary>
		/// Drops everything below the second, which is the precision at which orchestration times are stored.
		/// </summary>
		public static DateTimeOffset TruncateToSecond(this DateTimeOffset time)
		{
			return time.AddTicks(-(time.Ticks % TimeSpan.TicksPerSecond));
		}

		/// <summary>
		/// Drops everything below the second, which is the precision at which orchestration times are stored.
		/// </summary>
		public static DateTime TruncateToSecond(this DateTime time)
		{
			return time.AddTicks(-(time.Ticks % TimeSpan.TicksPerSecond));
		}
	}
}
