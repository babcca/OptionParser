﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using OptionParser.CommonTypes;
using OptionParser.Exceptions;

namespace OptionParser
{
    /// <summary>
    /// 
    /// </summary>
    public class OptionParser // meli bychom se zamyslet nad tim, jestli ho neudelat Sealed. Jestli ne, pak by meli byt verejne metody virtualni.
    {
        #region Members

        Tokenizer tokenizer;
        string mainHelp;

        List<Option> Options = new List<Option>();
        List<string> parameters = new List<string>();

        CultureInfo culture;
        bool ignoreCase;

        StringEqualityComparer stringEqualityComparer;

        Option[] optionalOptions
        {
            get
            {
                return Options.Where(opt => opt.Mode == Option.ModeType.Optional).ToArray();
            }
        }

        Option[] requiredOptions
        {
            get
            {
                return Options.Where(opt => opt.Mode == Option.ModeType.Required).ToArray();
            }
        }
        #endregion
        
        #region Constructors

        public OptionParser()
            : this(new BasicTokenizer(), CultureInfo.InvariantCulture, false, "")
        { }

        public OptionParser(Tokenizer tokenizer)
            : this(tokenizer, CultureInfo.InvariantCulture, false, "")
        { }

        public OptionParser(string mainHelp)
            : this(new BasicTokenizer(), CultureInfo.InvariantCulture, false, mainHelp)
        { }

        public OptionParser(Tokenizer tokenizer, CultureInfo culture, bool ignoreCase, string mainHelp = "")
        {
            if (tokenizer == null) throw new ArgumentNullException("tokenizer");
            if (culture == null) throw new ArgumentNullException("culture");

            this.tokenizer = tokenizer;
            this.culture = culture;
            this.ignoreCase = ignoreCase;
            this.mainHelp = mainHelp;
            stringEqualityComparer = new StringEqualityComparer(culture, ignoreCase);
            tokenizer.Culture = culture;
            tokenizer.IgnoreCase = ignoreCase;
        }

        #endregion

        #region Private Methods

        bool SwitchesAreCorect(IEnumerable<string> switches)
        {
            foreach (string s in switches)
            {
                
                    int count = Options.Where(opt => opt.Switches.Contains(s, stringEqualityComparer)).Count();
                    if (count > 0) return false;

            }

            return true;
        }

        Option GetOptionByToken(Token token)
        {
            return Options.Where(opt => opt.Switches.Contains(token.Value)).FirstOrDefault();
        }
        #endregion

