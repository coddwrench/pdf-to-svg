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

using System.Diagnostics;
using System.Text;
using IText.IO.Font;
using IText.IO.Source;
using IText.IO.Util;

namespace IText.Kernel.Pdf {
    /// <summary>
    /// A
    /// <c>PdfString</c>
    /// -class is the PDF-equivalent of a
    /// JAVA-
    /// <c>String</c>
    /// -object.
    /// </summary>
    /// <remarks>
    /// A
    /// <c>PdfString</c>
    /// -class is the PDF-equivalent of a
    /// JAVA-
    /// <c>String</c>
    /// -object.
    /// <para />
    /// A string is a sequence of characters delimited by parenthesis.
    /// If a string is too long to be conveniently placed on a single line, it may
    /// be split across multiple lines by using the backslash character (\) at the
    /// end of a line to indicate that the string continues on the following line.
    /// Within a string, the backslash character is used as an escape to specify
    /// unbalanced parenthesis, non-printing ASCII characters, and the backslash
    /// character itself. Use of the \<i>ddd</i> escape sequence is the preferred
    /// way to represent characters outside the printable ASCII character set.<br />
    /// This object is described in the 'Portable Document Format Reference Manual
    /// version 1.7' section 3.2.3 (page 53-56).
    /// </remarks>
    /// <seealso cref="PdfObject"/>
    public class PdfString : PdfPrimitiveObject {
        protected internal string value;

        protected internal string encoding;

        protected internal bool hexWriting;

        private int decryptInfoNum;

        private int decryptInfoGen;

        // if it's not null: content shall contain encrypted data; value shall be null
        private PdfEncryption decryption;

        public PdfString(string value, string encoding)
        {
            Debug.Assert(value != null);
            this.value = value;
            this.encoding = encoding;
        }

        public PdfString(string value)
            : this(value, null) {
        }

        public PdfString(byte[] content)
        {
            if (content != null && content.Length > 0) {
                var str = new StringBuilder(content.Length);
                foreach (var b in content) {
                    str.Append((char)(b & 0xff));
                }
                value = str.ToString();
            }
            else {
                value = "";
            }
        }

        /// <summary>Only PdfReader can use this method!</summary>
        /// <param name="content">
        /// byte content the
        /// <see cref="PdfString"/>
        /// will be created from
        /// </param>
        /// <param name="hexWriting">boolean indicating if hex writing will be used</param>
        protected internal PdfString(byte[] content, bool hexWriting)
            : base(content) {
            this.hexWriting = hexWriting;
        }

        private PdfString()
        {
        }

        public override byte GetObjectType() {
            return STRING;
        }

        public virtual bool IsHexWriting() {
            return hexWriting;
        }

        public virtual PdfString SetHexWriting(bool hexWriting) {
            if (value == null) {
                GenerateValue();
            }
            content = null;
            this.hexWriting = hexWriting;
            return this;
        }

        public virtual string GetValue() {
            if (value == null) {
                GenerateValue();
            }
            return value;
        }

        /// <summary>Gets the encoding of this string.</summary>
        /// <returns>
        /// the name of the encoding specifying the byte representation of current
        /// <see cref="PdfString"/>
        /// value
        /// </returns>
        public virtual string GetEncoding() {
            return encoding;
        }

        /// <summary>
        /// Returns the Unicode
        /// <c>String</c>
        /// value of this
        /// <c>PdfString</c>
        /// -object.
        /// </summary>
        /// <returns>
        /// Unicode string value created by current
        /// <see cref="PdfString"/>
        /// object
        /// </returns>
        public virtual string ToUnicodeString() {
            if (encoding != null && encoding.Length != 0) {
                return GetValue();
            }
            if (content == null) {
                GenerateContent();
            }
            var b = DecodeContent();
            if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) {
                return PdfEncodings.ConvertToString(b, PdfEncodings.UNICODE_BIG);
            }

            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) {
	            return PdfEncodings.ConvertToString(b, PdfEncodings.UTF8);
            }

