namespace Skyline.DataMiner.MediaOps.Live.DOM.Tools
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public static class DomModuleInstaller
	{
		public static void Install(IEngine engine, IDomModuleInfo domModuleInfo, Action<string> logAction)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			if (domModuleInfo == null)
			{
				throw new ArgumentNullException(nameof(domModuleInfo));
			}

			var moduleSettingsHelper = new ModuleSettingsHelper(engine.SendSLNetMessages);
			var domHelper = new DomHelper(engine.SendSLNetMessages, domModuleInfo.ModuleId);

			CreateOrUpdateModuleSettings(moduleSettingsHelper, domModuleInfo.ModuleSettings, logAction);

			foreach (var definition in domModuleInfo.Definitions)
			{
				CreateOrUpdateDomDefinition(domHelper, definition, logAction);
			}
		}

		private static void CreateOrUpdateModuleSettings(ModuleSettingsHelper helper, ModuleSettings settings, Action<string> logAction)
		{
			CreateOrUpdate(
				helper.ModuleSettings,
				ModuleSettingsExposers.ModuleId.Equal(settings.ModuleId),
				settings,
				logAction);
		}

		private static void CreateOrUpdateDomDefinition(DomHelper helper, IDomDefinitionInfo domDefinitionInfo, Action<string> logAction)
		{
			foreach (var sectionDefinition in domDefinitionInfo.SectionDefinitions)
			{
				CreateOrUpdateSectionDefinition(helper, sectionDefinition, logAction);
			}

			CreateOrUpdateDomDefinition(helper, domDefinitionInfo.Definition, logAction);
		}

		private static void CreateOrUpdateDomDefinition(DomHelper helper, DomDefinition definition, Action<string> logAction)
		{
			CreateOrUpdate(
				helper.DomDefinitions,
				DomDefinitionExposers.Id.Equal(definition.ID),
				definition,
				logAction);
		}

		private static void CreateOrUpdateSectionDefinition(DomHelper helper, SectionDefinition definition, Action<string> logAction)
		{
			CreateOrUpdate(
				helper.SectionDefinitions,
				SectionDefinitionExposers.ID.Equal(definition.GetID()),
				definition,
				logAction);
		}

		private static void CreateOrUpdate<T>(ICrudHelperComponent<T> crudHelperComponent, FilterElement<T> filter, T obj, Action<string> logAction)
			where T : DataType
		{
			Log(logAction, "Searching for", obj);
			T existingObj = crudHelperComponent.Read(filter).SingleOrDefault();

			if (existingObj == null)
			{
				Log(logAction, "Creating", obj);
				crudHelperComponent.Create(obj);
				return;
			}

			if (existingObj.Equals(obj))
			{
				Log(logAction, "Skipping", obj);
			}
			else
			{
				Log(logAction, "Updating", obj);
				crudHelperComponent.Update(obj);
			}
		}

		private static void Log(Action<string> logAction, string action, DataType dataType)
		{
			logAction?.Invoke($"{action} {dataType.GetType().Name}: {GetName(dataType)} [{dataType.DataTypeID}].");
		}

		private static string GetName(DataType dataType)
		{
			switch (dataType)
			{
				case DomBehaviorDefinition domBehaviorDefinition:
					return domBehaviorDefinition.Name;

				case DomDefinition domDefinition:
					return domDefinition.Name;

				case DomInstance domInstance:
					return domInstance.Name;

				case DomTemplate domTemplate:
					return domTemplate.Name;

				case ModuleSettings moduleSettings:
					return moduleSettings.ModuleId;

				case CustomSectionDefinition customSectionDefinition:
					return customSectionDefinition.Name;

				default:
					return String.Empty;
			}
		}
	}
}
