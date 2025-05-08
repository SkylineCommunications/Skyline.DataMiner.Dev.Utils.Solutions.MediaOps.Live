namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;

	public static class NameUtil
	{
		public static bool Validate(string name, out string error)
		{
			if (name == null)
			{
				error = "Name cannot be null.";
				return false;
			}

			if (String.IsNullOrWhiteSpace(name))
			{
				error = "Name cannot be whitespace or empty.";
				return false;
			}

			if (name.Length < 1 || name.Length > 100)
			{
				error = "Name must be between 1 and 100 characters long.";
				return false;
			}

			if (Char.IsWhiteSpace(name[0]) || Char.IsWhiteSpace(name[name.Length - 1]))
			{
				error = "Name cannot start or end with whitespace.";
				return false;
			}

			error = null;
			return true;
		}

		public static void Validate(string name)
		{
			if (!Validate(name, out var error))
			{
				throw new ArgumentException(error, nameof(name));
			}
		}
	}
}