        #region Public Methods
        public void AddOptions(params Option[] options)
        {
            foreach (Option option in options)
            {
                if (SwitchesAreCorect(option.Switches))
                {
                    option.ValueType.Culture = culture;
                    option.ValueType.IgnoreCase = ignoreCase;
                    Options.Add(option);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        // bude vracet hodnotu argumentu prvniho optionu
        public T GetValue<T>(string switchIdentifier)
        {
            // najit prepinac odpovidajic vstupu option

            // priklad nachazeni optionu (linearni cas)
            Option option = Options.Where(opt => opt.Switches.Contains(switchIdentifier, stringEqualityComparer)).FirstOrDefault();
            if (option == null)
            {
                throw new OptionNotFoundException(switchIdentifier);
            }
            
            
            string argument = option.FirstOrDefault();
            if (argument == null)
            {
                return (T)option.DefaultValue;
            }
            else
            {
                // test na ne-null
                return (T)option.ValueType.FromString(argument); // tady pak bude misto option ten argument, co se precetl z prikazove radky
            }
        }

        // bude vracet hodnoty vsech argumentu optionu, pretezovani se totiz neucastni navratovy typ
        public T[] GetValues<T>(string option)
        {
            return null;
        }


        //vrati parametry... ty, co jsou na konci za tema optionama a jejich argumentama
        public string[] GetParameters()
        {
            return parameters.ToArray();
        }

        public void Parse(string[] arguments)
        {
            Dictionary<string, OptionArity> optionDictionary = new Dictionary<string, OptionArity>();
            foreach (Option option in Options)
            {
                foreach (string identifier in option.Switches)
                {
                    optionDictionary.Add(identifier, option.Arity);
                }
            }
            Token[] tokens = tokenizer.Tokenize(optionDictionary, arguments);

            CheckUnexpectedOption(tokens);
            List<Option> parsedOptions = GetParsedOptions(tokens);
            CheckRequiredOptions(parsedOptions);
            
        }

        public void WriteHelp(TextWriter writer)
        {
            writer.WriteLine("{0}", mainHelp);
            foreach (Option option in Options)
            {
                writer.WriteLine("{0}", option.ToHelp());
            }

        }

        #endregion

        #region Tokens checker
        // pro kazdy token najdi registrovany option
        void CheckUnexpectedOption(Token[] tokens)
        {
            List<Option> usedOptions = new List<Option>();

            foreach (var token in tokens) {
                bool isOption = token is OptionToken;
                if (isOption)
                {
                    Option tokenOption = GetOptionByToken(token);
                    bool optionExist = tokenOption != null;
                 
                    if (!optionExist)
                    {
                        throw new SwitchNotRegistredException(token.Value);
                    }
                    else if (usedOptions.Contains(tokenOption))
                    {
                        throw new DuplicitOptionSwitchException(tokenOption.Switches.First());
                    }
                    else
                    {
                        usedOptions.Add(tokenOption);
                    }
                }
            }
        }


        // prunik naparsovanych a povinnych musi byt roven povinnym
        // |N prunik P| == |P|
        void CheckRequiredOptions(List<Option> parsedOption)
        {
            int intersectionSize = requiredOptions.Where(opt => parsedOption.Contains(opt)).Count();
            if (intersectionSize != requiredOptions.Length)
            {
                throw new RequiredOptionIsMissingException("any");
            }
        }


        #endregion

        #region Get Parsed Options
        // pro kazdy token najdi prislusny option
        List<Option> GetParsedOptions(Token[] tokens)
        {
            Parser parser = new Parser(tokens, Options);
            parser.Parse();
            parameters = parser.Parameters;
            return parser.ParsedOption;
        }
        #endregion

     
        #region @deprected
        public void Parse(string arguments, params char[] delimiters)
        {
            //TODO vyhodit vhodnou vyjimku, kdyz je delimiters.Length == 0
            string[] input = arguments.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            Parse(input);
        }

        public bool IsSet(string switchIdentifier)
        {
            // predstava funkcnosti
            Option option = Options.Where(opt => opt.Switches.Contains(switchIdentifier, stringEqualityComparer)).FirstOrDefault();
            return option.ArgumentsCount != 0;
        }

        void CheckRequiredArguments(List<Option> parsedOptions)
        {
            // pocita se s tim, ze parsovani uz proslo... u nej se vyhodi vyjimka, pokud se argumenty spatne naparsuji, takze tady uz jsou vsechny v poradku
            Option corupted = parsedOptions.Where(opt => opt.ArgumentsCount < opt.Arity.MinimalOccurs).FirstOrDefault();
            if (corupted != null)
            {
                throw new RequiredOptionIsMissingException(corupted.Switches.First());
            }
        }
        #endregion
    }


    //tohle pak zmizi a bude z toho DLL projekt
    class Program
    {
        static void Main(string[] args)
        {
            OptionParser parser = new OptionParser(new SmartTokenizer());

            // Switche vzdy NoArgument, Type.Optional
            // Ve skutecnost Maji jeden nepovinny argument typu bool s defaultni hodnotou false
            // Option("-v --verbose", "Ukecany vypis") ~ Option("-v --verbose", "Ukecany vypis", Option.Type.Optional, Option.NoArgument, new BoolType(false));
            
            // Zatim je povoleno mit -v a --v
            // Option parser toto nedovoluje => kontrola uz pri vytvareni optionu
            Option verbose = new Option("v", "verbose ukecany povidej", "Ukecany vypis");

            Option super = new Option("n", "nice", "Hezky vypis");

            // Optiony s argumenty, povinne nebo nepovinne
            Option outputFile = new Option("o","output", "Vystupni soubor", Option.ModeType.Optional, OptionArity.OneArgument, new StringType());
            // s option
            Option sOption = new Option("s", "", "S option", Option.ModeType.Optional, OptionArity.OneArgument, new StringType());
            // U optional nejak nastavit defaultni hodnotu
            Option logFile = new Option("l", "output-log", "Vystupni log", Option.ModeType.Optional, OptionArity.OneArgument, new StringType());
            // Nebo druha moznost
            Option inputFile = new Option("i", "file", "Vstupni soubory", "defaultni hodnota", Option.ModeType.Optional,
                OptionArity.ZeroOrMoreArguments, new StringType());

            Option range = new Option("r", "range", "Rozmezi", Option.ModeType.Optional, new OptionArity(2,3), new EnumStringType(new string[]{"a", "b", "c", "d", "e"}));


            parser.AddOptions(verbose, super, logFile, inputFile, sOption, outputFile, range);
            parser.Parse(args);

            //tady bych ty veci mozna cekal bez -- a -
            //to - a -- je zavisle na platforme, coz od knihovny asi nechece... navic stejne ocekavam, ze vevnitr v tom parseru nebo v cem si ty stringy budeme ukladat bez toho - a --
            Tester(true, parser.GetValue<bool>("verbose"));
            Tester("ahoj", parser.GetValue<string>("s"));
            Tester("c:\\users\\Petr Babicka\\output", parser.GetValue<string>("o"));
            Tester("c:\\log\\main_log.txt", parser.GetValue<string>("l"));
            parser.WriteHelp(Console.Out);
        }

        //tohle taky zmizi
        static void Tester<T>(T expect, T value)
        {
            if (expect.Equals(value))
            {
                Console.WriteLine("Pass");
            }
            else
            {
                Console.WriteLine("FAIL:\nActual: {0}, Except: {1}", value, expect);
            }
        }
    }
}
