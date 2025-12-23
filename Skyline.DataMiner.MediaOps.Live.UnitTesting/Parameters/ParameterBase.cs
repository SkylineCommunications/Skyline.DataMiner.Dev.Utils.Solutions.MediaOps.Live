namespace Skyline.DataMiner.MediaOps.Live.UnitTesting.Parameters
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.UnitTesting.Simulation;

	public abstract class ParameterBase
	{
		public ParameterBase(SimulatedElement element, int id)
		{
			Element = element ?? throw new ArgumentNullException(nameof(element));
			Id = id;
		}

		public SimulatedElement Element { get; }

		public int Id { get; }
	}
}
