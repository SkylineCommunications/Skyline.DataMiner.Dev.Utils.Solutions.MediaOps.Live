namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;

	public abstract class TakeRequest
	{
		public TimeSpan? Timeout { get; set; }
	}
}
