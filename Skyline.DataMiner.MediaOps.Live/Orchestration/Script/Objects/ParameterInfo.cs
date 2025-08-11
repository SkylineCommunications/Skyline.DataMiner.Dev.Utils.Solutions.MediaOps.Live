namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.Net;

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