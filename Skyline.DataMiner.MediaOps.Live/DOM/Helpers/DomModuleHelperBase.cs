namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;

	public abstract class DomModuleHelperBase
	{
		protected DomModuleHelperBase(string moduleId, DomHelper domHelper)
		{
			ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
			DomHelper = domHelper ?? throw new ArgumentNullException(nameof(domHelper));

			if (DomHelper.ModuleId != ModuleId)
			{
				throw new ArgumentException("Module ID doesn't match");
			}
		}

		protected DomModuleHelperBase(string moduleId, IEngine engine) : this(moduleId, engine.SendSLNetMessages)
		{
		}

		protected DomModuleHelperBase(string moduleId, Func<DMSMessage[], DMSMessage[]> messageHandler) : this(moduleId, new DomHelper(messageHandler, moduleId))
		{
		}

		public string ModuleId { get; }

		public DomHelper DomHelper { get; }

		public static implicit operator DomHelper(DomModuleHelperBase helper)
		{
			return helper.DomHelper;
		}
	}
}
