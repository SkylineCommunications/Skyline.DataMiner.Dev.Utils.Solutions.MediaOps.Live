namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	public class OrchestrationProfileDefinition : IOrchestrationParameters
	{
		private readonly string _profileDefinitionName;
		private readonly Dictionary<string, string> _orchestrationOverrideNames;
		private readonly Dictionary<string, Parameter> _parameterReferences;

		private ProfileDefinition _profileDefinition;
		private bool _isLoaded;

		public OrchestrationProfileDefinition(string profileDefinitionName) : this(profileDefinitionName, new Dictionary<string, string>())
		{
		}

		public OrchestrationProfileDefinition(string profileDefinitionName, Dictionary<string, string> orchestrationOverrideParameterNames)
		{
			_orchestrationOverrideNames = orchestrationOverrideParameterNames;
			_profileDefinitionName = profileDefinitionName;
			_parameterReferences = new Dictionary<string, Parameter>();
			_isLoaded = false;
		}

		public string Name => _profileDefinitionName;

		public IDictionary<string, Guid> GetParameterInformation(IEngine engine)
		{
			LoadInformation(engine);

			return _parameterReferences.ToDictionary(kv => kv.Key, kv => kv.Value.ID);
		}

		public IDictionary<string, Parameter> GetParameterReferences(IEngine engine)
		{
			LoadInformation(engine);

			return _parameterReferences;
		}

		public ProfileDefinition GetDefinitionReference(IEngine engine)
		{
			LoadInformation(engine);

			return _profileDefinition;
		}

		public void LoadInformation(IEngine engine)
		{
			if (_isLoaded)
			{
				return;
			}

			ProfileHelper helper = new ProfileHelper(engine.SendSLNetMessages);

			List<ProfileDefinition> profileDefinitions = helper.ProfileDefinitions.Read(ProfileDefinitionExposers.Name.Equal(Name));

			if (profileDefinitions.Count == 0)
			{
				throw new InvalidOperationException($"No profile definition found with name {Name}");
			}

			if (profileDefinitions.Count > 1)
			{
				throw new InvalidOperationException($"Multiple profile definitions found with name {Name}");
			}

			_profileDefinition = profileDefinitions.First();

			foreach (Parameter parameter in _profileDefinition.Parameters)
			{
				if (_parameterReferences.ContainsKey(parameter.Name))
				{
					throw new InvalidOperationException($"Duplicate parameter name found in profile definition '{Name}': {parameter.Name}");
				}

				string nameKey = _orchestrationOverrideNames.TryGetValue(parameter.Name, out string overriddenNameInfo)
					? overriddenNameInfo
					: parameter.Name;

				_parameterReferences.Add(nameKey, parameter);
			}

			_isLoaded = true;
		}
	}
}
