using System;
using System.Collections.Generic;
using Compiler.Tokenizer;
using Compiler.Parser.Nodes;
using Token = Compiler.Tokenizer.Token;
using Operator = Compiler.Tokenizer.Tokenizer.Operator;
using Keyword = Compiler.Tokenizer.Tokenizer.Keyword;
using System.Text;
using System.IO;

namespace Compiler.Parser
{
	using Tokenizer = Tokenizer.Tokenizer;

	//using ExpressionsCombinations = System.Collections.Generic.Dictionary<
	//	Nodes.Expression, System.Collections.Generic.Dictionary<
	//		Nodes.Expression, System.Action>>;

	public class ParserException : InvalidOperationException
	{
		public ParserException() { }

		public ParserException(string message) : base(message) { }

		public ParserException(string message, Exception inner) : base(message, inner) { }
	}

	public class WrongTokenFound : ParserException
	{
		public WrongTokenFound(Token token, string rawValue) : base(CreateMessage(token, rawValue)) { }

		private static string CreateMessage(Token token, string rawValue) => 
			string.Format("(r:{0}, c:{1}) syntax error: '{2}' expected, but {3} found",
				token.RowIndex, token.ColIndex, rawValue, token.RawValue);
	}

	public class TypeNotFound : ParserException
	{
		public TypeNotFound(Token token) : base(CreateMessage(token)) { }

		private static string CreateMessage(Token token) =>
			string.Format("(r:{0}, c:{1}) syntax error: type '{2}' not found",
				token.RowIndex, token.ColIndex, (string)token.Value);
	}

	public class InvalidExpressionsCombination : ParserException
	{
		public InvalidExpressionsCombination(string op1, string op2) : base(CreateMessage(op1, op2)) { }

		private static string CreateMessage(string op1, string op2) =>
			string.Format("Syntax error: invalid expressions combination {0} and {1}", op1, op2);
	}

	public class Parser
	{
		private readonly Tokenizer tokenizer;
		private readonly Stack<Token> foreseeableFuture;


		public Parser(Tokenizer tokenizer)
		{
			this.tokenizer = tokenizer;
			foreseeableFuture = new Stack<Token>();
			NextTokenUnsafe();
			var exception = ParseExpression();
			if (exception != null) {
				Console.WriteLine("yes");
			}
		}

		private Token PeekToken() => foreseeableFuture.Count > 0 ? foreseeableFuture.Peek() : tokenizer.Peek();

		private bool NextToken() => TryGetToken() != null;

		private Token NextTokenUnsafe()
		{
			Token token;
			if (null == (token = TryGetToken())) {
				throw new ParserException("Program break");
			}
			return token;
		}

		private Token TryGetToken()
		{
			var token = foreseeableFuture.Count > 0 ? foreseeableFuture.Pop() : tokenizer.Next();
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
			return ParseAssignment();
		}

