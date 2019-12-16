using System;
using System.Collections.Generic;
using static Compiler.Compiler;
using Token = Compiler.Tokenizer.Token;
using TokenizerKeyword = Compiler.Tokenizer.Tokenizer.Keyword;

namespace Compiler.Parser.Nodes
{
	public abstract class Node
	{
		public Node Parent;
		public List<Node> Children;

		public Node() => Children = new List<Node>();

		public sealed override string ToString() => ToString("", true, false);

		public abstract string ToString(string indent, bool last, bool empty);

		public string GetIndent(string indent, bool last) => indent + (last ? "  " : "| ");
		public string GetPrefix(string indent, bool last) => indent + (last ? "└─" : "├─");
	}

	public interface IScope
	{
		Dictionary<string, VariableInfo> Variables { get; set; }
	}

	public class GlobalScope : Node, IScope
	{
		public static GlobalScope Instance { get; private set; }

		public Dictionary<string, VariableInfo> Variables { get; set; }

		public GlobalScope()
		{
			if (Instance != null) {
				throw new InvalidOperationException();
			}
			Instance = this;
		}

		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class TypeDefinition : Node
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class FieldDefinition : Node
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class MethodDefinition : Node
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public abstract class Expression : Node
	{
		public static readonly TypeInfo NullTypeInfo	= Types["null"];
		public static readonly TypeInfo VoidTypeInfo	= Types["void"];
		public static readonly TypeInfo BoolTypeInfo	= Types["bool"];
		public static readonly TypeInfo CharTypeInfo	= Types["char"];
		public static readonly TypeInfo IntTypeInfo		= Types["int"];
		public static readonly TypeInfo FloatTypeInfo	= Types["float"];
		public static readonly TypeInfo StringTypeInfo	= Types["string"];

		public Expression() { }

		public static Compiler.Type ConvertTokenType(Token token)
		{
			switch (token.TypeId) {
				case Token.Type.Keyword:
					switch ((TokenizerKeyword)token.Value) {
						case TokenizerKeyword.Null: return new Compiler.Type(NullTypeInfo);
						case TokenizerKeyword.Void: return new Compiler.Type(VoidTypeInfo);
						case TokenizerKeyword.True: return new Compiler.Type(BoolTypeInfo);
						case TokenizerKeyword.False: return new Compiler.Type(BoolTypeInfo);
						case TokenizerKeyword.Int: return new Compiler.Type(IntTypeInfo);
						case TokenizerKeyword.Float: return new Compiler.Type(FloatTypeInfo);
						case TokenizerKeyword.Char: return new Compiler.Type(CharTypeInfo);
						case TokenizerKeyword.String: return new Compiler.Type(StringTypeInfo);
						default: throw new InvalidOperationException();
					}
				case Token.Type.Int: return new Compiler.Type(IntTypeInfo);
				case Token.Type.Char: return new Compiler.Type(CharTypeInfo);
				case Token.Type.Float: return new Compiler.Type(FloatTypeInfo);
				case Token.Type.String: return new Compiler.Type(StringTypeInfo);
				default: throw new InvalidOperationException();
			}
		}
	}

	public class ObjectCreation : Expression
	{
		public readonly Compiler.Type Type;

		public ObjectCreation(TypeInfo typeInfo, Invocation invocation)
		{
			Type = new Compiler.Type(typeInfo);
			Children = new List<Node>(invocation.Parameters);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" new {0}(... ObjectCreation\n", Type.Info.Name);
			for (int i = 0; i < Children.Count; i++) {
				if (Children[i] != null) {
					res += Children[i].ToString(indent, i == Children.Count - 1, false);
				}
			}
			return res;
		}
	}

	public interface IVariable { }

	///<summary>
	/// VariableDefinition and MethodDefinition can be null!
	/// if VariableOrFieldReference != null then this is variable reference.
	/// if MethodDefinition != null then this is method reference.
	///</summary>
	public class VariableOrMemberReference : Expression // todo
	{
		public IVariable VariableOrFieldReference;
		public MethodDefinition MethodReference;
		public readonly string MemberName;

