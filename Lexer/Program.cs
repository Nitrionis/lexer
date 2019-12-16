using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Token = Compiler.Tokenizer.Token;
using Operator = Compiler.Tokenizer.Tokenizer.Operator;
using TokenType = Compiler.Tokenizer.Token.Type;

namespace Compiler
{
	class Program
	{
		private static Tokenizer.Tokenizer lexer;

		static void Main(string[] args)
		{
			using (lexer = new Tokenizer.Tokenizer(GenerateStreamFromString(
				"a = b && c || d * (int)(float)(char)(string)-e.a.b.c(a+b, c-d)"
				//"a = new int[1][][][] {new int[1][][]{},new int[1][][]{},new int[1][][]{}}"
			))) {
				Parser.Parser parser = new Parser.Parser(lexer);
				//Test[] tests = GenerateTests();
				//for (int i = 0; i < tests.Length; i++) {
				//	var result = tests[i].Execute();
				//	Console.WriteLine((result.IsDone ? "Done " : "Failed ") + i);
				//	if (!result.IsDone) {
				//		Console.WriteLine("Result");
				//		foreach (var t in result.Tokens) {
				//			Console.WriteLine(t.ToString());
				//		}
				//		Console.WriteLine("Output");
				//		foreach (var t in tests[i].Output) {
				//			Console.WriteLine(t.ToString());
				//		}
				//	}
				//}
			}
		}

