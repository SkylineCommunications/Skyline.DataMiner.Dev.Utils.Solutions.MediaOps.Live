namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	using ElementState = Skyline.DataMiner.Net.Messages.ElementState;

	public readonly struct ElementStateChange(IDmsElement element, ElementState state)
	{
		public IDmsElement Element { get; } = element ?? throw new ArgumentNullException(nameof(element));

		public ElementState State { get; } = state;
	}
}
