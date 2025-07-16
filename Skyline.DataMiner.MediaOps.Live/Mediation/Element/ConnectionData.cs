namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	public class ConnectionData
	{
		public ConnectionData()
		{
		}

		public ConnectionData(Guid destinationId)
		{
			Destination = destinationId;
		}

		public Guid Destination { get; set; }

		public bool IsConnected { get; set; }

		public Guid? ConnectedSource { get; set; }

		public Guid? PendingSource { get; set; }

		public PendingConnectionActionType? PendingAction { get; set; }

		public static ICollection<ConnectionData> FromJson(string json)
		{
			if (String.IsNullOrWhiteSpace(json))
			{
				return [];
			}

			return JsonConvert.DeserializeObject<ICollection<ConnectionData>>(json);
		}

		public static string ToJson(ICollection<ConnectionData> connections)
		{
			if (connections == null || connections.Count == 0)
			{
				return "[]";
			}

			var jsonSerializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
			};

			return JsonConvert.SerializeObject(connections, jsonSerializerSettings);
		}
	}
}
