namespace CheckMediaOpsLive
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.BpaLib;

	public class Result
	{
		public string Version { get; set; }

		public ICollection<Metric> Metrics { get; set; }

		public ICollection<Error> Errors { get; set; }

		[JsonIgnore]
		public BpaTestOutcome Outcome
		{
			get
			{
				if (Errors != null)
				{
					if (Errors.Any(x => x.Severity == Error.ErrorSeverity.Error))
					{
						return BpaTestOutcome.IssuesDetected;
					}

					if (Errors.Any(x => x.Severity == Error.ErrorSeverity.Warning))
					{
						return BpaTestOutcome.Warning;
					}
				}

				return BpaTestOutcome.NoIssues;
			}
		}
	}
}
