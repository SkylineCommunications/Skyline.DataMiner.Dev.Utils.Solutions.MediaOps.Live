namespace CheckMediaOpsLive
{
	public class Metric
	{
		public Metric(string name, double value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; }

		public double Value { get; }
	}
}
