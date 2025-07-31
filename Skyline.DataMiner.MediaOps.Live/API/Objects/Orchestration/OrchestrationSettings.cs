namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	public class OrchestrationSettings
	{
		public OrchestrationSettings()
		{
			Timeout = TimeSpan.FromMinutes(1);
		}

		public TimeSpan Timeout { get; set; }
	}
}
