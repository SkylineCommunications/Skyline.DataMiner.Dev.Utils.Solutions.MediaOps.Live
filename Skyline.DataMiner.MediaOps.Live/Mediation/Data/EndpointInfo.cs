namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	public class EndpointInfo
	{
		public Guid ID { get; set; }

		public string Name { get; set; }

		public string Element { get; set; }

		public string Identifier { get; set; }

		public static EndpointInfo Create(Endpoint instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			return new EndpointInfo
			{
				ID = instance.ID,
				Name = instance.Name,
				Element = instance.Element,
				Identifier = instance.Identifier,
			};
		}
	}
}
