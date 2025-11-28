namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	public class VsgConnectionResult
	{
		public VsgConnectionResult(VsgConnectionRequest request)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgConnectionRequest Request { get; }
	}
}
