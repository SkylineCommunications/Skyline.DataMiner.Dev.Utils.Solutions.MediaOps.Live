namespace Skyline.DataMiner.MediaOps.Live.Automation.Take
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Take;

	public class TakeHelper : TakeHelperBase
	{
		public TakeHelper(IEngine engine) : base(engine.GetMediaOpsLiveApi())
		{
		}
	}
}
