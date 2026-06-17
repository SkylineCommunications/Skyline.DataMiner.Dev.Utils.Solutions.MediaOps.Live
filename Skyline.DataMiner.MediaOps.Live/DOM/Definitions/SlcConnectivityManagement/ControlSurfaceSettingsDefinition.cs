namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	internal class ControlSurfaceSettingsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Control Surface Settings")
		{
			ID = SlcConnectivityManagementIds.Definitions.ControlSurfaceSettings,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.ControlSurfaceSettings.Id),
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetControlSurfaceSettingsSectionDefinition(),
		};

		private static CustomSectionDefinition GetControlSurfaceSettingsSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.ControlSurfaceSettings.Id,
				Name = "Control Surface Settings",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(bool),
					ID = SlcConnectivityManagementIds.Sections.ControlSurfaceSettings.JobDetailsLinkEnabled,
					Name = "Job Details Link Enabled",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.ControlSurfaceSettings.JobDetailsUrlTemplate,
					Name = "Job Details Url Template",
					IsOptional = true,
				});

			return sectionDefinition;
		}
	}
}
