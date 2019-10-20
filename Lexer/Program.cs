using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lexer
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
		public Type type = Type.Undefined;
		public string strValue = "";
		public int rowIndex = -1;
		public int colIndex = -1;
		public bool isError = false;

		public override string ToString() => string.Format(
			"r:{0,3} c:{1,3} {2,16} {3}",
			rowIndex, colIndex, !isError ? type.ToString() : "Error-" + type.ToString(), strValue);
	}

	public class Lexer
	{
		private string fileAsString;
		public string FileAsString
		{
			get => fileAsString;
			set
			{
				fileAsString = value;
				Reset();
			}
		}

		private readonly Stack<int> rowSizes;
		private readonly Dictionary<string, int> keywords;

		private int charIndex = 0;
		private int rowIndex = 0;
		private int colIndex = 0;
		private char symbol;
		private bool isError;

		private enum State : int
		{
			Undefined = -1,
			Start = 0,
			Solidus = 1,
			DoubleSolidus = 2,
			QuotationMark = 3,
			Ampersand = 4,
			Minus = 5,
			Equals = 6,
			Word = 7,
			Int = 8,
			Float = 9,
			Char = 10
		}
		private State activeState = State.Start;
		private readonly int statesCount;
		private readonly int alphabetSize = 128;

		private delegate void Action();
		private Action[][] actions;
		private Token token;
		private bool tokenCompleted;

		public Lexer()
		{
			rowSizes = new Stack<int>();
			keywords = new Dictionary<string, int>()
			{
				["void"] = 0,
				["int"] = 1,
				["float"] = 2,
				["char"] = 3,
				["if"] = 4,
				["while"] = 5,
				["struct"] = 6
			};
			statesCount = Enum.GetValues(typeof(State)).Length;
			actions = new Action[statesCount][];
			for (int i = 0; i < statesCount; i++) {
				actions[i] = new Action[alphabetSize];
				Array.Fill(actions[i], ActionSkip);
			}
			BuildStartLevel();
			BuildSolidusLevel();
			BuildDoubleSolidusLevel();
			BuildQuotationMarkLevel();
			BuildAmpersandLevel();
			BuildEqualsLevel();
			BuildMinusLevel();
			BuildCharLevel();
			BuildWordLevel();
			BuildIntLevel();
			BuildFloatLevel();
		}

		public Token Next()
		{
			if (isError) {
				return null;
			}
			token = new Token();
			tokenCompleted = false;
			while (!tokenCompleted && charIndex < fileAsString.Length) {
				symbol = fileAsString[charIndex];
				actions[(int)activeState][symbol]();
				charIndex++; colIndex++;
			}
			isError = token.isError;
			return token.type != Token.Type.Undefined ? token : null;
		}

		public void Reset()
		{
			charIndex = 0;
			rowIndex = 0;
			colIndex = 0;
			isError = false;
			tokenCompleted = false;
			activeState = State.Start;
		}

		private void BuildStartLevel()
		{
			var startActions = actions[(int)State.Start];
			startActions[0x0a/* \n */] = ActrionLineFeed;
			startActions[0x21 /* ! */] = ActionOneSymbolOperator;
			startActions[0x22 /* " */] = ActionSetQuotationMarkState;
			Array.Fill(startActions, ActionErrorSymbol, 0x23, 3); // # $ %
			startActions[0x26 /* & */] = ActionSetAmpersandState;
			startActions[0x27 /* ' */] = ActionSetCharState;
			Array.Fill(startActions, ActionOneSymbolOperator, 0x28, 4); // ( ) * +
			startActions[0x2c /* , */] = ActionErrorSymbol;
			startActions[0x2d /* - */] = ActionSetMinusState;
			startActions[0x2e /* . */] = ActionOneSymbolOperator;
			startActions[0x2f /* / */] = ActionSetSolidusState;
			Array.Fill(startActions, ActionSetIntState, 0x30, 10); // 0123456789
			startActions[0x3a /* : */] = ActionErrorSymbol;
			Array.Fill(startActions, ActionOneSymbolOperator, 0x3b, 2); // ; <
			startActions[0x3d /* = */] = ActionSetEqualsState;
			Array.Fill(startActions, ActionErrorSymbol, 0x3e, 3); // > ? @
			Array.Fill(startActions, ActionSetWordState, 0x41, 26); // A B C ...
			startActions[0x5b /* [ */] = ActionOneSymbolOperator;
			startActions[0x5c /* \ */] = ActionErrorSymbol;
			startActions[0x5d /* ] */] = ActionOneSymbolOperator;
			Array.Fill(startActions, ActionErrorSymbol, 0x5e, 3); // ^ _ `
			Array.Fill(startActions, ActionSetWordState, 0x61, 26); // A B C ...
			startActions[0x7b /* { */] = ActionOneSymbolOperator;
			startActions[0x7c /* | */] = ActionErrorSymbol;
			Array.Fill(startActions, ActionOneSymbolOperator, 0x7d, 2); // } ~
		}

		private void BuildSolidusLevel()
		{
			var solidusActions = actions[(int)State.Solidus];
			var action = new Action(ActionOneSymbolOperator);
			action += ActionBack;
			Array.Fill(solidusActions, action);
			solidusActions[0x2f /* / */] = ActionSetDoubleSolidusState;
		}

		private void BuildDoubleSolidusLevel()
		{
			var doubleSolidusActions = actions[(int)State.DoubleSolidus];
			Array.Fill(doubleSolidusActions, ActionAddCharToToken);
			doubleSolidusActions[0x0a /* \n */] = ActionSetStartState;
			doubleSolidusActions[0x0a /* \n */] += ActionClear;
		}

		private void BuildQuotationMarkLevel()
		{
			var quotationMarkActions = actions[(int)State.QuotationMark];
			Array.Fill(quotationMarkActions, ActionAddCharToToken);
			quotationMarkActions[0x00 /* \0 */] = ActionErrorSymbol;
			quotationMarkActions[0x22 /* " */] = ActionAddCharToToken;
			quotationMarkActions[0x22 /* " */] += ActionTokenCompleted;
		}

		private void BuildCharLevel()
		{
			var quotationMarkActions = actions[(int)State.Char];
			Array.Fill(quotationMarkActions, () => {
				ActionAddCharToToken();
				if (token.strValue.Length > 2) {
					ActionErrorSymbol();
				}
			});
			quotationMarkActions[0x00 /* \0 */] = ActionErrorSymbol;
			quotationMarkActions[0x27 /* ' */] = ActionAddCharToToken;
			quotationMarkActions[0x27 /* ' */] += ActionTokenCompleted;
		}

		private void BuildAmpersandLevel() => BuildDoubleOperatorLevel(State.Ampersand, 0x26 /* & */);
		private void BuildEqualsLevel() => BuildDoubleOperatorLevel(State.Equals, 0x3d /* = */);
		private void BuildMinusLevel() => BuildDoubleOperatorLevel(State.Minus, 0x3e /* > */);

		private void BuildDoubleOperatorLevel(State level, int secondChar)
		{
			var operatorActions = actions[(int)level];
			var action = new Action(ActionTokenCompleted);
			action += ActionBack;
			Array.Fill(operatorActions, action);
			operatorActions[secondChar] = ActionAddCharToToken;
			operatorActions[secondChar] += ActionTokenCompleted;
		}

		private static bool CheckDigit(int symbol) => (symbol >= 0x30 && symbol <= 0x39);
		private static bool CheckLatin(int symbol) => (symbol >= 0x41 && symbol <= 0x5a) || (symbol >= 0x61 && symbol <= 0x7a);

		private void BuildWordLevel()
		{
			var wordActions = actions[(int)State.Word];
			for (int i = 0; i < wordActions.Length; i++) {
				if (CheckDigit(i) || CheckLatin(i)) {
					wordActions[i] = ActionAddCharToToken;
				} else {
					wordActions[i] = ActionBack;
					wordActions[i] += ActionTokenCompleted;
					wordActions[i] += () => {
						token.type = keywords.ContainsKey(token.strValue) ? Token.Type.Keyword : Token.Type.Word;
					};
				}
			}
		}

		private void BuildIntLevel()
		{
			var intActions = actions[(int)State.Int];
			var action = new Action(ActionBack);
			action += ActionTokenCompleted;
			for (int i = 0; i < intActions.Length; i++) {
				if (CheckDigit(i)) {
					intActions[i] = ActionAddCharToToken;
				} else if (CheckLatin(i)) {
					intActions[i] = ActionErrorSymbol;
				} else {
					intActions[i] = action;
				}
			}
			Array.Fill(intActions, ActionErrorSymbol, 0x22, 3); // " % #
			Array.Fill(intActions, ActionErrorSymbol, 0x27, 2); // ' (
			intActions[0x2c /* , */] = ActionErrorSymbol;
			intActions[0x2e /* . */] = ActionSetFloatState;
			intActions[0x40 /* @ */] = ActionErrorSymbol;
			intActions[0x5c /* \ */] = ActionErrorSymbol;
			Array.Fill(intActions, ActionErrorSymbol, 0x5f, 2); // _ \
		}

		private void BuildFloatLevel()
		{
			CopyLevelOfFiniteStateMachines((int)State.Int, (int)State.Float);
			actions[(int)State.Float][0x2e] = ActionErrorSymbol;
		}

		private void CopyLevelOfFiniteStateMachines(int srcLvlIndex, int dstLvlIndex)
		{
			for (int i = 0; i < alphabetSize; i++) {
				actions[dstLvlIndex][i] = actions[srcLvlIndex][i];
			}
		}

		private void ActionSkip() { }

		private void ActionClear() => token.strValue = "";

		private void ActionOneSymbolOperator()
		{
			token.type = Token.Type.Operator;
			token.strValue = symbol.ToString();
			UpdateTokenLocation();
			ActionTokenCompleted();
		}

		private void ActionSetStartState() => activeState = State.Start;
		private void ActionSetWordState() => SetState(State.Word, Token.Type.Word, updateLocation: true);
		private void ActionSetIntState() => SetState(State.Int, Token.Type.Int, updateLocation: true);
		private void ActionSetCharState() => SetState(State.Char, Token.Type.Char, updateLocation: true);
		private void ActionSetFloatState() => SetState(State.Float, Token.Type.Float, updateLocation: false);
		private void ActionSetMinusState() => SetState(State.Minus, Token.Type.Operator, updateLocation: true);
		private void ActionSetEqualsState() => SetState(State.Equals, Token.Type.Operator, updateLocation: true);
		private void ActionSetSolidusState() => SetState(State.Solidus, Token.Type.Operator, updateLocation: true);
		private void ActionSetAmpersandState() => SetState(State.Ampersand, Token.Type.Operator, updateLocation: true);
		private void ActionSetQuotationMarkState() => SetState(State.QuotationMark, Token.Type.ConstStr, updateLocation: true);
		private void ActionSetDoubleSolidusState() => SetState(State.DoubleSolidus, Token.Type.Undefined, updateLocation: true);
		private void SetState(State state, Token.Type type, bool updateLocation)
		{
			activeState = state;
			token.type = type;
			token.strValue += symbol.ToString();
			if (updateLocation) {
				UpdateTokenLocation();
			}
		}

		private void ActionErrorSymbol()
		{
			token.isError = true;
			tokenCompleted = true;
			ActionAddCharToToken();
		}

		private void ActionBack()
		{
			charIndex--;
			colIndex--;
			if (colIndex < 0) {
				colIndex = rowSizes.Pop() - 1;
				rowIndex--;
			}
		}

		private void ActionAddCharToToken() => token.strValue += symbol;

		private void ActionTokenCompleted()
		{
			tokenCompleted = true;
			ActionSetStartState();
		}

		private void ActrionLineFeed()
		{
			rowIndex++;
			rowSizes.Push(colIndex + 1);
			colIndex = -1;
		}

		private void UpdateTokenLocation()
		{
			token.rowIndex = rowIndex;
			token.colIndex = colIndex;
		}
	}

	class Program
	{
		private static Lexer lexer;

		static void Main(string[] args)
		{
			lexer = new Lexer();
			Test[] tests = new Test[]
			{
				new Test("\0", ""),
				new Test(" \0",   ""),
				new Test("a\0",   "r:  0 c:  0             Word a\n"),
				new Test("int\0", "r:  0 c:  0          Keyword int\n"),
				new Test("1\0",   "r:  0 c:  0              Int 1\n"),
				new Test("1.0\0", "r:  0 c:  0            Float 1.0\n"),
				new Test("\"const string\"\0",    "r:  0 c:  0         ConstStr \"const string\"\n"),
				new Test("123456789\0",           "r:  0 c:  0              Int 123456789\n"),
				new Test("123456789.123456789\0", "r:  0 c:  0            Float 123456789.123456789\n"),
				new Test("*\0", "r:  0 c:  0         Operator *\n"),
				new Test("->\0", "r:  0 c:  0         Operator ->\n"),
				new Test("&\0", "r:  0 c:  0         Operator &\n"),
				new Test("&&&\0", 
					"r:  0 c:  0         Operator &&\n" +
					"r:  0 c:  2         Operator &\n"),
				new Test("& &&\0", 
					"r:  0 c:  0         Operator &\n" +
					"r:  0 c:  2         Operator &&\n"),
				new Test("123*123\0",
					"r:  0 c:  0              Int 123\n" +
					"r:  0 c:  3         Operator *\n" +
					"r:  0 c:  4              Int 123\n"),
				new Test("123 * 123\0", 
					"r:  0 c:  0              Int 123\n" +
					"r:  0 c:  4         Operator *\n" +
					"r:  0 c:  6              Int 123\n"),
				new Test("float *\0", 
					"r:  0 c:  0          Keyword float\n" +
					"r:  0 c:  6         Operator *\n"),
				new Test("float*\0", 
					"r:  0 c:  0          Keyword float\n" +
					"r:  0 c:  5         Operator *\n"),
				new Test("word123\0", "r:  0 c:  0             Word word123\n"),
				new Test("123word\0", "r:  0 c:  0        Error-Int 123w\n"),
				new Test("word 123\0",
					"r:  0 c:  0             Word word\n" +
					"r:  0 c:  5              Int 123\n"),
				new Test("*+\0", 
					"r:  0 c:  0         Operator *\n" +
					"r:  0 c:  1         Operator +\n"),
				new Test("\"~!@#$%^&*()_+'':;<>//qwertyuiopasdfghjklzxcvbnm1234567890\"\0", 
					"r:  0 c:  0         ConstStr \"~!@#$%^&*()_+'':;<>//qwertyuiopasdfghjklzxcvbnm1234567890\"\n"),
				new Test("//~!@#$%^&*()_+'':;<>//qwertyuiopasdfghjklzxcvbnm1234567890\0", ""),
				new Test("word123456789qwertyuiopasdfghjklzxcvbnm\0", "r:  0 c:  0             Word word123456789qwertyuiopasdfghjklzxcvbnm\n"),
			};
			for (int i = 0; i < tests.Length; i++) {
				bool result = tests[i].Execute();
				if (result) {
					Console.WriteLine(string.Format("Test {0, 2} done", i));
				} else {
					Console.WriteLine(string.Format("Test {0, 2} fail", i));
					Console.WriteLine("\tInput\n" + tests[i].Input);
					Console.WriteLine("\tOutput\n" + tests[i].Output);
					Console.WriteLine("\tResult\n" + tests[i].GetTokens());
				}
			}
		}

		private struct Test
		{
			public readonly string Input;
			public readonly string Output;

			public Test(string input, string output)
			{
				Input = input; Output = output;
			}

			public bool Execute()
			{
				return GetTokens() == Output;
			}

			public string GetTokens()
			{
				string result = "";
				lexer.FileAsString = Input;
				Token token;
				while (null != (token = lexer.Next())) {
					result += token.ToString() + '\n';
				}
				return result;
			}
		}
	}
}
