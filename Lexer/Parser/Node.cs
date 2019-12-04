using System;
using System.Collections.Generic;
using static Compiler.Compiler;
using Token = Compiler.Tokenizer.Token;
using Tokenizer = Compiler.Tokenizer.Tokenizer;
using TokenizerKeyword = Compiler.Tokenizer.Tokenizer.Keyword;

namespace Compiler.Parser.Nodes
{
	public abstract class Node
	{
		public Node Parent;
		public List<Node> Children;

		public Node() => Children = new List<Node>();

		public sealed override string ToString() => throw new NotImplementedException();

		protected abstract void ToString(string indent, bool last, bool empty);
	}

	public class SyntaxTree : Node
	{
		public Node Root;

		public SyntaxTree()
		{

		}

		protected override void ToString(string indent, bool last, bool empty) => throw new NotImplementedException();
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

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class TypeDefinition : Node
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class FunctionDefinition : Node
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
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
					if ((((TokenizerKeyword)token.Value) & TokenizerKeyword.Bool) != 0) {
						return new Compiler.Type(BoolTypeInfo);
					} else if ((((TokenizerKeyword)token.Value) & TokenizerKeyword.Null) != 0) {
						return new Compiler.Type(NullTypeInfo);
					} else {
						throw new InvalidOperationException();
					}
				case Token.Type.Int: return new Compiler.Type(IntTypeInfo);
				case Token.Type.Char: return new Compiler.Type(CharTypeInfo);
				case Token.Type.Float: return new Compiler.Type(FloatTypeInfo);
				case Token.Type.String: return new Compiler.Type(StringTypeInfo);
				default: throw new InvalidOperationException();
			}
		}
	}

	public class Variable : Expression
	{
		public VariableInfo Value;
		public readonly string Name;

		public Variable(string name, VariableInfo value)
		{
			Name = name;
			Value = value;
		}

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class Literal : Expression
	{
		public VariableInfo Value;

		public Literal(Token token) => Value = new VariableInfo(ConvertTokenType(token), null) { Value = token.Value };

		public Literal(Compiler.Type type, object value) => Value = new VariableInfo(type, null) { Value = value };

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
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
		public Expression Right { get => (Expression) Children[1]; set => Children[1] = value; }

		public BinaryOperation(Token token, Expression left, Expression right)
			: base(token.RawValue, (Tokenizer.Tokenizer.Operator)token.Value)
		{
			Children.Capacity = 2;
			Children.Add(left);
			Children.Add(right);
		}

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class UnaryOperation : Operation
	{
		public readonly Expression Child;

		public UnaryOperation(Token token, Expression child)
			: base(token.RawValue, (Tokenizer.Tokenizer.Operator)token.Value)
		{
			Children.Capacity = 1;
			Children.Add(child);
		}

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class ArrayAccess : Expression
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class FieldAccess : Expression
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class MethodCall : Expression
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class TypeCast : Expression
	{
		public readonly TypeInfo type;
		public readonly Expression Child;

		public TypeCast(Token token, Expression child)
		{
			type = Types[(string)token.Value];
			Children.Capacity = 1;
			Children.Add(child);
		}

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public interface IStatement
	{

	}

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

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class CallStatement : Node, IStatement
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class Assignment : Node, IStatement
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class Block : Node, IStatement, IScope
	{
		public Dictionary<string, VariableInfo> Variables { get; set; }

		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class If : Node, IStatement
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class While : Node, IStatement
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}

	public class Break : Node, IStatement
	{
		protected override void ToString(string indent, bool last, bool empty) { /*todo*/ }
	}
}