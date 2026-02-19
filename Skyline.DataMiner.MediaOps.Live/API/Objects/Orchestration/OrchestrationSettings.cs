namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;

	public class OrchestrationSettings
	{
		public OrchestrationSettings()
		{
			Timeout = TimeSpan.FromMinutes(1);
		}

		public TimeSpan Timeout { get; set; }
	}
}
