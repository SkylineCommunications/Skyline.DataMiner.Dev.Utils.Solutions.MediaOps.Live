namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;
	using System.Linq;

	internal static class UnitTestDetector
	{
		private static readonly string[] TestAssemblies =
		[
			"Microsoft.TestPlatform",
			"Microsoft.VisualStudio.TestPlatform",
			"Microsoft.VisualStudio.TestTools.UnitTesting",
			"MSTest.TestFramework",
			"NUnit.Framework",
			"xunit.core",
		];

		static UnitTestDetector()
		{
			IsInUnitTest = AppDomain.CurrentDomain
				.GetAssemblies()
				.Select(a => a.GetName()?.Name)
				.Where(name => name != null)
				.Any(name => TestAssemblies.Any(t =>
					name.StartsWith(t, StringComparison.OrdinalIgnoreCase)));
		}

		public static bool IsInUnitTest { get; private set; }
	}
}