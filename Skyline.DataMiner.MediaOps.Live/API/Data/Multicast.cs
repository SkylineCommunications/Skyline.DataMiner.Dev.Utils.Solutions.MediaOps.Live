namespace Skyline.DataMiner.MediaOps.Live.API.Data
{
	using System;

	public class Multicast
	{
		public Multicast(string ip, int port, string source)
		{
			if (String.IsNullOrWhiteSpace(ip))
			{
				throw new ArgumentException($"'{nameof(ip)}' cannot be null or whitespace.", nameof(ip));
			}

			IpAddress = ip;
			Port = port;
			SourceIP = source;
		}

		public Multicast(string ip)
			: this(ip, 0, null)
		{
		}

		public Multicast(string ip, int port)
			: this(ip, port, null)
		{
		}

		public string IpAddress { get; set; }

		public int Port { get; set; }

		public string SourceIP { get; set; }

		public override string ToString()
		{
			if (SourceIP != null)
				return $"{IpAddress}:{Port}@{SourceIP}";
			else
				return $"{IpAddress}:{Port}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Multicast other))
				return false;

			return Equals(IpAddress, other.IpAddress)
				&& Port == other.Port
				&& Equals(SourceIP, other.SourceIP);
		}

		public override int GetHashCode()
		{
			return (IpAddress, Port, SourceIP).GetHashCode();
		}
	}
}
