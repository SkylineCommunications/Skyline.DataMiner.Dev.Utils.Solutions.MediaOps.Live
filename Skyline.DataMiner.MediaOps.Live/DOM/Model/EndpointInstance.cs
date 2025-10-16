namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement
{
	internal partial class EndpointInstance
	{
		public bool IsSource => EndpointInfo.Role == SlcConnectivityManagementIds.Enums.Role.Source;

		public bool IsDestination => EndpointInfo.Role == SlcConnectivityManagementIds.Enums.Role.Destination;
	}
}
