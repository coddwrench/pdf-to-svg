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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using IText.IO.Source;
using IText.IO.Util;

namespace  IText.IO.Font.Cmap {
    public class CMapContentParser {
        /// <summary>Commands have this type.</summary>
        public const int COMMAND_TYPE = 200;

        /// <summary>Holds value of property tokeniser.</summary>
        private PdfTokenizer tokeniser;

        /// <summary>Creates a new instance of PdfContentParser</summary>
        /// <param name="tokeniser">the tokeniser with the content</param>
        public CMapContentParser(PdfTokenizer tokeniser) {
            this.tokeniser = tokeniser;
        }

        /// <summary>Parses a single command from the content.</summary>
        /// <remarks>
        /// Parses a single command from the content. Each command is output as an array of arguments
        /// having the command itself as the last element. The returned array will be empty if the
        /// end of content was reached.
        /// </remarks>
        /// <param name="ls">
        /// an
        /// <c>ArrayList</c>
        /// to use. It will be cleared before using.
        /// </param>
        public virtual void Parse(IList<CMapObject> ls) {
            ls.Clear();
            CMapObject ob;
            while ((ob = ReadObject()) != null) {
                ls.Add(ob);
                // PdfTokenType.Other or CMapObject.Literal means a command
                if (ob.IsLiteral()) {
                    break;
                }
            }
        }

        /// <summary>Reads a dictionary.</summary>
        /// <remarks>
        /// Reads a dictionary. The tokeniser must be positioned past the
        /// <c>"&lt;&lt;"</c>
        /// token.
        /// </remarks>
        /// <returns>the dictionary</returns>
        public virtual CMapObject ReadDictionary() {
            IDictionary<string, CMapObject> dic = new Dictionary<string, CMapObject>();
            while (true) {
                if (!NextValidToken()) {
                    throw new IOException("Unexpected end of file.");
                }
                if (tokeniser.GetTokenType() == PdfTokenizer.PdfTokenType.EndDic) {
                    break;
                }
                if (tokeniser.GetTokenType() == PdfTokenizer.PdfTokenType.Other && "def".Equals(tokeniser.GetStringValue())) {
                    continue;
                }
                if (tokeniser.GetTokenType() != PdfTokenizer.PdfTokenType.Name) {
                    throw new IOException("Dictionary key {0} is not a name.").SetMessageParams(tokeniser.GetStringValue
                        ());
                }
                var name = tokeniser.GetStringValue();
                var obj = ReadObject();
                if (obj.IsToken()) {
                    if (obj.ToString().Equals(">>")) {
                        tokeniser.ThrowError(IOException.UnexpectedGtGt);
                    }
                    if (obj.ToString().Equals("]")) {
                        tokeniser.ThrowError(IOException.UnexpectedCloseBracket);
                    }
                }
                dic.Put(name, obj);
            }
            return new CMapObject(CMapObject.DICTIONARY, dic);
        }

        /// <summary>Reads an array.</summary>
        /// <remarks>Reads an array. The tokeniser must be positioned past the "[" token.</remarks>
        /// <returns>an array</returns>
        public virtual CMapObject ReadArray() {
            IList<CMapObject> array = new List<CMapObject>();
            while (true) {
                var obj = ReadObject();
                if (obj.IsToken()) {
                    if (obj.ToString().Equals("]")) {
                        break;
                    }
                    if (obj.ToString().Equals(">>")) {
                        tokeniser.ThrowError(IOException.UnexpectedGtGt);
                    }
                }
                array.Add(obj);
            }
            return new CMapObject(CMapObject.ARRAY, array);
        }