		private static Test[] GenerateTests()
		{
			return new Test[] {
				new Test("", new Token[0]),
				new Test("a", new []{ new Token(TokenType.Identifier, "a", "a", 0, 0) }),
				new Test("1", new []{ new Token(TokenType.Int, 1, "1", 0, 0) }),
				new Test("/", new []{ new Token(TokenType.Operator, Operator.Divide, "/", 0, 0) }),
				new Test("//", new Token[0]),
				new Test("//a", new Token[0]),
				new Test("//`1234567890-=~!@#$%^&*()_+qwertyuiop[]QWERTYUIOP{}asdfghjkl;'ASDFGHJKL:|\"\\zxcvbnm,./ZXCVBNM<>?",
					new Token[0]),
				new Test("&&&", new []{
					new Token(TokenType.Operator, Operator.LogicalAnd, "&&", 0, 0),
					new Token(TokenType.Operator, Operator.BitwiseAnd, "&", 0, 2) }),
				new Test("& &&", new []{
					new Token(TokenType.Operator, Operator.BitwiseAnd, "&", 0, 0),
					new Token(TokenType.Operator, Operator.LogicalAnd, "&&", 0, 2) }),
				new Test("+-*/=&&&!~()[]{}%<>!=|||", new []{
					new Token(TokenType.Operator, Operator.Add, "+", 0, 0),
					new Token(TokenType.Operator, Operator.Subtract, "-", 0, 1),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 2),
					new Token(TokenType.Operator, Operator.Divide, "/", 0, 3),
					new Token(TokenType.Operator, Operator.Assignment, "=", 0, 4),
					new Token(TokenType.Operator, Operator.LogicalAnd,"&&", 0, 5),
					new Token(TokenType.Operator, Operator.BitwiseAnd, "&", 0, 7),
					new Token(TokenType.Operator, Operator.LogicalNot, "!", 0, 8),
					new Token(TokenType.Operator, Operator.BitwiseNot, "~", 0, 9),
					new Token(TokenType.Operator, Operator.OpenParenthesis, "(", 0, 10),
					new Token(TokenType.Operator, Operator.CloseParenthesis, ")", 0, 11),
					new Token(TokenType.Operator, Operator.OpenSquareBracket, "[", 0, 12),
					new Token(TokenType.Operator, Operator.CloseSquareBracket, "]", 0, 13),
					new Token(TokenType.Operator, Operator.OpenCurlyBrace, "{", 0, 14),
					new Token(TokenType.Operator, Operator.CloseCurlyBrace, "}", 0, 15),
					new Token(TokenType.Operator, Operator.Remainder, "%", 0, 16),
					new Token(TokenType.Operator, Operator.LessTest, "<", 0, 17),
					new Token(TokenType.Operator, Operator.MoreTest, ">", 0, 18),
					new Token(TokenType.Operator, Operator.NotEqualityTest, "!=", 0, 19),
					new Token(TokenType.Operator, Operator.LogicalOr, "||", 0, 21),
					new Token(TokenType.Operator, Operator.BitwiseOr, "|", 0, 23),}),
				new Test("1234567890", new []{ new Token(TokenType.Int, 1234567890, "1234567890", 0, 0) }),
				new Test("0xff", new []{ new Token(TokenType.Int, 255, "0xff", 0, 0) }),
				new Test("001", new []{ new Token(TokenType.Int, 1, "001", 0, 0) }),
				new Test("99999999999999999999", new []{ new Token(TokenType.Int, null, "99999999999999999999", 0, 0) { Message = "OverflowException" } }),
				new Test("0xffffffff", new []{ new Token(TokenType.Int, -1, "0xffffffff", 0, 0) }),
				new Test("0xfffffffff", new []{ new Token(TokenType.Int, null, "0xfffffffff", 0, 0) { Message = "OverflowException" } }),
				new Test("0XFF", new []{ new Token(TokenType.Int, 255, "0XFF", 0, 0) }),
				new Test("1.0", new []{ new Token(TokenType.Float, 1.0f, "1.0", 0, 0) }),
				new Test("1.", new []{ new Token(TokenType.Float, null, "1.", 0, 0) }),
				new Test("1234567890.1234567890",
					new []{ new Token(TokenType.Float,
						1234567890.1234567890f,
						"1234567890.1234567890", 0, 0) }),
				new Test("3.4E+39", new []{ new Token(TokenType.Float, null, "3.4E+39", 0, 0) { Message = "OverflowException" } }),
				new Test("1.5E-46", new []{ new Token(TokenType.Float, 0f, "1.5E-46", 0, 0) }),
				new Test("1.0e1", new []{ new Token(TokenType.Float, 10.0f, "1.0e1", 0, 0) }),
				new Test("1.0e+1", new []{ new Token(TokenType.Float, 10.0f, "1.0e+1", 0, 0) }),
				new Test("1.0e-1", new []{ new Token(TokenType.Float, 0.1f, "1.0e-1", 0, 0) }),
				new Test("1.0e1.", new []{ new Token(TokenType.Float, null, "1.0e1.", 0, 0) }),
				new Test("1.0e+1.", new []{ new Token(TokenType.Float, null, "1.0e+1.", 0, 0) }),
				new Test("1.0e-1.", new []{ new Token(TokenType.Float, null, "1.0e-1.", 0, 0) }),
				new Test("'a'", new []{ new Token(TokenType.Char, 'a', "'a'", 0, 0) }),
				new Test("'ab'", new []{ new Token(TokenType.Char, null, "'ab", 0, 0) }),
				new Test("\"a\"", new []{ new Token(TokenType.String, "a", "\"a\"", 0, 0) }),
				new Test("\"`1234567890-=~!@#$%^&*()_+qwertyuiop[]QWERTYUIOP{}asdfghjkl;'ASDFGHJKL:|\\zxcvbnm,./ZXCVBNM<>?\"",
					new []{ new Token(TokenType.String,
						"`1234567890-=~!@#$%^&*()_+qwertyuiop[]QWERTYUIOP{}asdfghjkl;'ASDFGHJKL:|\\zxcvbnm,./ZXCVBNM<>?",
						"\"`1234567890-=~!@#$%^&*()_+qwertyuiop[]QWERTYUIOP{}asdfghjkl;'ASDFGHJKL:|\\zxcvbnm,./ZXCVBNM<>?\"", 0, 0) }),
				new Test("a a", new []{
					new Token(TokenType.Identifier, "a", "a", 0, 0),
					new Token(TokenType.Identifier, "a", "a", 0, 2) }),
				new Test("123*123", new []{
					new Token(TokenType.Int, 123, "123", 0, 0),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 3),
					new Token(TokenType.Int, 123, "123", 0, 4) }),
				new Test("123 * 123", new []{
					new Token(TokenType.Int, 123, "123", 0, 0),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 4),
					new Token(TokenType.Int, 123, "123", 0, 6) }),
				new Test("123.0*123.0", new []{
					new Token(TokenType.Float, 123.0f, "123.0", 0, 0),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 5),
					new Token(TokenType.Float, 123.0f, "123.0", 0, 6) }),
				new Test("123.0 * 123.0", new []{
					new Token(TokenType.Float, 123.0f, "123.0", 0, 0),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 6),
					new Token(TokenType.Float, 123.0f, "123.0", 0, 8) }),
				new Test("a.", new []{
					new Token(TokenType.Identifier, "a", "a", 0, 0),
					new Token(TokenType.Operator, Operator.Dot, ".", 0, 1) }),
				new Test("a*b", new []{
					new Token(TokenType.Identifier, "a", "a", 0, 0),
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 1),
					new Token(TokenType.Identifier, "b", "b", 0, 2) }),
				new Test("-1.0e-1", new []{
					new Token(TokenType.Operator, Operator.Subtract, "-", 0, 0),
					new Token(TokenType.Float, 0.1f, "1.0e-1", 0, 1)}),
				new Test("-1", new []{
					new Token(TokenType.Operator, Operator.Subtract, "-", 0, 0),
					new Token(TokenType.Int, 1, "1", 0, 1)}),
				new Test("void", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.Void, "void", 0, 0) }),
				new Test("int", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.Int, "int", 0, 0) }),
				new Test("float", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.Float, "float", 0, 0) }),
				new Test("char", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.Char, "char", 0, 0) }),
				new Test("string", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.String, "string", 0, 0) }),
				new Test("class", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.Class, "class", 0, 0) }),
				new Test("if", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.If, "if", 0, 0) }),
				new Test("for", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.For, "for", 0, 0) }),
				new Test("while", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.While, "while", 0, 0) }),
				new Test(" while", new []{ new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.While, "while", 0, 1) }),
				new Test("*while+", new []{
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 0),
					new Token(TokenType.Keyword, Tokenizer.Tokenizer.Keyword.While, "while", 0, 1),
					new Token(TokenType.Operator, Operator.Add, "+", 0, 6),}),
				new Test("qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890", new []{
					new Token(TokenType.Identifier, "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890",
					"qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890", 0, 0) }),
				new Test("*a+", new []{
					new Token(TokenType.Operator, Operator.Multiply, "*", 0, 0),
					new Token(TokenType.Identifier, "a", "a", 0, 1),
					new Token(TokenType.Operator, Operator.Add, "+", 0, 2),}),
			};
		}

		private struct Test
		{
			public struct Result
			{
				public bool IsDone;
				public List<Token> Tokens;
			}

			public readonly string Input;
			public readonly Token[] Output;

			public Test(string input, Token[] output)
			{
				Input = input; Output = output;
			}

			public Result Execute()
			{
				var tokens = GetTokens();
				bool equals = true;
				equals &= Output.Length == tokens.Count;
				if (equals) {
					for (int i = 0; i < Output.Length; i++) {
						equals &= Token.Equals(Output[i], tokens[i]);
					}
				}
				return new Result { IsDone = equals, Tokens = tokens };
			}

			private List<Token> GetTokens()
			{
				lexer.UpdateStream(GenerateStreamFromString(Input));
				var tokens = new List<Token>();
				Token token;
				while (null != (token = lexer.Next())) {
					tokens.Add(token);
				}
				return tokens;
			}
		}

		private static MemoryStream GenerateStreamFromString(string value)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
		}
	}
}
