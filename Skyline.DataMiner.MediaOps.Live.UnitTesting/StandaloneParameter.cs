namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using Skyline.DataMiner.Net.Messages;

	public class StandaloneParameter
	{
		public StandaloneParameter(SimulatedElement element, int id)
		{
			Element = element;
			Id = id;
		}

		public SimulatedElement Element { get; }

		public int Id { get; }

		public object Value { get; private set; }

		internal ParameterValue ToParameterValue()
		{
			return ParameterValue.Compose(Value);
		}
	}
}
