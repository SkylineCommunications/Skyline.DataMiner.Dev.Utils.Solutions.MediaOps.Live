namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System.Collections.Generic;

	public class OrchestrationProfile
	{
		public string Definition { get; set; }

		public string Instance { get; set; }

		public IList<OrchestrationProfileValue> Values { get; set; }
	}
}