        /// <summary>Reads a pdf object.</summary>
        /// <returns>the pdf object</returns>
        public virtual CMapObject ReadObject() {
            if (!NextValidToken()) {
                return null;
            }
            var type = tokeniser.GetTokenType();
            switch (type) {
                case PdfTokenizer.PdfTokenType.StartDic: {
                    return ReadDictionary();
                }

                case PdfTokenizer.PdfTokenType.StartArray: {
                    return ReadArray();
                }

                case PdfTokenizer.PdfTokenType.String: {
                    CMapObject obj;
                    if (tokeniser.IsHexString()) {
                        obj = new CMapObject(CMapObject.HEX_STRING, PdfTokenizer.DecodeStringContent(tokeniser.GetByteContent(), true
                            ));
                    }
                    else {
                        obj = new CMapObject(CMapObject.STRING, PdfTokenizer.DecodeStringContent(tokeniser.GetByteContent(), false
                            ));
                    }
                    return obj;
                }

                case PdfTokenizer.PdfTokenType.Name: {
                    return new CMapObject(CMapObject.NAME, DecodeName(tokeniser.GetByteContent()));
                }

                case PdfTokenizer.PdfTokenType.Number: {
                    var numObject = new CMapObject(CMapObject.NUMBER, null);
                    try {
                        numObject.SetValue((int)double.Parse(tokeniser.GetStringValue(), CultureInfo.InvariantCulture
                            ));
                    }
                    catch (FormatException) {
                        numObject.SetValue(int.MinValue);
                    }
                    return numObject;
                }

                case PdfTokenizer.PdfTokenType.Other: {
                    return new CMapObject(CMapObject.LITERAL, tokeniser.GetStringValue());
                }

                case PdfTokenizer.PdfTokenType.EndArray: {
                    return new CMapObject(CMapObject.TOKEN, "]");
                }

                case PdfTokenizer.PdfTokenType.EndDic: {
                    return new CMapObject(CMapObject.TOKEN, ">>");
                }

                default: {
                    return new CMapObject(0, "");
                }
            }
        }

        /// <summary>Reads the next token skipping over the comments.</summary>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if a token was read,
        /// <see langword="false"/>
        /// if the end of content was reached.
        /// </returns>
        public virtual bool NextValidToken() {
            while (tokeniser.NextToken()) {
                if (tokeniser.GetTokenType() == PdfTokenizer.PdfTokenType.Comment) {
                    continue;
                }
                return true;
            }
            return false;
        }

        // TODO: Duplicates PdfName.generateValue (REFACTOR)
        protected internal static string DecodeName(byte[] content) {
            var buf = new StringBuilder();
            try {
                for (var k = 0; k < content.Length; ++k) {
                    var c = (char)content[k];
                    if (c == '#') {
                        var c1 = content[k + 1];
                        var c2 = content[k + 2];
                        c = (char)((ByteBuffer.GetHex(c1) << 4) + ByteBuffer.GetHex(c2));
                        k += 2;
                    }
                    buf.Append(c);
                }
            }
            catch (IndexOutOfRangeException) {
            }
            // empty on purpose
            return buf.ToString();
        }

        private static string ToHex4(int n) {
            var s = "0000" + JavaUtil.IntegerToHexString(n);
            return s.Substring(s.Length - 4);
        }

        /// <summary>Gets an hex string in the format "&lt;HHHH&gt;".</summary>
        /// <param name="n">the number</param>
        /// <returns>the hex string</returns>
        public static string ToHex(int n) {
            if (n < 0x10000) {
                return "<" + ToHex4(n) + ">";
            }
            n -= 0x10000;
            var high = n / 0x400 + 0xd800;
            var low = n % 0x400 + 0xdc00;
            return "[<" + ToHex4(high) + ToHex4(low) + ">]";
        }

        public static string DecodeCMapObject(CMapObject cMapObject)
        {
	        if (cMapObject.IsHexString()) {
                return PdfEncodings.ConvertToString(((string)cMapObject.GetValue()).GetBytes(), PdfEncodings.UNICODE_BIG_UNMARKED
                    );
            }

	        return (string)cMapObject.GetValue();
        }
    }
}
