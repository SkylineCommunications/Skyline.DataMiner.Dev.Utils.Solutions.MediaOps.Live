namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	internal partial class GlobalConfigurationSection
	{
		public List<OrchestrationScriptArgument> OrchestrationScriptArgumentsList
		{
			get;
			private set;
		}

		public OrchestrationProfile Profile
		{
			get;
			set;
		}

		protected override void AfterLoad()
		{
			if (String.IsNullOrEmpty(OrchestrationScriptArguments))
			{
				OrchestrationScriptArgumentsList = new List<OrchestrationScriptArgument>();
			}
			else
			{
				OrchestrationScriptArgumentsList = JsonConvert.DeserializeObject<List<OrchestrationScriptArgument>>(OrchestrationScriptArguments);
			}

			if (String.IsNullOrEmpty(OrchestrationProfile))
			{
				Profile = new OrchestrationProfile();
			}
			else
			{
				Profile = JsonConvert.DeserializeObject<OrchestrationProfile>(OrchestrationProfile);
			}
		}

		protected override void BeforeToSection()
		{
			OrchestrationScriptArguments = OrchestrationScriptArgumentsList != null && OrchestrationScriptArgumentsList.Any()
				? JsonConvert.SerializeObject(OrchestrationScriptArgumentsList)
				: null;

			OrchestrationProfile = Profile != null
				? JsonConvert.SerializeObject(Profile)
				: null;
		}
	}
}
