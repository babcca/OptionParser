using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using OptionParser.CommonTypes;

namespace OptionParser
{
    public abstract class AbstractSwitches
    {
        private static char[] delimiters = new char[] { ' ' };
        // Struct for switches string
        private List<string> switchesList = new List<string>();

        #region Properties
        // Return all switches as array of array of string
        public string[] Switches { get { return switchesList.ToArray(); } }
        // Return count of switches
        public int Count { get { return switchesList.Count; } }
        #endregion

        #region Parse Method
        protected void ParseSwicthesString(string switchesString)
        {
            string[] switches = switchesString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sw in switches)
            {
                if (switchesList.Contains(sw))
                {
                    throw new ArgumentException("Prepinace se opakuji");
                }
                else
                {
                    switchesList.Add(sw);
                }
            }
        }
        #endregion

        #region Help generator
        public abstract string ToHelp(OptionArity arity, AbstractType valueType);
        #endregion
    }

    public class ShortSwitches : AbstractSwitches
    {
        public ShortSwitches(string switchesString)
        {
            ParseSwicthesString(switchesString);
        }

        #region Help generator
        public override string ToHelp(OptionArity arity, AbstractType valueType)
        {
            StringBuilder help = new StringBuilder();
            foreach (var sw in Switches)
            {
                help.AppendFormat("-{0}, ", sw);
            }

            return help.ToString();
        }
        #endregion
    }

    public class LongSwitches : AbstractSwitches
    {
        public LongSwitches(string switchesString)
        {
            ParseSwicthesString(switchesString);
        }

        #region Help generator
        public override string ToHelp(OptionArity arity, AbstractType valueType)
        {
            StringBuilder help = new StringBuilder();
            StringBuilder postfix = new StringBuilder();
            if (arity.MinimalOccurs == 0)
            {
                postfix.AppendFormat("[={0}]", valueType);
            }

            foreach (var sw in Switches)
            {
                help.AppendFormat("--{0}{1} ", sw, postfix);


            }

            return help.ToString();

        }
        #endregion
    }

    public class SwitchesManager
    {
        public ShortSwitches Short { get; private set; }
        public LongSwitches Long { get; private set; }

        public SwitchesManager(string shortSwitchesString, string longSwitchesString, OptionArity optionArity, AbstractType valueType)
        {
            Short = new ShortSwitches(shortSwitchesString);
            Long = new LongSwitches(longSwitchesString);
        }
        #region Help generator
        public string ToHelp(OptionArity arity, AbstractType valueType)
        {
            StringBuilder help = new StringBuilder();
            help.AppendFormat("{0}, {1}", Short.ToHelp(arity, valueType), Long.ToHelp(arity, valueType) );
            return help.ToString();
        }
        #endregion
    }
}
