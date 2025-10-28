namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	using ElementState = Skyline.DataMiner.Net.Messages.ElementState;

	internal readonly struct ElementStateChangeEvent(DmsElementId elementId, ElementState state)
	{
		public DmsElementId ElementId { get; } = elementId;

		public ElementState State { get; } = state;
	}
}
