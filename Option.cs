using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionParser.CommonTypes;

namespace OptionParser
{
    /// <summary>
    /// This sctruct provides maximum and minimum counts of arguments of an option.
    /// </summary>
    public struct OptionArity
    {
        #region Members

        /// <summary>
        /// Gets or sets the minimal occurs of arguments.
        /// </summary>
        public uint MinimalOccurs { private set; get; }

        /// <summary>
        /// Gets or sets the minimal occurs of arguments.
        /// </summary>
        public uint MaximalOccurs { private set; get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionArity"/> struct.
        /// Consider using static factory methods of this class instead.
        /// </summary>
        /// <param name="minimalOccurs">The minimal occurs of arguments.</param>
        /// <param name="maximalOccurs">The maximal occurs of arguments.</param>
        public OptionArity(uint minimalOccurs, uint maximalOccurs)
            :this()
        {
            MinimalOccurs = minimalOccurs;
            MaximalOccurs = maximalOccurs;
        }

        #endregion

        #region Factory Static Properties

        /// <summary>
        /// Gets the <see cref="OptionArity"/> for option with no argument.
        /// </summary>
        public static OptionArity NoArgument
        {
            get { return new OptionArity(0, 0); }
        }

        /// <summary>
        /// Gets the <see cref="OptionArity"/> for option with one optional argument.
        /// </summary>
        public static OptionArity OptionalArgument
        {
            get { return new OptionArity(0, 1); }
        }

        /// <summary>
        /// Gets the <see cref="OptionArity"/> for option with one required argument.
        /// </summary>
        public static OptionArity OneArgument
        {
            get { return new OptionArity(1, 1); }
        }

        /// <summary>
        /// Gets the <see cref="OptionArity"/> for option with one required argument and unlimited count of other arguments.
        /// </summary>
        public static OptionArity OneOrMoreArguments
        {
            get { return new OptionArity(1, uint.MaxValue); }
        }

        /// <summary>
        /// Gets the <see cref="OptionArity"/> for option with any number of optional arguments.
        /// </summary>
        public static OptionArity ZeroOrMoreArguments
        {
            get { return new OptionArity(0, uint.MaxValue); }
        }

        #endregion
    }

    /// <summary>
    /// This class represents an option for the <see cref="OptionParser"/>.
    /// Use instance od this class to add an option to parse.
    /// You can specify arity of the oprion and switches used for the option.
    /// You can also enumerate the string representations of arguments of the option after parsing is completed.
    /// </summary>
    public sealed class Option : IEnumerable<string> // myslim, ze je dobre, aby tahle trida byla sealed
    {
        /// <summary>
        /// Enumeration which tells whether the option is optional or required.
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// Option marked with this flag is optional.
            /// </summary>
            Optional,

            /// <summary>
            /// Option marked with this flag is required.
            /// If it is not specified before parsing input with <see cref="OptionParser"/> Exception will be thrown.
            /// </summary>
            Required
        }

        #region Mambers

        List<string> arguments = new List<string>();
        SwitchesManager switches;
        string description;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the switches that identifies the option.
        /// </summary>
        public string[] Switches
        {

            get
            {
                string[] result = new string[switches.Short.Count + switches.Long.Count];
                switches.Short.Switches.CopyTo(result, 0);
                switches.Long.Switches.CopyTo(result, switches.Short.Count);
                return result;
            }
        }

        /// <summary>
        /// Gets the string representation of the argument on specified position.
        /// </summary>
        public string this[int position]
        {
            get { return arguments[position]; } //TODO pridat vyhozeni vhodnejsi vyjimky nez IndexOutOfBoundsException
        }

        /// <summary>
        /// Gets the arguments count.
        /// </summary>
        public int ArgumentsCount
        {
            get { return arguments.Count; }
        }

        /// <summary>
        /// Gets the arity of the option.
        /// </summary>
        public OptionArity Arity { get; private set; }

        public AbstractType ValueType { get; private set; }

        public ModeType Mode { get; private set; }

