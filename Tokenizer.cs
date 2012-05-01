using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace OptionParser
{
    /// <summary>
    /// Base tokenizer class. Extend this class and override Tokenize method for creating a new option tokenizer.
    /// <see cref="CultureInfo"/> and case-sensitivity flag will be provided in the extended classes.
    /// </summary>
    public abstract class Tokenizer
    {
        /// <summary>
        /// Gets the culture for comparison provided by the <see cref="OptionParser"/>.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Gets the case-sensitivity flag provided by the <see cref="OptionParser"/>.
        /// </summary>
        public bool IgnoreCase { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        protected Tokenizer()
        {
            Culture = CultureInfo.InvariantCulture;
            IgnoreCase = false;
        }

        /// <summary>
        /// Tokenizes the specified input.
        /// Override this method to create new tokenizer.
        /// </summary>
        /// <param name="options">The options with their arities.</param>
        /// <param name="inputs">The input to tokenize.</param>
        /// <returns>A collection of tokens representing options and arguments.</returns>
        public abstract Token[] Tokenize(IDictionary<string, OptionArity> options, IEnumerable<string> inputs);
    }

    public class SmartTokenizer : Tokenizer
        //TODO: prejmenovat!
    {
        #region Consts

        const string ShortOptionStarter = "-";
        const string LongOptionStarter = "--";
        const string TreatAsArgumentMark = "--";
        const string MappingSymbol = "=";

        #endregion

        /// <summary>
        /// Enumeration telling how to treat currently processing string.
        /// </summary>
        enum TreatMode { None, OptionArgument, Argument }

        #region Private Methods

        /// <summary>
        /// Adds the <see cref="ArgumentToken"/> to the tokens.
        /// </summary>
        /// <param name="tokens">The tokens where the argument will be add.</param>
        /// <param name="argument">The argument to add.</param>
        void AddArgument(List<Token> tokens, string argument)
        {
            ArgumentToken token = new ArgumentToken(argument);
            tokens.Add(token);
        }

        /// <summary>
        /// Adds the <see cref="ArgumentToken"/> to the tokens and sets the context of the tokenizer.
        /// </summary>
        /// <param name="tokens">The tokens where the argument will be add.</param>
        /// <param name="argument">The argument to add.</param>
        /// <param name="treatMode">The treat mode.</param>
        /// <param name="argumentCount">The argument count of the last option.</param>
        /// <param name="optionArity">The option arity.</param>
        void AddArgument(List<Token> tokens, string argument, ref TreatMode treatMode, ref uint argumentCount, ref OptionArity? optionArity)
        {
            Debug.Assert(optionArity.HasValue && argumentCount < optionArity.Value.MaximalOccurs, "unexpected option arity");

            AddArgument(tokens, argument);

            argumentCount++;
            if (argumentCount >= optionArity.Value.MinimalOccurs)
            {
                treatMode = TreatMode.None;
            }

            if (argumentCount == optionArity.Value.MaximalOccurs)
            {
                optionArity = null;
                argumentCount = uint.MaxValue;
            }
        }

        /// <summary>
        /// Adds the <see cref="OptionToken"/> to the tokens.
        /// </summary>
        /// <param name="tokens">The tokens where the argument will be add.</param>
        /// <param name="option">The option to add.</param>
        void AddOption(List<Token> tokens, string option)
        {
            OptionToken token = new OptionToken(option);
            tokens.Add(token);
        }

        /// <summary>
        /// Adds the <see cref="OptionToken"/> to the tokens and sets the context of the tokenizer.
        /// </summary>
        /// <param name="tokens">The tokens where the argument will be add.</param>
        /// <param name="option">The option to add.</param>
        /// <param name="options">The options and their arities.</param>
        /// <param name="treatMode">The treat mode.</param>
        /// <param name="argumentCount">The argument count of the last option.</param>
        /// <param name="optionArity">The option arity.</param>
        void AddOption(List<Token> tokens, string option, IDictionary<string, OptionArity> options,
            ref TreatMode treatMode, ref uint argumentCount, ref OptionArity? optionArity)
        {
            if (options.ContainsKey(option))
            {
                AddOption(tokens, option);

                OptionArity arity = options[option];
                if (arity.MinimalOccurs > 0)
                {
                    treatMode = TreatMode.OptionArgument;
                    argumentCount = 0;
                    optionArity = arity;
                }
            }
            else
            {
                AddOption(tokens, option);
            }
        }

        /// <summary>
        /// Prepares the input by splitting according the MappingSymbol and explodes short options like -abc to -a -b -c.
        /// </summary>
        /// <param name="input">The input to prepare.</param>
        /// <returns>Precalculated input.</returns>
        IEnumerable<string> PrepareInput(IEnumerable<string> input)
        {
            List<string> mapping = new List<string>();

            foreach (string s in input)
            {
                string[] exploded = s.Split(new string[] { MappingSymbol }, StringSplitOptions.RemoveEmptyEntries);
                mapping.AddRange(exploded);
            }

            List<string> result = new List<string>();

            foreach (string s in mapping)
            {
                if ((s.StartsWith(ShortOptionStarter)) && (!s.StartsWith(LongOptionStarter)))
                {
                    for (int i = ShortOptionStarter.Length; i < s.Length; i++)
                    {
                        result.Add(ShortOptionStarter + s[i].ToString());
                    }
                }
                else
                {
                    result.Add(s);
                }
            }

            return result;
        }

        #endregion

        #region overrids

        /// <summary>
        /// Tokenizes the specified options.
        /// </summary>
        /// <param name="options">The options with their arities.</param>
        /// <param name="inputs">The input to tokenize.</param>
        /// <returns>A collection of tokens representing options and arguments.</returns>
        public override Token[] Tokenize(IDictionary<string, OptionArity> options, IEnumerable<string> inputs)
        {
            /*
             * Cte postupne vstupni stringy a rozhazuje je. Kdyz ma nejaky option nastaveny minimalni pocet argumentu n, tak 
             * n stringu za nim bude povazovano za jeho argumenty.
             * Pri nastaveni maximalniho poctu argumentu bude vsechno, co nezacina na ShortOptionStarter, LongOptionStarter,
             * nebo na TreatAsArgumentMarkpovazovano povazovano za argument (po dosazeni minimalniho poctu argumentu).
             * Boolovske optiony nemusi mit za sebou zadny argument. meli by mit nastaveny pocet argumentu na 0.
             */

            inputs = PrepareInput(inputs);

            List<Token> tokens = new List<Token>();
            TreatMode treatMode = TreatMode.None;
            StringEqualityComparer stringEqualityComparer = new StringEqualityComparer(Culture, IgnoreCase);

            uint lastOptionArgumentCount = uint.MaxValue;
            OptionArity? lastOptionArity = null;

            foreach (string input in inputs)
            {
                if (treatMode == TreatMode.Argument)
                {
                    AddArgument(tokens, input);
                }
                else if (stringEqualityComparer.Equals(input, TreatAsArgumentMark))
                {
                    treatMode = TreatMode.Argument;
                    tokens.Add(new TreatAsArgumentToken());
                }
                else if (treatMode == TreatMode.OptionArgument)
                {
                    AddArgument(tokens, input, ref treatMode, ref lastOptionArgumentCount, ref lastOptionArity);
                }
                // tady uz je treatMode == TreatMode.None
                else if (input.StartsWith(LongOptionStarter, IgnoreCase, Culture))
                {
                    string option = input.Substring(LongOptionStarter.Length);
                    AddOption(tokens, option, options, ref treatMode, ref lastOptionArgumentCount, ref lastOptionArity);
                }
                else if (input.StartsWith(ShortOptionStarter, IgnoreCase, Culture))
                {
                    string option = input.Substring(ShortOptionStarter.Length);
                    Debug.Assert(option.Length == 1, string.Format("Unexpected short option: {0}", option));
                    AddOption(tokens, option, options, ref treatMode, ref lastOptionArgumentCount, ref lastOptionArity);
                }
                else
                {
                    AddArgument(tokens, input);
                }
            }

            return tokens.ToArray();
        }

        #endregion
    }

    // tuhle tridu potom zahodime
    public class BasicTokenizer : Tokenizer
    {
        #region Consts

        const string ShortOptionStarter = "-";
        const string LongOptionStarter = "--";
        const string TreatAsArgumentMark = "--";
        const string MappingSymbol = "=";
        const string TextSymbol = "\"";
        const char JoinCharacter = ' ';
        const char EscapeSymbol = '\\';

        #endregion

        #region Private Methods

        IEnumerable<string> PrepareInput(IEnumerable<string> input)
        {
            List<string> mappingSymbolSplit = new List<string>();

            foreach (string str in input)
            {
                string[] exploded = str.Split(new string[] { MappingSymbol }, StringSplitOptions.RemoveEmptyEntries);
                mappingSymbolSplit.AddRange(exploded);
            }

            string escapeSequence = EscapeSymbol.ToString() + TextSymbol.ToString();

            List<string> result = new List<string>();

            bool inText = false;
            string text = "";
            foreach (string str in mappingSymbolSplit)
            {
                if ((!inText) && (str.StartsWith(TextSymbol)) && (str.EndsWith(TextSymbol)) && (!str.EndsWith(escapeSequence)))
                {
                    result.Add(str);
                }
                else if ((!inText) && (str.StartsWith(TextSymbol)))
                {
                    text = str;
                    inText = true;
                }
                else if ((inText) && (str.EndsWith(TextSymbol)) && (!str.EndsWith(escapeSequence)))
                {
                    text += JoinCharacter.ToString() + str;
                    inText = false;
                    result.Add(text);
                }
                else if ((inText) && (!str.StartsWith(TextSymbol)))
                {
                    text += JoinCharacter.ToString() + str;
                }
                else if (!inText)
                {
                    result.Add(str);
                }
                else
                {
                    //TODO error
                }
            }

            return result;
        }

        #endregion

        #region Overrides

        public override Token[] Tokenize(IDictionary<string, OptionArity> options, IEnumerable<string> inputs)
        {
            /*
             * Cte postupne stringy ze vstupu. Pokud narazi na neco, co se rovna TreatAsArgumentMarkeru, vse za tim je cteno jako argument.
             * Pokud narazi na neco, co zacina LongOptionStarter, prida se OptionToken s dlouhym nazvem.
             * Pokud narazi na neco, co zacina ShortOptionStarter, pride se OptionToken pro kazdy char ve stringu.
             * Jinak se prida ArgumentToken.
             * 
             * kdyz neco zacina uvozovkou (a take ji pak musi koncit), je to povazovano za argument, at je v tom cokoliv (vcetne mezer a OptionStarteru)
             */

            //TODO uvozovky

            inputs = PrepareInput(inputs);

            List<Token> tokens = new List<Token>();

            bool treatAsArgument = false;

            StringEqualityComparer stringEqualityComparer = new StringEqualityComparer(Culture, IgnoreCase);

            foreach (string input in inputs)
            {
                if (treatAsArgument)
                {
                    tokens.Add(new ArgumentToken(input));
                }
                else if (input.StartsWith(TextSymbol, IgnoreCase, Culture))
                {
                    Debug.Assert(input.EndsWith(TextSymbol, IgnoreCase, Culture), "Should end with " + TextSymbol);
                    string argument = input.Substring(TextSymbol.Length, input.Length - TextSymbol.Length * 2);
                    tokens.Add(new ArgumentToken(argument));
                }
                else if (stringEqualityComparer.Equals(input, TreatAsArgumentMark))
                {
                    treatAsArgument = true;
                }
                else if (input.StartsWith(LongOptionStarter, IgnoreCase, Culture))
                {
                    string option = input.Substring(LongOptionStarter.Length);
                    tokens.Add(new OptionToken(option));
                }
                else if (input.StartsWith(ShortOptionStarter, IgnoreCase, Culture))
                {
                    string option = input.Substring(ShortOptionStarter.Length);
                    foreach (char c in option)
                    {
                        tokens.Add(new OptionToken(c.ToString()));
                    }
                }
                else
                {
                    tokens.Add(new ArgumentToken(input));
                }
            }

            return tokens.ToArray();
        }

        #endregion
    }

    public abstract class Token
    {
        public virtual string Value { get; protected set; }
    }

    public class OptionToken : Token
    {
        public OptionToken(string value)
        {
            Value = value;
        }
    }

    public class ArgumentToken : Token
    {
        public ArgumentToken(string value)
        {
            Value = value;
        }
    }

    public class TreatAsArgumentToken : Token
    {

    }
}
