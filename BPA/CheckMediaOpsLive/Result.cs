namespace CheckMediaOpsLive
{
	using System.Collections.Generic;

	public class Result
	{
		public string Version { get; set; }

		public ICollection<Metric> Metrics { get; set; }
	}
}
