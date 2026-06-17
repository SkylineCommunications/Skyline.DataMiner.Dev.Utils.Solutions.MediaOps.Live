namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ControlSurfaceSettingsRepository : Repository<ControlSurfaceSettings>
	{
		internal ControlSurfaceSettingsRepository(MediaOpsLiveApi api) : base(api, api.SlcConnectivityManagementHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => ControlSurfaceSettings.DomDefinition;

		/// <summary>
		/// Gets the singleton Control Surface settings, creating them with default values if they do not exist yet.
		/// </summary>
		/// <returns>The Control Surface settings.</returns>
		public ControlSurfaceSettings GetOrCreate()
		{
			var existing = ReadAll().FirstOrDefault();
			if (existing != null)
			{
				return existing;
			}

			return CreateOrUpdate(new ControlSurfaceSettings());
		}

		protected internal override ControlSurfaceSettings CreateInstance(DomInstance domInstance)
		{
			return new ControlSurfaceSettings(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<ControlSurfaceSettings> instances)
		{
			// no checks needed
		}

		protected override void ValidateBeforeDelete(ICollection<ControlSurfaceSettings> instances)
		{
			// no checks needed
		}
	}
}
