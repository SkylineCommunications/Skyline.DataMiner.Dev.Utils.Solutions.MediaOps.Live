namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Enums;

	internal class OrchestrationScriptInternalInput
	{
		public OrchestrationScriptInternalInput(Guid eventId, OrchestrationLevel level = OrchestrationLevel.Unknown)
		{
			EventId = eventId;
			Level = level;
		}

		public Guid EventId { get; set; }

		public OrchestrationLevel Level { get; set; }

		public IEnumerable<OrchestrationScriptArgument> ToMetadataArguments()
		{
			List<OrchestrationScriptArgument> arguments = new List<OrchestrationScriptArgument>();

			if (Level != OrchestrationLevel.Unknown)
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, "{Orchestration Level}", Level.ToString()));
			}

			if (EventId != Guid.Empty)
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, "{Event ID}", EventId.ToString()));
			}

			return arguments;
		}
	}
}
