namespace CheckMediaOpsLive
{
	using System;

	using Skyline.DataMiner.BpaLib;
	using Skyline.DataMiner.Net.BPA.Config;

	public class BpaTestEntry : ABpaTest
	{
		public override string Name => nameof(CheckMediaOpsLive);

		public override string Description => "Checks the configuration and collect statistics of MediaOps.LIVE.";

		public override BpaScheduleConfig DefaultSchedule => BpaScheduleConfig.FromDays(1);

		public override BpaFlags Flags => BpaFlags.RequireSLNet;

		public override ABpaTestResult Run(BpaExecuteContext context)
		{
			using (new CustomAssemblyResolver())
			{
				try
				{
					return MediaOpsLiveBpa.Execute(context);
				}
				catch (Exception e)
				{
					return new BpaTestResult(e);
				}
			}
		}
	}
}