		private Expression ParseAssignment() // _ = _
		{
			var left = ParseConditionalOrExpression();
			if (left == null) return null;
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
			if (left == null) return null;
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
			if (left == null) return null;
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
			if (left == null) return null;
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
			if (left == null) return null;
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
			if (left == null) return null;
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 (((Operator)token.Value & Operator.EqualityOperator) != 0))) 
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
			if (left == null) return null;
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 (((Operator)token.Value & Operator.RelationalOperator) != 0))) 
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
			if (left == null) return null;
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 (((Operator)token.Value & Operator.AdditiveOperator) != 0))) 
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
			if (left == null) return null;
			while (true) {
				var token = PeekToken();
				if (token == null ||
					!((token.TypeId == Token.Type.Operator) &&
					 (((Operator)token.Value & Operator.MultiplicativeOperator) != 0))) 
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
			if (token == null) return null;
			if (token.TypeId == Token.Type.Operator)
			{
				if (((Operator)token.Value & Operator.UnaryOperator) != 0) {
					NextTokenUnsafe();
					return new UnaryOperation(token, ParseUnaryExpression());
				}
				if ((Operator)token.Value == Operator.OpenParenthesis) {
					var expression = ParseArrayCreationExpression();
					if (expression == null) return null;
					var parenthesis = expression as Parenthesis;
					if (parenthesis != null) {
						var typeReference = parenthesis.Children[0] as TypeReference;
						if (typeReference != null) {
							NextTokenUnsafe();
							return new TypeCast(typeReference.Type, ParseUnaryExpression());
						}
					}
					return expression;
				}
			}
			return ParseArrayCreationExpression();
		}

		private static bool IsTypeName(Token token) =>
			token.TypeId == Token.Type.Identifier && Compiler.Types.ContainsKey((string)token.Value);

		private static bool IsOperator(Token token, Operator @operator) =>
			token.TypeId == Token.Type.Operator && (Operator)token.Value == @operator;

		private Expression ParseArrayCreationExpression()
		{
			var token = PeekToken();
			if (token != null && token.TypeId == Token.Type.Keyword && (Keyword)token.Value == Keyword.New) {
				var typeName = NextTokenUnsafe();
				if (IsTypeName(typeName)) {
					var openSquareBracket = NextTokenUnsafe();
					if (IsOperator(openSquareBracket, Operator.OpenSquareBracket)) {
						NextToken();
						var size = ParseExpression();
						token = PeekToken();
						if (size == null) {
							throw new ParserException(string.Format(
								"(r:{0}, c:{1}) syntax error: array size not set",
								token.RowIndex, token.ColIndex));
						}
						if (!IsOperator(token, Operator.CloseSquareBracket)) {
							throw new WrongTokenFound(token, "]");
						}
						uint rang = ParseArrayRang();
						var data = ParseArrayData();
						return new ArrayCreation(new Compiler.Type(Compiler.Types[(string)typeName.Value], rang), size, data);
					}
					foreseeableFuture.Push(typeName);
				}
				foreseeableFuture.Push(token);
			}
			return ParsePrimaryExpression();
		}

		private uint ParseArrayRang()
		{
			Token token;
			for (uint rang = 1; true; rang++) {
				NextTokenUnsafe();
				token = PeekToken();
				if (IsOperator(token, Operator.OpenCurlyBrace)) {
					return rang;
				}
				if (!IsOperator(token, Operator.OpenSquareBracket)) {
					throw new WrongTokenFound(token, "[");
				}
				NextTokenUnsafe();
				token = PeekToken();
				if (!IsOperator(token, Operator.CloseSquareBracket)) {
					throw new WrongTokenFound(token, "]");
				}
			}
		}

		private List<Expression> ParseArrayData()
		{
			var parameters = new List<Expression>();
			var token = PeekToken();
			if (IsOperator(token, Operator.OpenCurlyBrace)) {
				while (true) {
					NextTokenUnsafe();
					parameters.Add(ParseExpression());
					token = PeekToken();
					if (!IsOperator(token, Operator.Comma)) {
						break;
					}
				}
				if (!IsOperator(token, Operator.CloseCurlyBrace)) {
					throw new WrongTokenFound(token, "}");
				}
				NextToken();
			}
			return parameters;
		}

		private Expression ParsePrimaryExpression()
		{
			var left = PrimaryRouter(null);
			if (left == null) return null;
			while (true) {
				var right = PrimaryRouter(left);
				if (right == null) {
					break;
				}
				left = right;
			}
			return left;
		}

		private Expression PrimaryRouter(Expression previousExpression)
		{
			var token = PeekToken();
			if (token != null) {
				switch (token.TypeId) {
					case Token.Type.Int:
					case Token.Type.Float:
					case Token.Type.Char:
					case Token.Type.String: NextToken(); return new Literal(token);
					case Token.Type.Keyword:
						switch ((Keyword)token.Value) {
							case Keyword.Int: NextToken(); return new TypeReference(Expression.IntTypeInfo);
							case Keyword.Float: NextToken(); return new TypeReference(Expression.FloatTypeInfo);
							case Keyword.Char: NextToken(); return new TypeReference(Expression.CharTypeInfo);
							case Keyword.String: NextToken(); return new TypeReference(Expression.StringTypeInfo);
							case Keyword.True: NextToken(); return new Literal(new Compiler.Type(Expression.BoolTypeInfo), true);
							case Keyword.False: NextToken(); return new Literal(new Compiler.Type(Expression.BoolTypeInfo), false);
							case Keyword.Null: NextToken(); return new Literal(new Compiler.Type(Expression.NullTypeInfo), null);
							case Keyword.New: return ParseObjectCreationExpression(previousExpression);
							default: throw new ParserException(
								string.Format("(r:{0}, c:{1}) syntax error: bad keyword '{2}'",
									token.RowIndex, token.ColIndex, token.RawValue));
						}
					case Token.Type.Identifier: return ParseReference(previousExpression);
					case Token.Type.Operator: 
						switch ((Operator)PeekToken().Value) {
							case Operator.OpenParenthesis: return ParseParenthesis(previousExpression);
							case Operator.OpenSquareBracket: return ParseArrayAccess(previousExpression);
							case Operator.Dot: return ParseMemberAccess(previousExpression);
							default: return null;
						}
					default: throw new InvalidOperationException();
				}
			}
			return null;
		}

		private Expression ParseParenthesis(Expression previousExpression)
		{
			if (previousExpression != null) {
				return ParseInvocation(previousExpression);
			}
			NextToken();
			var token = PeekToken();
			if (token == null) return null;
			if (IsOperator(token, Operator.CloseParenthesis)) {
				throw new ParserException(string.Format(
					"(r:{0}, c:{1}) syntax error: empty parenthesis expression",
					token.RowIndex, token.ColIndex));
			}
			var expression = ParseExpression();
			token = PeekToken();
			if (!IsOperator(token, Operator.CloseParenthesis)) {
				throw new WrongTokenFound(token, ")");
			}
			return new Parenthesis(expression);
		}

		private Expression ParseInvocation(Expression previousExpression)
		{
			Token token;
			var parameters = new List<Expression>();
			while (true) {
				NextToken();
				parameters.Add(ParseExpression());
				token = PeekToken();
				if (!IsOperator(token, Operator.Comma)) {
					break;
				}
			}
			if (!IsOperator(token, Operator.CloseParenthesis)) {
				throw new WrongTokenFound(token, ")");
			}
			NextToken();
			return new Invocation(parameters, previousExpression);
		}

		private Expression ParseArrayAccess(Expression previousExpression)
		{
			NextTokenUnsafe();
			var expression = ParseExpression();
			var token = PeekToken();
			if (!IsOperator(token, Operator.CloseSquareBracket)) {
				throw new WrongTokenFound(token, "]");
			}
			NextToken();
			return new ArrayAccess(expression, previousExpression);
		}

		private Expression ParseMemberAccess(Expression previousExpression)
		{
			var token = NextTokenUnsafe();
			if (token.TypeId != Token.Type.Identifier) {
				throw new WrongTokenFound(token, "identifier");
			}
			NextToken();
			return new MemberAccess((string)token.Value, previousExpression);
		}

		private Expression ParseObjectCreationExpression(Expression previousExpression)
		{
			var token = NextTokenUnsafe();
			if (token.TypeId != Token.Type.Identifier) {
				throw new WrongTokenFound(token, "identifier");
			}
			if (!Compiler.Types.ContainsKey((string)token.Value)) {
				throw new TypeNotFound(token);
			}
			var maybeParenthesis = NextTokenUnsafe();
			if (!IsOperator(maybeParenthesis, Operator.OpenParenthesis)) {
				throw new WrongTokenFound(token, "(");
			}
			NextToken();
			return new ObjectCreation(
				Compiler.Types[(string)token.Value], 
				(Invocation)ParseInvocation(Invocation.Constructor));
		}

		private Expression ParseReference(Expression previousExpression)
		{
			var token = PeekToken();
			NextToken();
			return Compiler.Types.ContainsKey((string)token.Value) ?
				(Expression)(new TypeReference(token)) : 
				(Expression)(new VariableOrMemberReference((string)token.Value));
		}
	}
}
