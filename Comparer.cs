using System;
using System.Collections.Generic;
using System.Globalization;

namespace OptionParser
{
    /// <summary>
    /// String comparer which determines whether two strings are equal.
    /// Comparison is based on provided <see cref="CultureInfo"/> and case-sensitivity.
    /// </summary>
    class StringEqualityComparer : IEqualityComparer<string>
    {
        StringComparer comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringEqualityComparer"/> class.
        /// </summary>
        /// <param name="culture">The instance of <see cref="CultureInfo"/>.</param>
        /// <param name="ignoreCase">If set to <c>true</c> comparison will be case-insensitive.</param>
        public StringEqualityComparer(CultureInfo culture, bool ignoreCase)
        {
            if (culture == null) throw new ArgumentNullException("culture");

            comparer = StringComparer.Create(culture, ignoreCase);
        }

        #region IEqualityComparer<string> Members

        /// <summary>
        /// Equalses the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if both swtings are equal; <c>false</c> otherwise.</returns>
        public bool Equals(string x, string y)
        {
            return comparer.Compare(x, y) == 0;
        }

        /// <summary>
        /// Returns a hash code for provided string.
        /// </summary>
        /// <param name="obj">The string to calculate the hash code.</param>
        /// <returns>
        /// A hash code for the provided string, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }

        #endregion
    }
}
