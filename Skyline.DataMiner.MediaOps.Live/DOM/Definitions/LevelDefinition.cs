namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;

	internal class LevelDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Level")
		{
			ID = SlcConnectivityManagementIds.Definitions.Level,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.LevelInfo.Id),
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcConnectivityManagementIds.Sections.LevelInfo.Name),
					},
				},
			},
		};

		public IEnumerable<SectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetLevelInfoSectionDefinition(),
		};

		private static SectionDefinition GetLevelInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.LevelInfo.Id,
				Name = "Level Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(long),
					ID = SlcConnectivityManagementIds.Sections.LevelInfo.Number,
					Name = "Number",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.LevelInfo.Name,
					Name = "Name",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.LevelInfo.TransportType,
					Name = "Transport Type",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.TransportType },
				});

			return sectionDefinition;
		}
	}
}
