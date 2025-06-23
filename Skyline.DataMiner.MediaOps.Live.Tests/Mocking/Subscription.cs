namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;

	internal class Subscription
	{
		public Subscription(string setId)
		{
			SetId = setId;
		}

		public string SetId { get; }

		public ConcurrentHashSet<SubscriptionFilter> Filters { get; } = new ConcurrentHashSet<SubscriptionFilter>();
	}
}
