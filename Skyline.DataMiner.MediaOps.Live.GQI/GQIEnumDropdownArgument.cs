namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;

	/// <summary>
	/// GQI dropdown argument for enum types.
	/// </summary>
	/// <typeparam name="T">The enum type for the dropdown.</typeparam>
	public class GQIEnumDropdownArgument<T> : GQIStringDropdownArgument
		where T : Enum
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GQIEnumDropdownArgument{T}"/> class.
		/// </summary>
		/// <param name="name">The name of the argument.</param>
		/// <exception cref="ArgumentException">Thrown when T is not an enumerated type.</exception>
		public GQIEnumDropdownArgument(string name) : base(name, Enum.GetNames(typeof(T)))
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}
		}

		/// <summary>
		/// Gets the enum value from the processed arguments.
		/// </summary>
		/// <param name="args">The processed input arguments.</param>
		/// <returns>The enum value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
		public T GetArgumentValue(OnArgumentsProcessedInputArgs args)
		{
			if (args == null)
			{
				throw new ArgumentNullException(nameof(args));
			}

			return (T)Enum.Parse(typeof(T), args.GetArgumentValue(this));
		}

		/// <summary>
		/// Tries to get the enum value from the processed arguments.
		/// </summary>
		/// <param name="args">The processed input arguments.</param>
		/// <param name="value">When this method returns, contains the enum value if successful; otherwise, the default value.</param>
		/// <returns>true if the argument value was successfully retrieved; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
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
