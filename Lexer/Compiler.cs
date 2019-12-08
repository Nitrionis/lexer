using System.Collections.Generic;

namespace Compiler
{
	public class Compiler
	{
		public class TypeInfo
		{
			public class FieldInfo
			{
				public readonly Type Type;
				public readonly string Name;
			}

			public class MethodInfo
			{
				public class PramsInfo
				{
					public readonly Type Type;
					public readonly string Name;

					public PramsInfo(Type type, string name)
					{
						Type = type;
						Name = name;
					}
				}

				public readonly bool IsStatic;
				public readonly Type OutputType;
				public readonly string Name;
				public readonly List<PramsInfo> Prams;

				public MethodInfo(Type outputType, string name, List<PramsInfo> prams = null)
				{
					OutputType = outputType;
					Name = name;
					Prams = prams ?? new List<PramsInfo>();
				}
			}

			public readonly string Name;
			public readonly Dictionary<string, FieldInfo> Fields;
			public readonly Dictionary<string, MethodInfo> Methods;

			public TypeInfo(string name)
			{
				Name = name;
				Fields = new Dictionary<string, FieldInfo>();
				Methods = new Dictionary<string, MethodInfo>();
			}

			public bool ContainsMember(string identifier) => 
				Fields.ContainsKey(identifier) || Methods.ContainsKey(identifier);
		}

		public class Type
		{
			public readonly TypeInfo Info;
			public readonly uint ArrayRang;

			public Type(TypeInfo info, uint arrayRang = 0)
			{
				Info = info;
				ArrayRang = arrayRang;
			}
		}

		public class VariableInfo
		{
			public readonly Type Type;
			public readonly string Name;
			public object Value;

			public VariableInfo(Type type, string name)
			{
				Type = type;
				Name = name;
			}
		}

		public static readonly Dictionary<string, TypeInfo> Types;

		static Compiler()
		{
			Types = new Dictionary<string, TypeInfo>() {
				["null"]	= new TypeInfo("null"),
				["void"]	= new TypeInfo("void"),
				["bool"]	= new TypeInfo("bool"),
				["char"]	= new TypeInfo("char"),
				["int"]		= new TypeInfo("int"),
				["float"]	= new TypeInfo("float"),
				["string"]	= new TypeInfo("string"),
			};
		}
	}
}
