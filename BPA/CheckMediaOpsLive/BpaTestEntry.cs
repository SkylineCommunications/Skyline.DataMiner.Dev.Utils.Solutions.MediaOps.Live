namespace CheckMediaOpsLive
{
	using System;

	using CheckMediaOpsLive.Tools;

	using Skyline.DataMiner.BpaLib;
	using Skyline.DataMiner.Net.BPA.Config;

	public class BpaTestEntry : ABpaTest
	{
		public override string Name => "MediaOps.LIVE Configuration";

		public override string Description => "Checks the MediaOps.LIVE configuration and collects performance statistics.";

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
