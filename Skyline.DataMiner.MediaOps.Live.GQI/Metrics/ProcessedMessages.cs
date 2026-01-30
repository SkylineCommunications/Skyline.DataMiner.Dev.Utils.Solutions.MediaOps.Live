namespace Skyline.DataMiner.Solutions.MediaOps.Live.GQI.Metrics
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	public class ProcessedMessages
	{
		public ProcessedMessages(ICollection<DMSMessage> requests, ICollection<DMSMessage> responses, TimeSpan duration)
		{
			Requests = requests ?? throw new ArgumentNullException(nameof(requests));
			Responses = responses ?? throw new ArgumentNullException(nameof(responses));
			Duration = duration;
		}

		public ICollection<DMSMessage> Requests { get; }

		public ICollection<DMSMessage> Responses { get; }

		public TimeSpan Duration { get; }
	}
}
