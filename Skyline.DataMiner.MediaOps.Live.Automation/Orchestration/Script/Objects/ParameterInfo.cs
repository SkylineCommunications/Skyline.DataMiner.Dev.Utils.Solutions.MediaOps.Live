namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects
{
	using System;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;

	public class ParameterInfo
	{
		public string Description { get; set; }

		public IParameterDisplayInfo DisplayInfo { get; set; }

		public string Id { get; set; }

		public IDMAObjectRef Reference { get; set; }

		public string Type { get; set; }

		public object Value { get; set; }

		public Type ValueType { get; set; }

		public ParameterGroup Group { get; set; }

		public override string ToString()
		{
			return $"{Id}: {Type} with reference {Reference} ({Description}) / Value: {Value} ({ValueType})";
		}
	}
}