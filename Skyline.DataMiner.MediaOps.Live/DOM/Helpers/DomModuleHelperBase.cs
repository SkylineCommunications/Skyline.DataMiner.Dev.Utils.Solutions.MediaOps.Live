namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;

	internal abstract class DomModuleHelperBase
	{
		protected DomModuleHelperBase(string moduleId, IConnection connection)
		{
			ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			MessageHandler = connection.HandleMessages;

			DomHelper = new DomHelper(MessageHandler, moduleId);
		}

		public string ModuleId { get; }

		protected Func<DMSMessage[], DMSMessage[]> MessageHandler { get; }

		public IConnection Connection { get; }

		public DomHelper DomHelper { get; }

		public static implicit operator DomHelper(DomModuleHelperBase helper)
		{
			return helper.DomHelper;
		}
	}
}
