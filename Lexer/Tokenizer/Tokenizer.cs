using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace Compiler.Tokenizer
{
	public class TokenizerException : InvalidOperationException
	{
		public TokenizerException() { }

		public TokenizerException(string message) : base(message) { }

		public TokenizerException(string message, Exception inner) : base(message, inner) { }
	}

	public class Tokenizer : IDisposable
	{
		public enum Keyword
		{
			Type	= 512,
			Void	= 1 | Type,
			Int		= 2 | Type,
			Float	= 3 | Type,
			Char	= 4 | Type,
			String	= 5 | Type,
			Class	= 6,
			Logic	= 1024,
			If		= 7  | Logic,
			For		= 8  | Logic,
			While	= 9  | Logic,
			Return	= 10 | Logic,
			Break	= 11 | Logic,
			Modifier= 2048,
			Public	= 12 | Modifier,
			Static  = 13 | Modifier,
			New		= 14,
			Bool	= 4096,
			True	= 15 | Bool,
			False	= 16 | Bool,
			Null	= 17
		}

		public enum Operator
		{
			Assignment			= 1,
			AdditiveOperator		= 128,
			UnaryOperator			= 256,
			Add					= 2 | AdditiveOperator | UnaryOperator,
			Subtract			= 3 | AdditiveOperator | UnaryOperator,
			MultiplicativeOperator	= 512,
			Multiply			= 4 | MultiplicativeOperator,
			Divide				= 5 | MultiplicativeOperator,
			Remainder			= 6 | MultiplicativeOperator,
			LogicalNot			= 7 | UnaryOperator,
			BitwiseNot			= 8 | UnaryOperator,
			LogicalAnd			= 9,
			BitwiseAnd			= 10,
			LogicalOr			= 11,
			BitwiseOr			= 12,
			EqualityOperator		= 1024,
			EqualityTest		= 13 | EqualityOperator,
			NotEqualityTest		= 14 | EqualityOperator,
			RelationalOperator		= 2048,
			LessTest			= 15 | RelationalOperator,
			MoreTest			= 16 | RelationalOperator,
			Primary					= 4096,
			OpenParenthesis		= 17 | Primary,
			CloseParenthesis	= 18 | Primary,
			OpenCurlyBrace		= 19,
			CloseCurlyBrace		= 20,
			OpenSquareBracket	= 21 | Primary,
			CloseSquareBracket	= 22 | Primary,
			Dot					= 23 | Primary,
			Comma				= 24,
			SemiColon			= 25
		}

		private static readonly Dictionary<string, Keyword> keywords;

		static Tokenizer() {
			keywords = new Dictionary<string, Keyword>() {
				["void"]	= Keyword.Void,
				["int"]		= Keyword.Int,
				["float"]	= Keyword.Float,
				["char"]	= Keyword.Char,
				["string"]	= Keyword.String,
				["bool"]	= Keyword.Bool,
				["true"]	= Keyword.True,
				["false"]	= Keyword.False,
				["if"]		= Keyword.If,
				["for"]		= Keyword.For,
				["while"]	= Keyword.While,
				["class"]	= Keyword.Class,
				["return"]	= Keyword.Return,
				["break"]	= Keyword.Break,
				["public"]	= Keyword.Public,
				["new"]		= Keyword.New,
				["null"]	= Keyword.Null
			};
		}

		public static bool IsKeyword(string word) => keywords.ContainsKey(word);
		public static Keyword KeywordToEnum(string word) => keywords[word];

		public static bool IsDigit(int symbol) => (symbol >= 0x30 && symbol <= 0x39);
		public static bool IsLatin(int symbol) => (symbol >= 0x41 && symbol <= 0x5a) || (symbol >= 0x61 && symbol <= 0x7a);

		private enum State : uint
		{
			Start,
			Division,
			Comment,
			ConstString,
			Ampersand,
			Pipe,
			Minus,
			Equals,
			NotEquals,
			Word,
			Int,           // [0-9]+
			Int0X,         // (0[xX])[0-9]*
			Float,         // ([0-9]+.)[0-9]*
			FloatExp,      // ([0-9]+.[0-9]+)[eE][0-9]*
			FloatExpSign,  // ([0-9]+.[0-9]+)[eE][-+][0-9]*
			Char
		}

		private readonly StreamState stream;
		private readonly Action[][] actions;
		private readonly int statesCount;
		private readonly int alphabetSize = 128;

		private State activeState = State.Start;
		private Token token;
		private bool tokenCompleted;
		private bool isError;

		private Tokenizer()
		{
			statesCount = Enum.GetValues(typeof(State)).Length;
			actions = new Action[statesCount][];
			for (int i = 0; i < statesCount; i++) {
				actions[i] = new Action[alphabetSize];
				Array.Fill(actions[i], ActionSkip);
			}
			// Start Level
			{
				void ActionSetWordState() => SetState(State.Word, Token.Type.Identifier, updateLocation: true);
				void ActionSetIntState() => SetState(State.Int, Token.Type.Int, updateLocation: true);

				var startActions = actions[(int)State.Start];
				startActions[0x21 /* ! */] = () => SetState(State.NotEquals, Token.Type.Operator, updateLocation: true);
				startActions[0x22 /* " */] = () => SetState(State.ConstString, Token.Type.String, updateLocation: true);
				Array.Fill(startActions, ActionErrorSymbol, 0x23, 2); // # $
				startActions[0x25 /* % */] = () => ActionOneSymbolOperator(Operator.Remainder);
				startActions[0x26 /* & */] = () => SetState(State.Ampersand, Token.Type.Operator, updateLocation: true);
				startActions[0x27 /* ' */] = () => SetState(State.Char, Token.Type.Char, updateLocation: true);
				startActions[0x28 /* ( */] = () => ActionOneSymbolOperator(Operator.OpenParenthesis);
				startActions[0x29 /* ) */] = () => ActionOneSymbolOperator(Operator.CloseParenthesis);
				startActions[0x2a /* * */] = () => ActionOneSymbolOperator(Operator.Multiply);
				startActions[0x2b /* + */] = () => ActionOneSymbolOperator(Operator.Add);
				startActions[0x2c /* , */] = () => ActionOneSymbolOperator(Operator.Comma);
				startActions[0x2d /* - */] = () => ActionOneSymbolOperator(Operator.Subtract);
				startActions[0x2e /* . */] = () => ActionOneSymbolOperator(Operator.Dot);
				startActions[0x2f /* / */] = () => SetState(State.Division, Token.Type.Operator, updateLocation: true);
				Array.Fill(startActions, ActionSetIntState, 0x30, 10); // 0 1 2 ...
				startActions[0x3a /* : */] = ActionErrorSymbol;
				startActions[0x3b /* ; */] = () => ActionOneSymbolOperator(Operator.SemiColon);
				startActions[0x3c /* < */] = () => ActionOneSymbolOperator(Operator.LessTest);
				startActions[0x3d /* = */] = () => SetState(State.Equals, Token.Type.Operator, updateLocation: true);
				startActions[0x3e /* > */] = () => ActionOneSymbolOperator(Operator.MoreTest);
				Array.Fill(startActions, ActionErrorSymbol, 0x3f, 2); // ? @
				Array.Fill(startActions, ActionSetWordState, 0x41, 26); // A B C ...
				startActions[0x5b /* [ */] = () => ActionOneSymbolOperator(Operator.OpenSquareBracket);
				startActions[0x5c /* \ */] = ActionErrorSymbol;
				startActions[0x5d /* ] */] = () => ActionOneSymbolOperator(Operator.CloseSquareBracket);
				Array.Fill(startActions, ActionErrorSymbol, 0x5e, 3); // ^ _ `
				Array.Fill(startActions, ActionSetWordState, 0x61, 26); // a b c ...
				startActions[0x7b /* { */] = () => ActionOneSymbolOperator(Operator.OpenCurlyBrace);
				startActions[0x7c /* | */] = () => SetState(State.Pipe, Token.Type.Operator, updateLocation: true);
				startActions[0x7d /* } */] = () => ActionOneSymbolOperator(Operator.CloseCurlyBrace);
				startActions[0x7e /* } */] = () => ActionOneSymbolOperator(Operator.BitwiseNot);
			}
			// Division Level
			{
				var divisionActions = actions[(int)State.Division];
				var action = new Action(ActionNoRequestNextSymbol);
				action += () => {
					token.TypeId = Token.Type.Operator;
					token.Value = Operator.Divide;
					ActionTokenCompleted();
				};
				Array.Fill(divisionActions, action);
				divisionActions[0x2f /* / */] = () => SetState(State.Comment, Token.Type.Undefined, updateLocation: false);
			}
			// Comment Level
			{
				var commentActions = actions[(int)State.Comment];
				Array.Fill(commentActions, ActionAddCharToToken);
				commentActions[0x0a /* \n */] = ActionSetStartState;
				commentActions[0x0a /* \n */] += ActionClear;
			}
			// Const String Level
			{
				var constStringActions = actions[(int)State.ConstString];
				Array.Fill(constStringActions, ActionAddCharToToken);
				constStringActions[0x00 /* \0 */] = ActionErrorSymbol;
				constStringActions[0x22 /* " */] = ActionAddCharToToken;
				constStringActions[0x22 /* " */] += ActionTokenCompleted;
			}
			// Char Level
			{
				var quotationMarkActions = actions[(int)State.Char];
				Array.Fill(quotationMarkActions, () => {
					ActionAddCharToToken();
					if (token.RawValue.Length > 2) {
						isError = true;
						tokenCompleted = true;
					}
				});
				quotationMarkActions[0x00 /* \0 */] = ActionErrorSymbol;
				quotationMarkActions[0x27 /* ' */] = ActionAddCharToToken;
				quotationMarkActions[0x27 /* ' */] += ActionTokenCompleted;
			}
			{
				void BuildDoubleOperatorLevel(State level, int secondChar, Operator op1, Operator op2)
				{
					var operatorActions = actions[(int)level];
					var action = new Action(ActionTokenCompleted);
					action += ActionNoRequestNextSymbol;
					action += () => { token.Value = op1; };
					Array.Fill(operatorActions, action);
					operatorActions[secondChar] = ActionAddCharToToken;
					operatorActions[secondChar] += ActionTokenCompleted;
					operatorActions[secondChar] += () => { token.Value = op2; };
				}
				BuildDoubleOperatorLevel(State.Ampersand, secondChar: 0x26 /* & */, Operator.BitwiseAnd, Operator.LogicalAnd); // &&
				BuildDoubleOperatorLevel(State.Pipe, secondChar: 0x7c /* | */, Operator.BitwiseOr, Operator.LogicalOr); // ||
				BuildDoubleOperatorLevel(State.Equals, secondChar: 0x3d /* = */, Operator.Assignment, Operator.EqualityTest); // ==
				BuildDoubleOperatorLevel(State.NotEquals, secondChar: 0x3d /* = */, Operator.LogicalNot, Operator.NotEqualityTest); // !=
			}
			// Int Level
			{
				var intActions = actions[(int)State.Int];

				void CheckIntHexState()
				{
					if (token.RawValue.Length == 1 && token.RawValue[0] == '0') {
						SetState(State.Int0X, Token.Type.Int, updateLocation: false);
					}
				}

				void ActionCompleted()
				{
					ActionNoRequestNextSymbol();
					ActionTokenCompleted();
				}
				Array.Fill(intActions, ActionCompleted, 0x00, alphabetSize);

				Array.Fill(intActions, ActionAddCharToToken, 0x30, 10); // 0 1 2 ...
				Array.Fill(intActions, ActionErrorSymbol, 0x41, 26); // A B C ...
				Array.Fill(intActions, ActionErrorSymbol, 0x61, 26); // a b c ...
				Array.Fill(intActions, ActionErrorSymbol, 0x21, 4); // ! " # $
				Array.Fill(intActions, ActionErrorSymbol, 0x27, 2); // ' (
				intActions[0x2c /* , */] = ActionErrorSymbol;
				intActions[0x2e /* . */] = () => SetState(State.Float, Token.Type.Float, updateLocation: false);
				intActions[0x3a /* : */] = ActionErrorSymbol;
				Array.Fill(intActions, ActionErrorSymbol, 0x3e, 3); // > ? @
				intActions[0x58 /* X */] = CheckIntHexState;
				Array.Fill(intActions, ActionErrorSymbol, 0x5b, 2); // [ \
				Array.Fill(intActions, ActionErrorSymbol, 0x5e, 3); // ^ _ `
				intActions[0x78 /* x */] = CheckIntHexState;
				intActions[0x7b /* { */] = ActionErrorSymbol;
				intActions[0x7e /* ~ */] = ActionErrorSymbol;
			}
			// Int0X
			{
				Array.Copy(actions[(int)State.Int], actions[(int)State.Int0X], alphabetSize);
				var intActions = actions[(int)State.Int0X];
				intActions[0x2e /* . */] = ActionErrorSymbol;
				intActions[0x58 /* X */] = ActionErrorSymbol;
				intActions[0x78 /* x */] = ActionErrorSymbol;
				Array.Fill(intActions, ActionAddCharToToken, 0x41, 6); // A B C D E F
				Array.Fill(intActions, ActionAddCharToToken, 0x61, 6); // a b c d e f
			}
			// Float Level
			{
				void CheckExponent()
				{
					var str = token.RawValue;
					if (str[str.Length - 1] != '.') {
						SetState(State.FloatExp, Token.Type.Float, updateLocation: false);
					} else {
						ActionErrorSymbol();
					}
				}
				Array.Copy(actions[(int)State.Int], actions[(int)State.Float], alphabetSize);
				var floatActions = actions[(int)State.Float];
				floatActions[0x2e /* . */] = ActionErrorSymbol;
				floatActions[0x58 /* X */] = ActionErrorSymbol;
				floatActions[0x78 /* x */] = ActionErrorSymbol;
				floatActions[0x65 /* e */] = CheckExponent;
				floatActions[0x45 /* E */] = CheckExponent;
			}
			// FloatExp Level
			{
				void CheckSign()
				{
					var str = token.RawValue;
					if (str[str.Length - 1] == 'e' || str[str.Length - 1] == 'E') {
						SetState(State.FloatExpSign, Token.Type.Float, updateLocation: false);
					} else {
						ActionErrorSymbol();
					}
				}
				Array.Copy(actions[(int)State.Float], actions[(int)State.FloatExp], alphabetSize);
				var floatActions = actions[(int)State.FloatExp];
				floatActions[0x65 /* E */] = ActionErrorSymbol;
				floatActions[0x45 /* e */] = ActionErrorSymbol;
				floatActions[0x2d /* - */] = CheckSign;
				floatActions[0x2b /* + */] = CheckSign;
			}
			// FloatExpSign Level
			{
				Array.Copy(actions[(int)State.FloatExp], actions[(int)State.FloatExpSign], alphabetSize);
				var floatActions = actions[(int)State.FloatExpSign];
				floatActions[0x2d /* - */] = ActionErrorSymbol;
				floatActions[0x2b /* + */] = ActionErrorSymbol;
			}
			// Word - Keyword Level
			{
				var wordActions = actions[(int)State.Word];
				for (int i = 0; i < wordActions.Length; i++) {
					if (IsDigit(i) || IsLatin(i)) {
						wordActions[i] = ActionAddCharToToken;
					} else {
						wordActions[i] = ActionNoRequestNextSymbol;
						wordActions[i] += ActionTokenCompleted;
					}
				}
			}
		}

		public Tokenizer(string path) : this() => stream = new StreamState(path);
		public Tokenizer(Stream stream) : this() => this.stream = new StreamState(stream);

		public void UpdateStream(string path)
		{
			stream.Update(path);
			isError = false;
		}

		public void UpdateStream(Stream stream)
		{
			this.stream.Update(stream);
			isError = false;
		}

		public Token Peek() => token;

		public Token Next()
		{
			if (isError) {
				return null;
			}
			token = new Token();
			tokenCompleted = false;
			while (!tokenCompleted && stream.Next() > -1) {
				if (stream.Symbol > alphabetSize) {
					throw new TokenizerException("Lexer fatal: unsupported character '" + stream.Symbol + "'");
				}
				actions[(int)activeState][stream.Symbol]();
			}
			UpdateTokenValue();
			ActionTokenCompleted();
			token = token.TypeId != Token.Type.Undefined ? token : null;
			return token;
		}

		private void ActionSkip() { }

		private void ActionClear() => token.RawValue = "";

		private void ActionOneSymbolOperator(Operator op)
		{
			token.TypeId = Token.Type.Operator;
			token.Value = op;
			token.RawValue = ((char)stream.Symbol).ToString();
			UpdateTokenLocation();
			ActionTokenCompleted();
		}

		private void ActionSetStartState() => activeState = State.Start;

		private void SetState(State state, Token.Type type, bool updateLocation)
		{
			activeState = state;
			token.TypeId = type;
			token.RawValue += ((char)stream.Symbol).ToString();
			if (updateLocation) {
				UpdateTokenLocation();
			}
		}

		private void ActionErrorSymbol()
		{
			isError = true;
			tokenCompleted = true;
			ActionAddCharToToken();
		}

		private void ActionNoRequestNextSymbol() => stream.NeedNextSymbol = false;

		private void ActionAddCharToToken() => token.RawValue += (char)stream.Symbol;

		private void ActionTokenCompleted()
		{
			tokenCompleted = true;
			ActionSetStartState();
		}

		private void UpdateTokenValue()
		{
			if (!isError) {
				try {
					switch (token.TypeId) {
						case Token.Type.Char:
							token.Value = token.RawValue[1];
							break;
						case Token.Type.String:
							token.Value = token.RawValue.Substring(1, token.RawValue.Length - 2);
							break;
						case Token.Type.Float:
							if (token.RawValue.Length > 2) {
								token.Value = float.Parse(token.RawValue, CultureInfo.InvariantCulture);
								if (float.IsInfinity((float)token.Value)) {
									token.Value = null;
									token.Message = "OverflowException";
								}
							}
							break;
						case Token.Type.Int:
							var raw = token.RawValue;
							if (raw.Length > 1 && (raw[1] == 'x' || raw[1] == 'X')) {
								token.Value = int.Parse(token.RawValue.Substring(2), NumberStyles.HexNumber);
							} else {
								token.Value = int.Parse(token.RawValue);
							}
							break;
						case Token.Type.Keyword:
							token.Value = KeywordToEnum(token.RawValue);
							break;
						case Token.Type.Identifier:
							token.TypeId = IsKeyword(token.RawValue) ? Token.Type.Keyword : Token.Type.Identifier;
							if (token.TypeId == Token.Type.Keyword) {
								token.Value = KeywordToEnum(token.RawValue);
							} else {
								token.Value = token.RawValue;
							}
							break;
						case Token.Type.Operator:
							if (token.Value == null) {
								switch (activeState) {
									case State.Ampersand: token.Value = Operator.BitwiseAnd; break;
									case State.Pipe: token.Value = Operator.BitwiseOr; break;
									case State.NotEquals: token.Value = Operator.LogicalNot; break;
									case State.Division: token.Value = Operator.Divide; break;
									case State.Equals: token.Value = Operator.EqualityTest; break;
									default: throw new InvalidOperationException();
								}
							}
							//token.Value = token.RawValue;
							break;
					}
				} catch (OverflowException) {
					token.Message = "OverflowException";
				} catch (FormatException) {
					token.Message = "FormatException";
				}
			}
		}

		private void UpdateTokenLocation()
		{
			token.RowIndex = stream.RowIndex;
			token.ColIndex = stream.ColIndex;
		}

		public void Dispose() => stream.Dispose();
	}
}
