namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;

	public abstract class DomModuleHelperBase
	{
		protected DomModuleHelperBase(string moduleId, Func<DMSMessage[], DMSMessage[]> messageHandler)
		{
			ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
			MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));

			DomHelper = new DomHelper(messageHandler, moduleId);
		}

		protected DomModuleHelperBase(string moduleId, IEngine engine) : this(moduleId, engine.SendSLNetMessages)
		{
		}

		public string ModuleId { get; }

		public DomHelper DomHelper { get; }

		protected Func<DMSMessage[], DMSMessage[]> MessageHandler { get; }

		public static implicit operator DomHelper(DomModuleHelperBase helper)
		{
			return helper.DomHelper;
		}
	}
}
