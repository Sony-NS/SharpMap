using System;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils
{
    ///<summary>
    ///</summary>
    public static class NamingHelper
    {
        private class NameComparer : IEqualityComparer<string>
        {
            private readonly bool ignoreCase;

            public NameComparer(bool ignoreCase)
            {
                this.ignoreCase = ignoreCase;
            }

            public bool Equals(string x, string y)
            {
                return ignoreCase ? x.Equals(y, StringComparison.CurrentCultureIgnoreCase) : x.Equals(y);
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Extracts an unique name from the item collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <param name="items"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetUniqueName<T>(string filter, IEnumerable<T> items, Type t) where T : INameable
        {
            return GetUniqueName<T>(filter, items, t, true);
        }

        /// <summary>
        /// Extracts an unique name from the item collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <param name="items"></param>
        /// <param name="t"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static string GetUniqueName<T>(string filter, IEnumerable<T> items, Type t, bool ignoreCase) where T : INameable
        {
            if (null != filter)
            {
                if (filter.Length == 0)
                {
                    // to do test if filter has format code
                    throw new ArgumentException("Can not create an unique name when filter is empty.");
                }

                if (!filter.Contains("{0")) // format supported with {0:d2}
                {
                    throw new ArgumentException("Invalid filter");
                }
            }
            else
            {
                filter = t.Name + "{0}";
            }

            var namesList = items.Select(item => item.Name).Distinct().ToList();

            String unique;
            int id = 1;

            do
            {
                unique = String.Format(filter, id++);
            } while (namesList.Contains(unique, new NameComparer(ignoreCase)));

            return unique;
        }
    }
}