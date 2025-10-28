namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	/// <summary>
	/// Defines the actions that can be performed by a connection handler script.
	/// </summary>
	public enum ConnectionHandlerScriptAction
	{
		/// <summary>
		/// Get the list of supported elements.
		/// </summary>
		GetSupportedElements,

		/// <summary>
		/// Get subscription information.
		/// </summary>
		GetSubscriptionInfo,

		/// <summary>
		/// Handle a parameter update.
		/// </summary>
		HandleParameterUpdate,

		/// <summary>
		/// Create a connection.
		/// </summary>
		Connect,

		/// <summary>
		/// Disconnect a connection.
		/// </summary>
		Disconnect,
	}
}
