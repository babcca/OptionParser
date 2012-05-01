using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using OptionParser.Exceptions;

namespace OptionParser.CommonTypes // teoreticky tady toho muze byt docela dost, tak jsem to vyclenil "vedle"
{
    public abstract class AbstractType
    {
        public CultureInfo Culture { get; internal set; }
        public bool IgnoreCase { get; internal set; }

        protected AbstractType()
        {
            Culture = CultureInfo.InvariantCulture;
            IgnoreCase = false;
        }

        public abstract object FromString(string value);
        public abstract bool Validate(string value);
    }

    public class IntType : AbstractType
    {
        int minValue;
        int maxValue;

        public IntType(int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
        
        public override object FromString(string value)
        {
            return int.Parse(value, Culture);
        }

        public override bool Validate(string value)
        {
            int dummy;
            bool result = int.TryParse(value, NumberStyles.Any, Culture, out dummy);
            if (result && dummy >= minValue && dummy <= maxValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class DoubleType : AbstractType
    {
        public override object FromString(string value)
        {
            return double.Parse(value, Culture);
        }

        public override bool Validate(string value)
        {
            double dummy;
            return double.TryParse(value, NumberStyles.Any, Culture, out dummy);
        }
    }

    public class BoolType : AbstractType
    {
        public override object FromString(string value)
        {
            return bool.Parse(value);
        }

        public override bool Validate(string value)
        {
            bool dummy;
            return bool.TryParse(value, out dummy);
        }
    }

    public class StringType : AbstractType
    {
        public override object FromString(string value)
        {
            return value;
        }

        public override bool Validate(string value)
        {
            return value != null;
        }
    }
    
    public class EnumStringType : AbstractType
    {
        string[] allowedStrings;

        public EnumStringType(params string[] allowedStrings)
        {
            if (allowedStrings == null) throw new ArgumentNullException("allowedStrings");

            this.allowedStrings = allowedStrings;
        }

        #region overrides

        public override object FromString(string value)
        {
            StringEqualityComparer stringEqualityComparer = new StringEqualityComparer(Culture, IgnoreCase);

            if (allowedStrings.Contains(value, stringEqualityComparer))
            {
                return value;
            }
            else
            {
                throw new NotImplementedException();
                //TODO vyhodit vhodnou vyjimku
            }
        }

        public override bool Validate(string value)
        {
            StringEqualityComparer stringEqualityComparer = new StringEqualityComparer(Culture, IgnoreCase);

            return allowedStrings.Contains(value, stringEqualityComparer);
        }

        #endregion
    }
}
