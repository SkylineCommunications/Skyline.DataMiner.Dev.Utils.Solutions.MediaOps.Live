namespace Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Parameters
{
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Simulation;

	public class StandaloneParameter : ParameterBase
	{
		public StandaloneParameter(SimulatedElement element, int id) : base(element, id)
		{
		}

		public object Value { get; private set; }

		public void SetValue(object value)
		{
			Value = value;
		}

		internal ParameterValue ToParameterValue()
		{
			return ParameterValue.Compose(Value);
		}
	}
}
