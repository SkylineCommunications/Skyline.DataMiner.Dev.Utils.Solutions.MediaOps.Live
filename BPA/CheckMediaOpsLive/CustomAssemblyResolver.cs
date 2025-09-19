namespace CheckMediaOpsLive
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	public sealed class CustomAssemblyResolver : IDisposable
	{
		public CustomAssemblyResolver()
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnLocateMissingAssemblies;
		}

		public void Dispose()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= OnLocateMissingAssemblies;
		}

		private Assembly OnLocateMissingAssemblies(object sender, ResolveEventArgs args)
		{
			// fix assembly resolves when executed in the StandaloneBpaExecutor context, where we 
			// cannot rely on running in the SLNet process context => dependent DLLs such as
			// "ICSharpCode.SharpZipLib.dll" might not be present.

			var requested = new AssemblyName(args.Name);

			try
			{
				if (TryFindAssembly(requested, out var assemblyPath))
				{
					return Assembly.LoadFile(assemblyPath);
				}
			}
			catch
			{
				// ignore
			}

			return null;
		}

		private bool TryFindAssembly(AssemblyName requested, out string assemblyPath)
		{
			var directoriesToCheck = new[]
			{
				AppDomain.CurrentDomain.BaseDirectory,
				Assembly.GetExecutingAssembly().Location,
				@"C:\Skyline DataMiner\Files",
				@"C:\Skyline DataMiner\ProtocolScripts",
			}
			.Distinct();

			AssemblyName bestCandidate = null;
			string bestCandidatePath = null;

			foreach (var dir in directoriesToCheck)
			{
				if (!Directory.Exists(dir))
				{
					continue;
				}

				foreach (var dll in Directory.GetFiles(dir, $"{requested.Name}.dll", SearchOption.AllDirectories))
				{
					try
					{
						var candidate = AssemblyName.GetAssemblyName(dll);

						// Match name first
						if (!String.Equals(candidate.Name, requested.Name, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						// Exact version match
						if (requested.Version != null && candidate.Version != null && candidate.Version == requested.Version)
						{
							assemblyPath = dll;
							return true; // exact match found
						}

						// Track highest version if no exact match
						if (bestCandidate == null || candidate.Version > bestCandidate.Version)
						{
							bestCandidate = candidate;
							bestCandidatePath = dll;
						}
					}
					catch
					{
						// ignore invalid assemblies
					}
				}
			}

			// Return highest version if no exact match
			assemblyPath = bestCandidatePath;
			return assemblyPath != null;
		}
	}
}
