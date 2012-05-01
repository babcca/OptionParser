using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using OptionParser.CommonTypes;

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

        List<Option> options = new List<Option>();
        List<string> parameters = new List<string>();

        CultureInfo culture;
        bool ignoreCase;

        StringEqualityComparer stringEqualityComparer;

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
                int count = options.Where(opt => opt.Switches.Contains(s, stringEqualityComparer)).Count();
                if (count > 0) return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        // Vybec netusim jak tady vracet ty precasteny hodnoty
        // bude vracet hodnotu argumentu prvniho optionu
        public T GetValue<T>(string switchIdentifier)
        {
            // najit prepinac odpovidajic vstupu option

            // priklad nachazeni optionu (linearni cas)
            Option option = options.Where(opt => opt.Switches.Contains(switchIdentifier, stringEqualityComparer)).FirstOrDefault();

            // test na ne-null

            string argument = option.FirstOrDefault();
            // test na ne-null
            return (T)option.ValueType.FromString(argument); // tady pak bude misto option ten argument, co se precetl z prikazove radky
        }

        // bude vracet hodnoty vsech argumentu optionu, pretezovani se totiz neucastni navratovy typ
        public T[] GetValues<T>(string option)
        {
            return null;
        }

        public bool IsSet(string switchIdentifier)
        {
            // predstava funkcnosti
            Option option = options.Where(opt => opt.Switches.Contains(switchIdentifier, stringEqualityComparer)).FirstOrDefault();
            return option.ArgumentsCount != 0;
        }

        //vrati parametry... ty, co jsou na konci za tema optionama a jejich argumentama
        public string[] GetParameters()
        {
            return parameters.ToArray();
        }

        public void Parse(string[] arguments)
        {
            Dictionary<string, OptionArity> optionDictionary = new Dictionary<string, OptionArity>();
            foreach (Option option in options)
            {
                foreach (string identifier in option.Switches)
                {
                    optionDictionary.Add(identifier, option.Arity);
                }
            }
            Token[] tokens = tokenizer.Tokenize(optionDictionary, arguments);
            // vyhodi vyjimku, pokud nebudou pritomny vsechny povinne optiony
            //TODO: vyhodnotit tokeny a pripadit optionum nactene argumenty pomoci option.AddArgument
        }

        public void Parse(string arguments, params char[] delimiters)
        {
            //TODO vyhodit vhodnou vyjimku, kdyz je delimiters.Length == 0
            string[] input = arguments.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            Parse(input);
        }

        public void AddOptions(params Option[] options)
        {
            foreach (Option option in options)
            {
                if (SwitchesAreCorect(option.Switches))
                {
                    option.ValueType.Culture = culture;
                    option.ValueType.IgnoreCase = ignoreCase;
                    this.options.Add(option);
                }
                else
                {
                    //TODO vyhodit vhodnou vyjimku
                }
            }
        }

        public void WriteHelp(TextWriter writer)
        {

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
            Option verbose = new Option("v p verbose ukecany povidej", "Ukecany vypis");
            Option super = new Option("n nice", "Hezky vypis");

            // Optiony s argumenty, povinne nebo nepovinne
            Option outputFile = new Option("o output", "Vystupni soubor", Option.Mode.Required, OptionArity.OneArgument, new StringType());
            // U optional nejak nastavit defaultni hodnotu
            Option logFile = new Option("ol output-log", "Vystupni log", Option.Mode.Optional, OptionArity.OneArgument, new StringType());
            // Nebo druha moznost
            Option inputFile = new Option("i file", "Vstupni soubory", "defaultni hodnota", Option.Mode.Optional,
                OptionArity.ZeroOrMoreArguments, new StringType());

            parser.AddOptions(verbose, super, outputFile, logFile, inputFile);
            
            parser.Parse(args);

            //tady bych ty veci mozna cekal bez -- a -
            //to - a -- je zavisle na platforme, coz od knihovny asi nechece... navic stejne ocekavam, ze vevnitr v tom parseru nebo v cem si ty stringy budeme ukladat bez toho - a --
            //Tester(true, parser.GetValue("--verbose", new BoolType()));
            //Tester(true, parser.GetValue("-s", new BoolType()));
            Tester("c:\\users\\Petr Babicka\\output", parser.GetValue<string>("o"));
            Tester("c:\\log\\main_log.txt", parser.GetValue<string>("ol"));
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
