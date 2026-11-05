namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Enums;

	internal class OrchestrationScriptInternalInput
	{
		internal const string OrchestrationLevelMetadataKey = "{Orchestration Level}";
		internal const string EventIdMetadataKey = "{Event ID}";
		internal const string NodeIdMetadataKey = "{Node ID}";
		internal const string ResumeMetadataKey = "{Resume}";

		public OrchestrationScriptInternalInput(Guid eventId, OrchestrationLevel level = OrchestrationLevel.Unknown, string nodeId = null, bool resume = false)
		{
			EventId = eventId;
			Level = level;
			NodeId = nodeId;
			Resume = resume;
		}

		public Guid EventId { get; set; }

		public OrchestrationLevel Level { get; set; }

		public string NodeId { get; set; }

		public bool Resume { get; set; }

		public IEnumerable<OrchestrationScriptArgument> ToMetadataArguments()
		{
			List<OrchestrationScriptArgument> arguments = new List<OrchestrationScriptArgument>();

			if (Level != OrchestrationLevel.Unknown)
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, OrchestrationLevelMetadataKey, Level.ToString()));
			}

			if (EventId != Guid.Empty)
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, EventIdMetadataKey, EventId.ToString()));
			}

			if (!String.IsNullOrEmpty(NodeId))
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, NodeIdMetadataKey, NodeId));
			}

			if (Resume)
			{
				arguments.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, ResumeMetadataKey, Boolean.TrueString));
			}

			return arguments;
		}
	}
}
