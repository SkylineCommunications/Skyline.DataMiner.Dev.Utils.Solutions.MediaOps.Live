namespace CheckMediaOpsLive
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.BpaLib;

	public class Result
	{
		public string Version { get; set; }

		public MediaOpsLiveStatistics Statistics { get; set; }

		public ICollection<Error> Errors { get; set; }

		[JsonIgnore]
		public BpaTestOutcome Outcome
		{
			get
			{
				if (Errors != null)
				{
					if (Errors.Any(x => x.Severity == ErrorSeverity.Error))
					{
						return BpaTestOutcome.IssuesDetected;
					}

					if (Errors.Any(x => x.Severity == ErrorSeverity.Warning))
					{
						return BpaTestOutcome.Warning;
					}
				}

				return BpaTestOutcome.NoIssues;
			}
		}

		[JsonIgnore]
		public string Message
		{
			get
			{
				if (Errors != null)
				{
					if (Errors.Any(x => x.Severity == ErrorSeverity.Error))
					{
						return "Errors detected in the system. Please contact your system administrator for support.";
					}

					if (Errors.Any(x => x.Severity == ErrorSeverity.Warning))
					{
						return "Warnings detected in the system. Please contact your system administrator for support.";
					}
				}

				return "No incorrect configurations detected in the system.";
			}
		}
	}
}
