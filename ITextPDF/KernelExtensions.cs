/*

This file is part of the iText (R) project.
    Copyright (c) 1998-2021 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using IText.IO.Util;
using IText.IO.Util.Collections;

namespace IText
{
    internal static class KernelExtensions
    {
        public static string JSubstring(this string str, int beginIndex, int endIndex)
        {
            return str.Substring(beginIndex, endIndex - beginIndex);
        }

        public static string JSubstring(this StringBuilder sb, int beginIndex, int endIndex)
        {
            return sb.ToString(beginIndex, endIndex - beginIndex);
        }

        public static bool EqualsIgnoreCase(this string str, string anotherString)
        {
            return string.Equals(str, anotherString, StringComparison.OrdinalIgnoreCase);
        }

        public static void JReset(this MemoryStream stream)
        {
            stream.SetLength(0);
        }

        public static void CustomWrite(this Stream stream, int value)
        {
            stream.WriteByte((byte) value);
        }

        public static int CustomRead(this Stream stream)
        {
            return stream.ReadByte();
        }

        public static int CustomRead(this Stream stream, byte[] buffer)
        {
            var size = stream.Read(buffer, 0, buffer.Length);
            return size == 0 ? -1 : size;
        }

        public static int JRead(this Stream stream, byte[] buffer, int offset, int count)
        {
            var result = stream.Read(buffer, offset, count);
            return result == 0 ? -1 : result;
        }

        public static void CustomWrite(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static byte[] GetBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static byte[] GetBytes(this string str, string encoding)
        {
            return EncodingUtil.GetEncoding(encoding).GetBytes(str);
        }

        public static byte[] GetBytes(this string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static long Seek(this FileStream fs, long offset)
        {
            return fs.Seek(offset, SeekOrigin.Begin);
        }

        public static long Skip(this Stream s, long n)
        {
            s.Seek(n, SeekOrigin.Current);
            return n;
        }

        public static List<T> SubList<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            if (list is SingletonList<T>)
            {
                if (fromIndex == 0 && toIndex >= 1)
                {
                    return new List<T>(list);
                }

                return new List<T>();
            }

            return ((List<T>) list).GetRange(fromIndex, toIndex - fromIndex);
        }


        public static void AddAll<T>(this IList<T> list, IEnumerable<T> c)
        {
            ((List<T>) list).AddRange(c);
        }

        public static void AddAll<T>(this IList<T> list, int index, IList<T> c)
        {
            for (var i = c.Count - 1; i >= 0; i--)
            {
                list.Insert(index, c[i]);
            }
        }

        public static void Add<T>(this IList<T> list, int index, T elem)
        {
            list.Insert(index, elem);
        }

        public static void AddAll<T>(this ICollection<T> c, IEnumerable<T> collectionToAdd)
        {
            foreach (var o in collectionToAdd)
            {
                c.Add(o);
            }
        }

        public static void AddAll<TKey, TValue>(this IDictionary<TKey, TValue> c,
            IDictionary<TKey, TValue> collectionToAdd)
        {
            foreach (var pair in collectionToAdd)
            {
                c[pair.Key] = pair.Value;
            }
        }

        public static void GetChars(this StringBuilder sb, int srcBegin, int srcEnd, char[] dst, int dstBegin)
        {
            sb.CopyTo(srcBegin, dst, dstBegin, srcEnd - srcBegin);
        }

        public static string[] Split(this string str, string regex)
        {
            return str.Split(str.ToCharArray());
        }

        public static bool Matches(this string str, string regex)
        {
            return Regex.IsMatch(str, regex);
        }

        public static T[] ToArray<T>(this ICollection<T> col, T[] toArray)
        {
            T[] r;
            var colSize = col.Count;
            if (colSize <= toArray.Length)
            {
                col.CopyTo(toArray, 0);
                if (colSize != toArray.Length)
                {
                    toArray[colSize] = default;
                }

                r = toArray;
            }
            else
            {
                r = new T[colSize];
                col.CopyTo(r, 0);
            }

            return r;
        }

        public static T[] ToArray<T>(this ICollection<T> col)
        {
            var r = new T[col.Count];
            col.CopyTo(r, 0);
            return r;
        }

        public static void ReadFully(this BinaryReader input, byte[] b, int off, int len)
        {
            if (len < 0)
            {
                throw new IndexOutOfRangeException();
            }

            var n = 0;
            while (n < len)
            {
                var count = input.Read(b, off + n, len - n);
                if (count <= 0)
                {
                    throw new EndOfStreamException();
                }

                n += count;
            }
        }

        public static TValue JRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            dictionary.TryGetValue(key, out value);
            dictionary.Remove(key);

            return value;
        }

        public static T JRemoveAt<T>(this IList<T> list, int index)
        {
            var value = list[index];
            list.RemoveAt(index);

            return value;
        }

        public static bool RemoveAll<T>(this IList<T> list, ICollection<T> c)
        {
            return BatchRemove(list, c, false);
        }

        // Removes from this list all of its elements that are not contained in the specified collection.
        public static bool RetainAll<T>(this IList<T> list, ICollection<T> c)
        {
            return BatchRemove(list, c, true);
        }

        private static bool BatchRemove<T>(IList<T> list, ICollection<T> c, bool complement)
        {
            var modified = false;
            var j = 0;
            for (var i = 0; i < list.Count; ++i)
            {
                if (c.Contains(list[i]) == complement)
                {
                    list[j++] = list[i];
                }
            }

            if (j != list.Count)
            {
                modified = true;
                for (var i = list.Count - 1; i >= j; --i)
                {
                    list.RemoveAt(i);
                }
            }

            return modified;
        }

        public static bool RemoveAll<T>(this ICollection<T> toClean, ICollection<T> c)
        {
            var modified = false;
            foreach (var element in c)
            {
                bool anythingToRemove;
                do
                {
                    anythingToRemove = toClean.Remove(element);
                    modified |= anythingToRemove;
                } while (anythingToRemove);
            }

            return modified;
        }

        public static bool RetainAll<T>(this ICollection<T> toClean, ICollection<T> c)
        {
            IList<T> toRemove = new List<T>();
            foreach (var element in toClean)
            {
                if (!c.Contains(element))
                {
                    toRemove.Add(element);
                }
            }

            return toClean.RemoveAll(toRemove);
        }

        public static T PollFirst<T>(this SortedSet<T> set)
        {
            var item = set.First();
            set.Remove(item);

            return item;
        }

        public static bool IsEmpty<T1, T2>(this ICollection<KeyValuePair<T1, T2>> collection)
        {
            return collection.Count == 0;
        }

        public static bool IsEmpty<T>(this ICollection<T> collection)
        {
            return collection.Count == 0;
        }

        public static bool IsEmpty<T>(this Stack<T> collection)
        {
            return collection.Count == 0;
        }

        public static void SetCharAt(this StringBuilder builder, int index, char ch)
        {
            builder[index] = ch;
        }

        public static float NextFloat(this Random random)
        {
            var mantissa = random.NextDouble();
            var exponent = Math.Pow(2.0, random.Next(-126, 128));
            if (mantissa < 0 || exponent < 0)
            {
                var a = 5;
            }

            var val = (float) (mantissa * exponent);
            if (val < 0)
            {
                var b = 6;
            }

            return (float) (mantissa * exponent);
        }

        public static bool NextBoolean(this Random random)
        {
            return random.NextDouble() > 0.5;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> col, TKey key)
        {
            var value = default(TValue);
            if (key != null)
            {
                col.TryGetValue(key, out value);
            }

            return value;
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> col, TKey key, TValue value)
        {
            TValue oldVal = col.Get(key);
            col[key] = value;
            return oldVal;
        }

        public static object Get(this IDictionary col, object key)
        {
            object value = null;
            if (key != null)
            {
                value = col[key];
            }

            return value;
        }

        public static void Put(this IDictionary col, object key, object value)
        {
            if (key != null)
            {
                col[key] = value;
            }
        }

        public static bool Contains<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public static Stack<T> Clone<T>(this Stack<T> stack)
        {
            return new Stack<T>(new Stack<T>(stack)); // create stack twice to retain the original order
        }


        public static bool CanExecute(this FileInfo fileInfo)
        {
            return fileInfo.Exists;
        }

        public static T PollFirst<T>(this LinkedList<T> list)
        {
            var result = list.First.Value;
            list.RemoveFirst();
            return result;
        }


    }
}
