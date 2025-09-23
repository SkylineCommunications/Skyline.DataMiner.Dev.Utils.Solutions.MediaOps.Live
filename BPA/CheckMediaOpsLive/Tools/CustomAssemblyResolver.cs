namespace CheckMediaOpsLive.Tools
{
	using System;
	using System.Collections.Generic;
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
			var candidates = GetCandidates(requested.Name).ToList();

			if (!candidates.Any())
			{
				assemblyPath = null;
				return false;
			}

			// Prefer exact version
			if (requested.Version != null)
			{
				var exact = candidates
					.Where(c => c.Version == requested.Version)
					.OrderByDescending(c => c.WriteTime)
					.FirstOrDefault();

				if (exact != null)
				{
					assemblyPath = exact.Path;
					return true;
				}
			}

			// Otherwise pick highest version, newest file
			var best = candidates
				.OrderByDescending(c => c.Version)
				.ThenByDescending(c => c.WriteTime)
				.First();

			assemblyPath = best.Path;
			return true;
		}

		private IEnumerable<AssemblyCandidate> GetCandidates(string assemblyName)
		{
			var directories = new[]
			{
				AppDomain.CurrentDomain.BaseDirectory,
				@"C:\Skyline DataMiner\Files",
				@"C:\Skyline DataMiner\ProtocolScripts"
			}
			.Where(p => !String.IsNullOrWhiteSpace(p))
			.Distinct();

			foreach (var dir in directories.Where(Directory.Exists))
			{
				foreach (var file in Directory.GetFiles(dir, $"{assemblyName}.dll", SearchOption.AllDirectories))
				{
					var assembly = SafeGetAssemblyName(file);

					if (assembly != null && String.Equals(assembly.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
					{
						yield return new AssemblyCandidate(file, assembly.Version, File.GetLastWriteTimeUtc(file));
					}
				}
			}
		}

		private AssemblyName SafeGetAssemblyName(string path)
		{
			try { return AssemblyName.GetAssemblyName(path); }
			catch { return null; }
		}

		private class AssemblyCandidate
		{
			public AssemblyCandidate(string path, Version version, DateTime writeTime)
			{
				Path = path;
				Version = version;
				WriteTime = writeTime;
			}

			public string Path { get; }
			public Version Version { get; }
			public DateTime WriteTime { get; }
		}
	}
}
