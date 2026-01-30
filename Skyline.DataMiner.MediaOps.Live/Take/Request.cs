namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;

	public abstract class Request
	{
		public TimeSpan? Timeout { get; set; }
	}
}
