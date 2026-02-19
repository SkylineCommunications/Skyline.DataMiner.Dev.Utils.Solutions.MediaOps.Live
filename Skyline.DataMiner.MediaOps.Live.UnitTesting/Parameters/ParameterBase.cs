namespace Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Parameters
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Simulation;

	public abstract class ParameterBase
	{
		protected ParameterBase(SimulatedElement element, int id)
		{
			Element = element ?? throw new ArgumentNullException(nameof(element));
			Id = id;
		}

		public SimulatedElement Element { get; }

		public int Id { get; }
	}
}
