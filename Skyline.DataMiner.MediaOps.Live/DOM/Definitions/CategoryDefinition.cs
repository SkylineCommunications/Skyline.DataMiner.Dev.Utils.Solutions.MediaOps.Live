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

	internal class CategoryDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Category")
		{
			ID = SlcConnectivityManagementIds.Definitions.Category,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.CategoryInfo.Id),
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcConnectivityManagementIds.Sections.CategoryInfo.Name),
					},
				},
			},
		};

		public IEnumerable<SectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetCategoryInfoSectionDefinition(),
		};

		private static SectionDefinition GetCategoryInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.CategoryInfo.Id,
				Name = "Category Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.CategoryInfo.Name,
					Name = "Name",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.CategoryInfo.ParentCategory,
					Name = "Parent Category",
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.Category },
				});

			return sectionDefinition;
		}
	}
}
