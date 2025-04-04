namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using Skyline.DataMiner.MediaOps.Live.API.Enums;

	public class OrchestrationScriptArgument
	{
		public OrchestrationScriptArgument(OrchestrationScriptArgumentType type, string name, string value)
		{
			Type = type;
			Name = name;
			Value = value;
		}

		public OrchestrationScriptArgumentType Type { get; set; }

		public string Name { get; set; }

		public string Value { get; set; }
	}
}
