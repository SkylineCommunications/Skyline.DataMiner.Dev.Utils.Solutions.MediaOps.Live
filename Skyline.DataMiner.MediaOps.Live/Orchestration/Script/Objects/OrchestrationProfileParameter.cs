namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
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

		public OrchestrationProfileParameter(string profileParameterName)
		{
			_profileParameterName = profileParameterName;
		}

		public OrchestrationProfileParameter(string profileParameterName, string orchestrationOverrideName) : this(profileParameterName)
		{
			_orchestrationOverrideName = orchestrationOverrideName;
		}

		public string Name =>
			String.IsNullOrEmpty(_orchestrationOverrideName)
				? _profileParameterName
				: _orchestrationOverrideName;

		public IDictionary<string, Guid> GetParameterInformation(IEngine engine)
		{
			ProfileHelper helper = new ProfileHelper(engine.SendSLNetMessages);

			List<Parameter> parameters = helper.ProfileParameters.Read(ParameterExposers.Name.Equal(Name));

			if (parameters.Count == 0)
			{
				throw new InvalidOperationException($"No profile parameter found with name '{Name}'");
			}

			Parameter parameter = parameters.First();
			return new Dictionary<string, Guid> { { Name, parameter.ID } };
		}

		public Guid GetDefinition(IEngine engine)
		{
			throw new NotImplementedException();
		}
	}
}
