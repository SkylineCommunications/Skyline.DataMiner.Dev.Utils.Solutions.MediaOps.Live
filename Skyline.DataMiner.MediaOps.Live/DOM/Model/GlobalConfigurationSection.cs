namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using API.Objects.SlcOrchestration;

	using Newtonsoft.Json;

	public partial class GlobalConfigurationSection
	{
		public List<OrchestrationScriptArgument> OrchestrationScriptArgumentsList
		{
			get;
			private set;
		}

		public override void AfterLoad()
		{
			if (String.IsNullOrEmpty(OrchestrationScriptArguments))
			{
				OrchestrationScriptArgumentsList = new List<OrchestrationScriptArgument>();
			}
			else
			{
				try
				{
					OrchestrationScriptArgumentsList = JsonConvert.DeserializeObject<List<OrchestrationScriptArgument>>(OrchestrationScriptArguments);
				}
				catch
				{
					OrchestrationScriptArgumentsList = new List<OrchestrationScriptArgument>();
				}
			}
		}

		public override void BeforeToSection()
		{
			OrchestrationScriptArguments = OrchestrationScriptArgumentsList != null && OrchestrationScriptArgumentsList.Any()
				? JsonConvert.SerializeObject(OrchestrationScriptArgumentsList)
				: null;
		}
	}
}
