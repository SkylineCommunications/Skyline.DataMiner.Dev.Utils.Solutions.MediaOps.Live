namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	/// <summary>
	/// Defines the role of an endpoint.
	/// </summary>
	// Keep in sync with SlcConnectivityManagementIds.Enums.Role
	public enum EndpointRole
	{
		/// <summary>
		/// Source endpoint role.
		/// </summary>
		Source = SlcConnectivityManagementIds.Enums.Role.Source,

		/// <summary>
		/// Destination endpoint role.
		/// </summary>
		Destination = SlcConnectivityManagementIds.Enums.Role.Destination,
	}
}
