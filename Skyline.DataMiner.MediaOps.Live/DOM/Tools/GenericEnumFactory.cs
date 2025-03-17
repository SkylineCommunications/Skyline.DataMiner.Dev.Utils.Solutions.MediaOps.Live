namespace Skyline.DataMiner.MediaOps.Live.DOM.Tools
{
	using System;
	using System.ComponentModel;

	using Skyline.DataMiner.Net.GenericEnums;

	internal static class GenericEnumFactory
	{
		public static GenericEnum<int> Create<T>() where T : Enum
		{
			var enumType = typeof(T);
			var enumNames = Enum.GetNames(enumType);
			var enumValues = Enum.GetValues(enumType);

			var genericEnum = new GenericEnum<int>();

			for (var i = 0; i < enumNames.Length; i++)
			{
				var enumMember = enumType.GetField(enumNames[i]);
				var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(enumMember, typeof(DescriptionAttribute));
				var displayName = attribute == null ? enumNames[i] : attribute.Description;
				var value = (int)enumValues.GetValue(i);

				genericEnum.AddEntry(displayName, value);
			}

			return genericEnum;
		}
	}
}
