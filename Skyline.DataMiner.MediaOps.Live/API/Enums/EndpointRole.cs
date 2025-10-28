namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	/// <summary>
	/// Defines the role of an endpoint in connectivity management.
	/// </summary>
	// Keep in sync with SlcConnectivityManagementIds.Enums.Role
	public enum EndpointRole
	{
		/// <summary>
		/// The endpoint serves as a source.
		/// </summary>
		Source = SlcConnectivityManagementIds.Enums.Role.Source,

		/// <summary>
		/// The endpoint serves as a destination.
		/// </summary>
		Destination = SlcConnectivityManagementIds.Enums.Role.Destination,
	}
}
