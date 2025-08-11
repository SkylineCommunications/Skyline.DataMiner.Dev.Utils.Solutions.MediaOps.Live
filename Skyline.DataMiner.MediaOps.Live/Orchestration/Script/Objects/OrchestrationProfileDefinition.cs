namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Net.Profiles.Parameter;

	internal class OrchestrationProfileDefinition : IOrchestrationParameters
	{

		private readonly string _profileDefinitionName;
		private readonly Dictionary<string, string> _orchestrationOverrideNames;

		private Guid _profileDefinitionId;
		private Dictionary<string, Guid> _parameterInformation;
		private bool _isLoaded;

		public OrchestrationProfileDefinition(string profileDefinitionName)
		{
			_profileDefinitionName = profileDefinitionName;
			_parameterInformation = new Dictionary<string, Guid>();
			_isLoaded = false;
		}

		public OrchestrationProfileDefinition(string profileDefinitionName, Dictionary<string, string> orchestrationOverrideParameterNames) : this(profileDefinitionName)
		{
			_orchestrationOverrideNames = orchestrationOverrideParameterNames;
		}

		public string Name => _profileDefinitionName;

		public IDictionary<string, Guid> GetParameterInformation(IEngine engine)
		{
			if (!_isLoaded)
			{
				LoadInformation(engine);
				_isLoaded = true;
			}

			return _parameterInformation;
		}

		public Guid GetDefinition(IEngine engine)
		{
			if (!_isLoaded)
			{
				LoadInformation(engine);
				_isLoaded = true;
			}

			return _profileDefinitionId;
		}

		public void LoadInformation(IEngine engine)
		{
			ProfileHelper helper = new ProfileHelper(engine.SendSLNetMessages);

			List<ProfileDefinition> profileDefinitions = helper.ProfileDefinitions.Read(ProfileDefinitionExposers.Name.Equal(Name));

			if (profileDefinitions.Count == 0)
			{
				throw new InvalidOperationException($"No profile definition found with name {Name}");
			}

			ProfileDefinition definition = profileDefinitions.First();
			_profileDefinitionId = definition.ID;

			foreach (Parameter parameter in definition.Parameters)
			{
				if (_parameterInformation.ContainsKey(parameter.Name))
				{
					throw new InvalidOperationException($"Duplicate parameter name found in profile definition '{Name}': {parameter.Name}");
				}

				_parameterInformation.Add(
					_orchestrationOverrideNames.TryGetValue(parameter.Name, out string overriddenName)
						? overriddenName
						: parameter.Name, parameter.ID);
			}
		}
	}
}
