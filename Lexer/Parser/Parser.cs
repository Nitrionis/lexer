using System;
using System.Collections.Generic;
using Compiler.Tokenizer;
using Compiler.Parser.Nodes;
using Token = Compiler.Tokenizer.Token;
using Operator = Compiler.Tokenizer.Tokenizer.Operator;


namespace Compiler.Parser
{
	using Tokenizer = Tokenizer.Tokenizer;

	public class ParserException : InvalidOperationException
	{
		public ParserException() { }

		public ParserException(string message) : base(message) { }

		public ParserException(string message, Exception inner) : base(message, inner) { }
	}

	public class Parser
	{
		private readonly Tokenizer tokenizer;
		// Use to look into the future.
		private readonly Stack<Token> tokens;


		public Parser(string path)
		{
			//tokens = new Stack<Token>();
		}

		private Token PeekToken() => tokens.Count > 0 ? tokens.Peek() : tokenizer.Peek();

		private bool NextToken() => TryGetToken() != null;

		private void NextTokenUnsafe()
		{
			if (TryGetToken() == null) throw new InvalidOperationException();
		}

		private Token TryGetToken()
		{
			var token = tokens.Count > 0 ? tokens.Pop() : tokenizer.Next();
			return token == null ? null : token.IsError ? 
				throw new TokenizerException("Lexical analysis failed.") : token;
		}

		public Token PeekAndNext()
		{
			var t = PeekToken();
			NextToken();
			return t;
		}

		public Token NextAndPeek()
		{
			NextToken();
			return PeekToken();
		}

		private Expression ParseExpression()
		{
			NextTokenUnsafe();
			return ParseAssignment();
		}

		private Expression ParseAssignment() // _ = _
		{
			var left = ParseConditionalOrExpression();
			if (left == null) throw new InvalidOperationException();
			var token = PeekToken();
			if (token == null ||
				!((token.TypeId == Token.Type.Operator) &&
				 ((Operator)token.Value == Operator.Assignment))) 
			{
				return left;
			}
			NextTokenUnsafe();
			var right = ParseAssignment();
			left = new BinaryOperation(token, left, right);
			return left;
		} 

		private Expression ParseConditionalOrExpression() // _ || _
		{
			var left = ParseConditionalAndExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.LogicalOr))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseConditionalAndExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseConditionalAndExpression() // _ && _
		{
			var left = ParseBitwiseOrExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.LogicalAnd))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseBitwiseOrExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseBitwiseOrExpression() // _ | _
		{
			var left = ParseBitwiseAndExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.BitwiseOr))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseBitwiseAndExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseBitwiseAndExpression() // _ & _
		{
			var left = ParseEqualityExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.BitwiseAnd))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseEqualityExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseEqualityExpression() // _ == _  _ != _
		{
			var left = ParseRelationalExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.EqualityOperator))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseRelationalExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseRelationalExpression() // _ < _  _ > _
		{
			var left = ParseAdditiveExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.RelationalOperator))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseAdditiveExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseAdditiveExpression() // _ + _  _ - _
		{
			var left = ParseMultiplicativeExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.AdditiveOperator))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseMultiplicativeExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseMultiplicativeExpression() // _ * _  _ / _  _ % _
		{
			var left = ParseUnaryExpression();
			if (left == null) throw new InvalidOperationException();
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 ((Operator)token.Value == Operator.MultiplicativeOperator))) 
				{
					break;
				}
				NextTokenUnsafe();
				var right = ParseUnaryExpression();
				left = new BinaryOperation(token, left, right);
			}
			return left;
		}

		private Expression ParseUnaryExpression()  // +_ -_ !_ ~_ (Type)_
		{
			var token = PeekToken();
			if (token != null && token.TypeId == Token.Type.Operator)
			{
				if ((Operator)token.Value == Operator.UnaryOperator) {
					NextTokenUnsafe();
					return new UnaryOperation(token, ParseUnaryExpression());
				}
				if ((Operator)token.Value == Operator.OpenParenthesis) {
					NextTokenUnsafe();
					var maybeType = PeekToken();
					if (IsTypeName(maybeType)) {
						NextTokenUnsafe();
						var maybeCloseParen = PeekToken();
						if (IsOperator(maybeCloseParen, Operator.CloseParenthesis)) {
							return new TypeCast(maybeType, ParseUnaryExpression());
						}
						tokens.Push(maybeCloseParen);
					}
					tokens.Push(maybeType);
					tokens.Push(token);
					return ParsePrimaryExpression();
				}
			}
			return ParsePrimaryExpression();
		}

		private static bool IsType(string name) => Compiler.Types.ContainsKey(name);

		private static bool IsTypeName(Token token) =>
			token.TypeId == Token.Type.Identifier && IsType((string)token.Value);

		private static bool IsOperator(Token token, Operator @operator) =>
			token.TypeId == Token.Type.Operator && (Operator)token.Value == @operator;

		private Expression ParsePrimaryExpression() // todo
		{
			var token = PeekToken();
			switch (token.TypeId) {
				case Token.Type.Literal: return new Literal(token);
				case Token.Type.Keyword:
					switch ((Tokenizer.Keyword)token.Value) {
						case Tokenizer.Keyword.True: return new Literal(new Compiler.Type(Expression.BoolTypeInfo), true);
						case Tokenizer.Keyword.False: return new Literal(new Compiler.Type(Expression.BoolTypeInfo), false);
						case Tokenizer.Keyword.Null: return new Literal(new Compiler.Type(Expression.NullTypeInfo), null);
						case Tokenizer.Keyword.New: return ParseObjectCreationExpression();
						default: throw new InvalidOperationException();
					}
				case Token.Type.Identifier: return ParseVariableReference(); // todo ??
				case Token.Type.Operator: return ParseParenthesis(); // todo ??
				default: throw new InvalidOperationException();
			}
		}

		private Expression ParseParenthesis()
		{
			var token = PeekToken();
			if ((Operator)token.Value == Operator.OpenParenthesis) {
				NextTokenUnsafe();
				var expression = ParseExpression();
				if (PeekToken().TypeId == Token.Type.Operator && (Operator)token.Value == Operator.CloseParenthesis) {
					throw new ParserException(
						string.Format("(r:{0}, c:{1}) syntax error: ')' expected, but {2} found",
							PeekToken().RowIndex, PeekToken().ColIndex, PeekAndNext().RawValue));
				}
				return expression;
			} else throw new InvalidOperationException();
		}

		// todo ParsePrimaryExpression
		private Expression ParseObjectCreationExpression() => throw new NotImplementedException();

		// todo ParsePrimaryExpression
		private Expression ParseVariableReference()
		{

			throw new NotImplementedException();
		}

		// todo ParsePrimaryExpression
		private Exception ParseFactor()
		{

			throw new NotImplementedException();
		}
	}
}
