namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	public class OrchestrationProfileParameter : IOrchestrationParameters
	{
		private readonly string _profileParameterName;
		private readonly string _orchestrationOverrideName;

		private Parameter _parameterReference;
		private bool _isLoaded;

		public OrchestrationProfileParameter(string profileParameterName, string orchestrationOverrideName = null)
		{
			_profileParameterName = profileParameterName;
			_orchestrationOverrideName = orchestrationOverrideName;
			_isLoaded = false;
		}

		public string Name =>
			String.IsNullOrEmpty(_orchestrationOverrideName)
				? _profileParameterName
				: _orchestrationOverrideName;

		public string ProfileParameterName => _profileParameterName;

		public Parameter ParameterReference
		{
			get => _parameterReference;
			private set => _parameterReference = value;
		}

		public IDictionary<string, Guid> GetParameterInformation(IEngine engine)
		{
			if (!_isLoaded)
			{
				LoadInformation(engine);
			}

			return new Dictionary<string, Guid> { { Name, ParameterReference.ID } };
		}

		public IDictionary<string, Parameter> GetParameterReferences(IEngine engine)
		{
			if (!_isLoaded)
			{
				LoadInformation(engine);
			}

			return new Dictionary<string, Parameter> { { Name, ParameterReference } };
		}

		private void LoadInformation(IEngine engine)
		{
			ProfileHelper helper = new ProfileHelper(engine.SendSLNetMessages);

			List<Parameter> parameters = helper.ProfileParameters.Read(ParameterExposers.Name.Equal(_profileParameterName));

			if (parameters.Count == 0)
			{
				throw new InvalidOperationException($"No profile parameter found with name '{_profileParameterName}'");
			}

			if (parameters.Count > 1)
			{
				throw new InvalidOperationException($"Multiple profile parameters found with name '{_profileParameterName}'");
			}

			ParameterReference = parameters.First();

			_isLoaded = true;
		}
	}
}
