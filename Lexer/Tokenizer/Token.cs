namespace Compiler.Tokenizer
{
	public class Token
	{
		public enum Type
		{
			Undefined,
			Word,
			Keyword,
			Operator,
			ConstStr,
			Int,
			Float,
			Char
		}

		public Type TypeId = Type.Undefined;
		public object Value;
		public string RawValue = "";
		public int RowIndex = -1;
		public int ColIndex = -1;
		public string Message = null;

		public Token() { }
		public Token(Type type, object value, string rawValue, int rowIndex, int colIndex)
		{
			TypeId = type;
			Value = value;
			RawValue = rawValue;
			RowIndex = rowIndex;
			ColIndex = colIndex;
		}

		public bool IsError => Value == null;

		public override string ToString() => string.Format(
			"r:{0,3} c:{1,3} {2,16} {3,-16} raw {4}",
			RowIndex, ColIndex, !IsError ? TypeId.ToString() : "Error-" + TypeId.ToString(), Value, RawValue);

		public static bool Equals(Token t1, Token t2) => 
			(t1 == null && t2 == null) ||
			(t1.TypeId == t2.TypeId &&
			object.Equals(t1.Value, t2.Value) &&
			t1.RawValue == t2.RawValue &&
			t1.RowIndex == t2.RowIndex &&
			t1.ColIndex == t2.ColIndex &&
			object.Equals(t1.Message, t2.Message));
	}
}
