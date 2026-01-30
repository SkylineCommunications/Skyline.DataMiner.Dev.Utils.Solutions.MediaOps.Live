namespace Skyline.DataMiner.Solutions.MediaOps.Live.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public class GQIEnumDropdownArgument<T> : GQIStringDropdownArgument
		where T : Enum
	{
		public GQIEnumDropdownArgument(string name) : base(name, Enum.GetNames(typeof(T)))
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
		}

		public T GetArgumentValue(OnArgumentsProcessedInputArgs args)
		{
			if (args == null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			return (T)Enum.Parse(typeof(T), args.GetArgumentValue(this));
		}

		public bool TryGetArgumentValue(OnArgumentsProcessedInputArgs args, out T value)
		{
			if (args == null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			value = default;

			if (!args.HasArgumentValue(this))
			{
				return false;
			}

			value = (T)Enum.Parse(typeof(T), args.GetArgumentValue(this));
			return true;
		}
	}
}
