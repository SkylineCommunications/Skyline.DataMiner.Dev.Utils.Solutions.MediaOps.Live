namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	public class VsgDisconnectResult
	{
		public VsgDisconnectResult(VsgDisconnectRequest request)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgDisconnectRequest Request { get; }
	}
}
