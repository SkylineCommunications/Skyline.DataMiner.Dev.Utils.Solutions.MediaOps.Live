namespace Skyline.DataMiner.MediaOps.Live.Automation.Tools
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Utils.MediaOps.Common.IOData.Scheduling.Scripts.JobHandler;

	internal class EngineMediaOpsPlanHelper : MediaOpsPlanHelper
	{
		private readonly IEngine _engine;

		internal EngineMediaOpsPlanHelper(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		internal override void UpdateJobState(SetJobOrchestrationStateAction action)
		{
			try
			{
				action.SendToJobHandler(_engine);
			}
			catch (Exception)
			{
				// No logic needed. Just needs to catch errors in case the events are not related to a PLAN job, which we do not know.
			}
		}
	}
}
