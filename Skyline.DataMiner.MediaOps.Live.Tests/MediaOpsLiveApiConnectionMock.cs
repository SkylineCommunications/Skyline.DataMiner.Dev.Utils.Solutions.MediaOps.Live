namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	internal class MediaOpsLiveApiConnectionMock : ICommunication
	{
		private DomSLNetMessageHandler domHandler;

		public MediaOpsLiveApiConnectionMock(DomSLNetMessageHandler handler)
		{
			domHandler = handler;
		}

		public void AddSubscriptionHandler(NewMessageEventHandler handler)
		{
			throw new NotImplementedException();
		}

		public void AddSubscriptions(NewMessageEventHandler handler, string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions)
		{
			throw new NotImplementedException();
		}

		public void AddSubscriptions(NewMessageEventHandler handler, string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, TimeSpan subscribeTimeout)
		{
			throw new NotImplementedException();
		}

		public void ClearSubscriptionHandler(NewMessageEventHandler handler)
		{
			throw new NotImplementedException();
		}

		public void ClearSubscriptions(string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, bool force = false)
		{
			throw new NotImplementedException();
		}

		public void ClearSubscriptions(string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, TimeSpan subscribeTimeout, bool force = false)
		{
			throw new NotImplementedException();
		}

		public DMSMessage[] SendMessage(DMSMessage message)
		{
			throw new NotImplementedException();
		}

		public DMSMessage[] SendMessages(DMSMessage[] messages)
		{
			return domHandler.HandleMessages(messages);
		}

		public DMSMessage SendSingleRawResponseMessage(DMSMessage message)
		{
			return domHandler.HandleMessage(message);
		}

		public DMSMessage SendSingleResponseMessage(DMSMessage message)
		{
			return domHandler.HandleMessage(message);
		}
	}
}
