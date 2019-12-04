using System;

namespace Compiler.AST
{
	public abstract class Node
	{
		public Node[] Children;

		public sealed override string ToString() => throw new InvalidOperationException();

		public abstract string ToString(string indent);
	}

	public abstract class Expression : Node
	{

	}

	public class UnaryOperation : Expression
	{
		public Tokenizer.Token Token;

		public Node Child => Children?[0];

		public override string ToString(string indent) => throw new NotImplementedException();
	}

	public class BinaryOperation : Expression
	{
		public Tokenizer.Token Token;

		public Node Left => Children?[0];
		public Node Right => Children?[1];

		public override string ToString(string indent) => throw new NotImplementedException();
	}

	public class Tree
	{
		private Node root;

		public Tree()
		{

		}

		public override string ToString()
		{
			throw new InvalidOperationException(); // TODO
		}
	}
}
