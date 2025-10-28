namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	/// <summary>
	/// Defines the actions that can be performed by connection handler scripts.
	/// </summary>
	public enum ConnectionHandlerScriptAction
	{
		/// <summary>
		/// Gets the list of supported elements.
		/// </summary>
		GetSupportedElements,

		/// <summary>
		/// Gets subscription information.
		/// </summary>
		GetSubscriptionInfo,

		/// <summary>
		/// Handles parameter updates.
		/// </summary>
		HandleParameterUpdate,

		/// <summary>
		/// Connects endpoints.
		/// </summary>
		Connect,

		/// <summary>
		/// Disconnects endpoints.
		/// </summary>
		Disconnect,
	}
}
