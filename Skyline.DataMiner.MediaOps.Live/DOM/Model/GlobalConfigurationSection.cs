namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;

	public partial class GlobalConfigurationSection
	{
		public List<OrchestrationScriptArgument> OrchestrationScriptArgumentsList
		{
			get;
			private set;
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
		}

		protected override void BeforeToSection()
		{
			OrchestrationScriptArguments = OrchestrationScriptArgumentsList != null && OrchestrationScriptArgumentsList.Any()
				? JsonConvert.SerializeObject(OrchestrationScriptArgumentsList)
				: null;
		}
	}
}
