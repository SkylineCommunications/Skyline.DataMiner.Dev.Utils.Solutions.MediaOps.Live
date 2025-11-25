namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	// Keep in sync with SlcConnectivityManagementIds.Enums.LockState
	public enum LockState
	{
		Unlocked = SlcConnectivityManagementIds.Enums.LockState.Unlocked,
		Locked = SlcConnectivityManagementIds.Enums.LockState.Locked,
		Protected = SlcConnectivityManagementIds.Enums.LockState.Protected,
	}
}