            return PdfEncodings.ConvertToString(b, PdfEncodings.PDF_DOC_ENCODING);
        }

        /// <summary>Gets bytes of String-value considering encoding.</summary>
        /// <returns>byte array</returns>
        public virtual byte[] GetValueBytes() {
            // Analog of com.itextpdf.text.pdf.PdfString.getBytes() method in iText5.
            if (value == null) {
                GenerateValue();
            }
            if (encoding != null && PdfEncodings.UNICODE_BIG.Equals(encoding) && PdfEncodings.IsPdfDocEncoding(value)) {
                return PdfEncodings.ConvertToBytes(value, PdfEncodings.PDF_DOC_ENCODING);
            }

            return PdfEncodings.ConvertToBytes(value, encoding);
        }

        public override bool Equals(object o) {
            if (this == o) {
                return true;
            }
            if (o == null || GetType() != o.GetType()) {
                return false;
            }
            var that = (PdfString)o;
            var v1 = GetValue();
            var v2 = that.GetValue();
            if (v1 != null && v1.Equals(v2)) {
                var e1 = GetEncoding();
                var e2 = that.GetEncoding();
                if ((e1 == null && e2 == null) || (e1 != null && e1.Equals(e2))) {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
	        if (value == null) {
                return JavaUtil.GetStringForBytes(DecodeContent(), EncodingUtil.ISO_8859_1);
            }

	        return GetValue();
        }

        public override int GetHashCode() {
            var v = GetValue();
            var e = GetEncoding();
            var result = v != null ? v.GetHashCode() : 0;
            return 31 * result + (e != null ? e.GetHashCode() : 0);
        }

        /// <summary>Marks this string object as not encrypted in the encrypted document.</summary>
        /// <remarks>
        /// Marks this string object as not encrypted in the encrypted document.
        /// <para />
        /// If it's marked so, it will be considered as already in plaintext and decryption will not be performed for it.
        /// In order to have effect, this method shall be called before
        /// <see cref="GetValue()"/>
        /// and
        /// <see cref="GetValueBytes()"/>
        /// methods.
        /// <para />
        /// NOTE: this method is only needed in a very specific cases of encrypted documents. E.g. digital signature dictionary
        /// /Contents entry shall not be encrypted. Also this method isn't meaningful in non-encrypted documents.
        /// </remarks>
        public virtual void MarkAsUnencryptedObject() {
            SetState(UNENCRYPTED);
        }

        internal virtual void SetDecryption(int decryptInfoNum, int decryptInfoGen, PdfEncryption decryption) {
            this.decryptInfoNum = decryptInfoNum;
            this.decryptInfoGen = decryptInfoGen;
            this.decryption = decryption;
        }

        protected internal virtual void GenerateValue() {
            Debug.Assert(content != null, "No byte[] content to generate value");
            value = PdfEncodings.ConvertToString(DecodeContent(), null);
            if (decryption != null) {
                decryption = null;
                content = null;
            }
        }

        protected internal override void GenerateContent() {
            content = EncodeBytes(GetValueBytes());
        }

        /// <summary>
        /// Encrypt content of
        /// <c>value</c>
        /// and set as content.
        /// </summary>
        /// <remarks>
        /// Encrypt content of
        /// <c>value</c>
        /// and set as content.
        /// <c>generateContent()</c>
        /// won't be called.
        /// </remarks>
        /// <param name="encrypt">
        /// 
        /// <see cref="PdfEncryption"/>
        /// instance
        /// </param>
        /// <returns>true if value was encrypted, otherwise false.</returns>
        protected internal virtual bool Encrypt(PdfEncryption encrypt) {
            if (CheckState(UNENCRYPTED)) {
                return false;
            }
            if (encrypt != decryption) {
                if (decryption != null) {
                    GenerateValue();
                }
                if (encrypt != null && !encrypt.IsEmbeddedFilesOnly()) {
                    var b = encrypt.EncryptByteArray(GetValueBytes());
                    content = EncodeBytes(b);
                    return true;
                }
            }
            return false;
        }

        protected internal virtual byte[] DecodeContent() {
            var decodedBytes = PdfTokenizer.DecodeStringContent(content, hexWriting);
            if (decryption != null && !CheckState(UNENCRYPTED)) {
                decryption.SetHashKeyForNextObject(decryptInfoNum, decryptInfoGen);
                decodedBytes = decryption.DecryptByteArray(decodedBytes);
            }
            return decodedBytes;
        }

        /// <summary>Escape special symbols or convert to hexadecimal string.</summary>
        /// <remarks>
        /// Escape special symbols or convert to hexadecimal string.
        /// This method don't change either
        /// <c>value</c>
        /// or
        /// <c>content</c>
        /// ot the
        /// <c>PdfString</c>.
        /// </remarks>
        /// <param name="bytes">byte array to manipulate with.</param>
        /// <returns>Hexadecimal string or string with escaped symbols in byte array view.</returns>
        protected internal virtual byte[] EncodeBytes(byte[] bytes) {
            if (hexWriting) {
                var buf = new ByteBuffer(bytes.Length * 2);
                foreach (var b in bytes) {
                    buf.AppendHex(b);
                }
                return buf.GetInternalBuffer();
            }
            else {
                var buf = StreamUtil.CreateBufferedEscapedString(bytes);
                return buf.ToByteArray(1, buf.Size() - 2);
            }
        }

        protected internal override PdfObject NewInstance() {
            return new PdfString();
        }

        protected internal override void CopyContent(PdfObject from, PdfDocument document) {
            base.CopyContent(from, document);
            var @string = (PdfString)from;
            value = @string.value;
            hexWriting = @string.hexWriting;
            decryption = @string.decryption;
            decryptInfoNum = @string.decryptInfoNum;
            decryptInfoGen = @string.decryptInfoGen;
            encoding = @string.encoding;
        }
    }
}