		public VariableOrMemberReference(string name) => MemberName = name;

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" Variable {0}\n", MemberName);
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class Literal : Expression
	{
		public VariableInfo Value;

		public Literal(Token token) => Value = new VariableInfo(ConvertTokenType(token), null) { Value = token.Value };

		public Literal(Compiler.Type type, object value) => Value = new VariableInfo(type, null) { Value = value };

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			res += string.Format(" Literal {0}\n", Value.Value.ToString());
			return res;
		}
	}

	public abstract class Operation : Expression
	{
		public readonly string StringRepresentation;
		public readonly Tokenizer.Tokenizer.Operator Operator;

		public Operation(string stringRepresentation, Tokenizer.Tokenizer.Operator @operator)
		{
			StringRepresentation = stringRepresentation;
			Operator = @operator;
		}

		public static bool IsArithmeticSupported(Compiler.Type type) => 
			type.Info == IntTypeInfo || type.Info == FloatTypeInfo;
	}

	public class BinaryOperation : Operation
	{
		public Expression Left { get => (Expression)Children[0]; set => Children[0] = value; }
		public Expression Right { get => (Expression)Children[1]; set => Children[1] = value; }

		public BinaryOperation(Token token, Expression left, Expression right)
			: base(token.RawValue, (Tokenizer.Tokenizer.Operator)token.Value)
		{
			Children.Capacity = 2;
			Children.Add(left);
			Children.Add(right);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" Binary {0}\n", StringRepresentation);
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class UnaryOperation : Operation
	{
		public Expression Child { get => (Expression)Children[0]; set => Children[0] = value; }

		public UnaryOperation(Token token, Expression child)
			: base(token.RawValue, (Tokenizer.Tokenizer.Operator)token.Value)
		{
			Children.Capacity = 1;
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" Unary {0}\n", StringRepresentation);
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class ArrayCreation : Expression
	{
		public readonly Compiler.Type Type;
		public readonly Expression ArraySize;

		public ArrayCreation(Compiler.Type type, Expression size, List<Expression> data)
		{
			Type = type;
			ArraySize = size;
			Children = new List<Node>(data);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" new {0} ArrayCreation\n", Type.Info.Name);
			res += ArraySize.ToString(indent, false, false);
			for (int i = 0; i < Children.Count; i++) {
				if (Children[i] != null) {
					res += Children[i].ToString(indent, i == Children.Count - 1, false);
				} else {
					res += indent + (i == Children.Count - 1 ? "└─" : "├─") + " null";
				}
			}
			return res;
		}
	}

	public class ArrayAccess : Expression
	{
		public Expression Index { get => (Expression)Children[0]; set => Children[0] = value; }
		public Expression Child { get => (Expression)Children[1]; set => Children[1] = value; }

		public ArrayAccess(Expression index, Expression child)
		{
			Children.Capacity = 2;
			Children.Add(index);
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += "[] index child\n";
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class MemberAccess : Expression
	{
		public readonly string MemberName;
		public Expression Child { get => (Expression)Children[0]; set => Children[0] = value; }

		public MemberAccess(string name, Expression child)
		{
			MemberName = name;
			Children.Capacity = 1;
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += " ." + MemberName + "\n";
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class Parenthesis : Expression
	{
		public Expression Child { get => (Expression)Children[0]; set => Children[0] = value; }

		public Parenthesis(Expression child)
		{
			Children.Capacity = 1;
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += "() Parenthesis\n";
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class Invocation : Expression
	{
		public static readonly Invocation Constructor = new Invocation(null, null);

		public readonly List<Expression> Parameters;
		public Expression Child { get => (Expression)Children[0]; set => Children[0] = value; }

		public Invocation(List<Expression> parameters, Expression child)
		{
			Parameters = parameters;
			Children.Capacity = 1;
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += " () Invocation\n";
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, Parameters.Count == 0, false);
			}
			for (int i = 0; i < Parameters.Count; i++) {
				if (Parameters[i] != null) {
					res += Parameters[i].ToString(indent, i == Parameters.Count - 1, false);
				} else {
					res += indent + (i == Parameters.Count - 1 ? "└─" : "├─") + " null";
				}
			}
			return res;
		}
	}

	public class TypeReference : Expression
	{
		public readonly TypeInfo Type;

		public TypeReference(Token token)
		{
			Type = Types[(string)token.Value];
			Children.Capacity = 0;
		}

		public TypeReference(TypeInfo type)
		{
			Type = type;
			Children.Capacity = 0;
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += " " + Type.Name + " TypeReference\n";
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public class TypeCast : Expression
	{
		public readonly TypeInfo Type;
		public readonly Expression Child;

		public TypeCast(TypeInfo type, Expression child)
		{
			Type = type;
			Children.Capacity = 1;
			Children.Add(child);
		}

		public override string ToString(string indent, bool last, bool empty)
		{
			string res = GetPrefix(indent, last);
			indent = GetIndent(indent, last);
			res += string.Format(" ({0}) TypeCast\n", Type.Name);
			for (int i = 0; i < Children.Count; i++) {
				res += Children[i].ToString(indent, i == Children.Count - 1, false);
			}
			return res;
		}
	}

	public interface IStatement { }

	public class VariableDefinition : Node, IStatement
	{
		public readonly Compiler.Type Type;
		public readonly string Name;
		public object Value;

		public VariableDefinition(Compiler.Type type, string name)
		{
			Type = type;
			Name = name;
		}

		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class CallStatement : Node, IStatement
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class Assignment : Node, IStatement
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class Block : Node, IStatement, IScope
	{
		public Dictionary<string, VariableInfo> Variables { get; set; }

		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class If : Node, IStatement
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class While : Node, IStatement
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}

	public class Break : Node, IStatement
	{
		public override string ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
	}
}