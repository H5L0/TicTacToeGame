using System;
using System.Collections.Generic;
using System.Reflection;

namespace HL
{
	public static class SubClassUtility
	{
		public static List<string> GetSubClassNames(Type baseType, ICollection<Type> subTypes)
		{
			List<string> names = new List<string>(subTypes.Count);
			foreach (var type in subTypes)
			{
				names.Add(ExtractSubClassName(baseType, type));
			}
			return names;
		}


		public static string ExtractSubClassName(Type baseClass, Type subClass)
		{
			if (baseClass == null || subClass == null)
				return null;
			return StandardizeVarName(subClass.Name.Replace(baseClass.Name + "_", string.Empty));
		}


		public static string StandardizeVarName(string name)
		{
			//' ', 'i', 'j'
			if (name.Length < 2) return name;
			//'iValue', 'tKeyIndex'
			if (char.IsLower(name[0]) && char.IsUpper(name[1])) return name;

			var sb = new System.Text.StringBuilder(name.Length + 2);

			//首字母大写
			var c0 = name[0];
			if (char.IsLetter(c0) && char.IsLower(c0))
				sb.Append((char)(c0 + 'A' - 'a'));
			else
				sb.Append(c0);

			for (int i = 1; i < name.Length; i++)
			{
				char c = name[i];
				if (char.IsUpper(c) && char.IsLower(name[i - 1]))
				{
					sb.Append(' ');
				}
				sb.Append(c);
			}
			return sb.ToString();
		}


		public static List<Type> GetSubClassTypes(Type baseType)
		{
			List<Type> subClassTypes = new List<Type>(8);
			var allTypes = baseType.Assembly.GetExportedTypes();
			foreach (var subClassType in allTypes)
			{
				if (subClassType.IsSubclassOf(baseType) && !subClassType.IsAbstract)
				{
					subClassTypes.Add(subClassType);
				}
			}
			return subClassTypes;
		}

		public static object CreateSubClassInstance(Type baseType, string name)
		{
			var types = GetSubClassTypes(baseType);
			foreach (var type in types)
			{
				var subName = type.Name.Replace(baseType.Name + "_", string.Empty);
				if (subName.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					return type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
				}
			}
			return null;
		}

		public static object CloneObject(object obj)
		{
			if (obj == null)
				return null;

			if (obj is ICloneable cloneable)
			{
				return cloneable.Clone();
			}
			else
			{
				return obj.GetType()
					.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(obj, Array.Empty<object>());
			}
		}
	}
}
