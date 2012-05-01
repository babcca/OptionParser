using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptionParser.Exceptions
{
    public abstract class OptionParserException : Exception
    {
        public OptionParserException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class RequiredOptionIsMissingException : OptionParserException
    {
        public RequiredOptionIsMissingException(string switchIdentifier)
            : base(string.Format("Reruired option with switch \"{0}\" is missing", switchIdentifier))
        { }
    }

    public class RequiredArgumentIsMissingException : OptionParserException
    {
        public RequiredArgumentIsMissingException(string switchIdentifier)
            : base(string.Format("Reruired argument for option with switch \"{0}\" is missing", switchIdentifier))
        { }
    }

    public class ArgumentValidityException : OptionParserException
    {
        public ArgumentValidityException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class DuplicitOptionSwitchException : OptionParserException
    {
        public DuplicitOptionSwitchException(string switchIdentifier)
            : base(string.Format("There is a duplicit switch \"{0}\" for options.", switchIdentifier))
        { }
    }

    public class SwitchNotRegistredException : OptionParserException
    {
        public SwitchNotRegistredException(string switchIdentifier)
            : base(string.Format("Switch \"{0}\" is not registred for any option.", switchIdentifier))
        { }
    }

    public class OptionNotFoundException : OptionParserException
    {
        public OptionNotFoundException(string optionIdentifier)
            : base(string.Format("Option \"{0}\" not found", optionIdentifier))
        { }
    }

}
