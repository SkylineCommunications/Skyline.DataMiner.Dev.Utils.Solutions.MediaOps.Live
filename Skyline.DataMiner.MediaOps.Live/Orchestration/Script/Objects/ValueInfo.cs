namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	public class ValueInfo
	{
		public Dictionary<Guid, object> ProfileParameterValues { get; } = new Dictionary<Guid, object>();
	}
}