namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;

	/// <summary>
	/// Builds and inspects the paths that uniquely identify orchestration input items.
	/// A path is the chain of names from the root of the definition down to the item.
	/// </summary>
	public static class OrchestrationInputPath
	{
		/// <summary>
		/// The character that separates the names within a path.
		/// </summary>
		public const char Separator = '/';

		/// <summary>
		/// Combines the path of a parent item with the name of one of its children.
		/// </summary>
		/// <param name="parentPath">The path of the parent item, or <see langword="null"/> for a root level item.</param>
		/// <param name="name">The name of the child item.</param>
		/// <returns>The full path of the child item.</returns>
		public static string Combine(string parentPath, string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
			}

			return String.IsNullOrEmpty(parentPath) ? name : String.Concat(parentPath, Separator, name);
		}

		/// <summary>
		/// Determines whether the specified name can be used for an orchestration input item.
		/// </summary>
		/// <param name="name">The name to verify.</param>
		/// <returns><see langword="true"/> when the name is valid; otherwise, <see langword="false"/>.</returns>
		public static bool IsValidName(string name)
		{
			return !String.IsNullOrWhiteSpace(name) && name.IndexOf(Separator) < 0;
		}
	}
}
