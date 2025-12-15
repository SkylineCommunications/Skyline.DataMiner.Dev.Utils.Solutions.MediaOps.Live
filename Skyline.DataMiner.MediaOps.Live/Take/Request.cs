namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	public abstract class Request
	{
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
	}
}
