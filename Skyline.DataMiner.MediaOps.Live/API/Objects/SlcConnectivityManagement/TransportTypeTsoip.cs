namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class TransportTypeTsoip
	{
		public TransportTypeTsoip()
		{
			DomSection = new TransportTypeTsoipSection();
		}

		public TransportTypeTsoip(string multicastIP) : this()
		{
			MulticastIP = multicastIP;
		}

		public TransportTypeTsoip(string multicastIP, int port) : this(multicastIP)
		{
			Port = port;
		}

		public TransportTypeTsoip(string multicastIP, int port, string sourceIP) : this(multicastIP, port)
		{
			SourceIP = sourceIP;
		}

		public TransportTypeTsoip(string multicastIP, string sourceIP) : this(multicastIP)
		{
			SourceIP = sourceIP;
		}

		internal TransportTypeTsoip(TransportTypeTsoipSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal TransportTypeTsoipSection DomSection { get; }

		public string SourceIP
		{
			get
			{
				return DomSection.SourceIP;
			}

			set
			{
				DomSection.SourceIP = value;
			}
		}

		public string MulticastIP
		{
			get
			{
				return DomSection.MulticastIP;
			}

			set
			{
				DomSection.MulticastIP = value;
			}
		}

		public int? Port
		{
			get
			{
				return (int?)DomSection.Port;
			}

			set
			{
				DomSection.Port = value;
			}
		}
	}
}