        public object DefaultValue { get; private set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Option"/> class with no arguments and marked as optional.
        /// Default value for this option will be <c>false</c>.
        /// </summary>
        /// <param name="shortSwitches">The short (one char) switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="shortSwitches">The long switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="description">The description which will be typed in the help message.</param>
        public Option(string shortSwitches, string longSwitches, string description)
            : this(shortSwitches, longSwitches, description, false, ModeType.Optional, OptionArity.NoArgument, new BoolType())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Option"/> class according to specified details.
        /// Default value for this option will be <c>null</c>.
        /// </summary>
        /// <param name="shortSwitches">The short (one char) switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="shortSwitches">The long switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="description">The description which will be typed in the help message.</param>
        /// <param name="mode">The mode of the option specifies the optionality.</param>
        /// <param name="optionArity">The option arity.</param>
        /// <param name="valueType">Type of the value used for validating the argument and converting it to the desired type.</param>
        public Option(string shortSwitches, string longSwitches, string description, ModeType mode, OptionArity optionArity, AbstractType valueType)
            : this(shortSwitches, longSwitches, description, null, mode, optionArity, valueType)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Option"/> class according to specified details.
        /// </summary>
        /// <param name="shortSwitches">The short (one char) switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="shortSwitches">The long switches identifying the option use space as a delimiter of the switches.</param>
        /// <param name="description">The description which will be typed in the help message.</param>
        /// <param name="defaultValue">The default value of the option.</param>
        /// <param name="mode">The mode of the option specifies the optionality.</param>
        /// <param name="optionArity">The option arity.</param>
        /// <param name="valueType">Type of the value used for validating the argument and converting it to the desired type.</param>
        public Option(string shortSwitches, string longSwitches, string description, object defaultValue, ModeType mode, OptionArity optionArity, AbstractType valueType)
        {
            // TODO kontrola null
            if (shortSwitches == null) throw new ArgumentNullException("short switches");
            if (longSwitches == null) throw new ArgumentNullException("long switches");
            if (description == null) throw new ArgumentNullException("description");

            switches = new SwitchesManager(shortSwitches, longSwitches, optionArity, valueType);

            this.description = description;

            //TODO vyhodit specifickou vyjimku, pokud je switches.Length == 0

            DefaultValue = defaultValue;
            Mode = mode;
            Arity = optionArity;
            this.ValueType = valueType;
        }

        #endregion

        T GetValue<T>(string argument)
        {
            if (argument == null)
            {
                //TODO: zabalit do try pro pripad, ze by BFU hodil jako T nejakou blbost
                return (T)DefaultValue;
            }
            else
            {
                //TODO: zabalit do try pro pripad, ze by BFU hodil jako T nejakou blbost
                return (T)ValueType.FromString(argument); // tady pak bude misto option ten argument, co se precetl z prikazove radky
            }
        }

        /// <summary>
        /// Adds the string representation of the argument value.
        /// </summary>
        /// <param name="argument">The argument.</param>
        internal void AddArgumentValue(string argument)
        {
            arguments.Add(argument);
        }

        #region Public Methods

        public T GetValue<T>()
        {
            string argument = arguments.FirstOrDefault();
            return GetValue<T>(argument);
        }

        public T[] GetValues<T>()
        {
            if (arguments.Count == 0)
            {
                return new T[] { GetValue<T>(null) };
            }
            else
            {
                List<T> values = new List<T>();
                foreach (string argument in arguments)
                {
                    values.Add(GetValue<T>(argument));
                }
                return values.ToArray();
            }
        }

        public bool ValidateArgument(string argument)
        {
            return ValueType.Validate(argument);
        }

        #endregion

        #region Help Generator
        // tohle neni moc vodne pouziti regionu, je to vhodne spis na komentar
        public string ToHelp()
        {
            StringBuilder help = new StringBuilder();
            help.AppendFormat("{0}\n\t{1}", switches.ToHelp(Arity, ValueType), description);
            return help.ToString();
        }
        #endregion

        #region Arguments IEnumerable<string> Members

        /// <summary>
        /// Gets the enumerator that iterates through a collection of the string representations of the arguments.
        /// </summary>
        /// <returns>An <see cref="IEnumerator<string>"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return arguments.GetEnumerator();
        }

        #endregion

        #region Arguments IEnumerable 
        //for non-generic approach

        /// <summary>
        /// Gets the enumerator that iterates through a collection of the string representations of the arguments.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)arguments).GetEnumerator();
        }

        #endregion
    }
}
