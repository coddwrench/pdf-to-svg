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

using System.Collections.Generic;
using IText.IO.Font;

namespace IText.Kernel.Pdf {
    public class PdfDocumentInfo {
        internal static readonly PdfName[] PDF20_DEPRECATED_KEYS = { PdfName.Title, PdfName.Author, 
            PdfName.Subject, PdfName.Keywords, PdfName.Creator, PdfName.Producer, PdfName.Trapped };

        private PdfDictionary infoDictionary;

        /// <summary>Create a PdfDocumentInfo based on the passed PdfDictionary.</summary>
        /// <param name="pdfObject">PdfDictionary containing PdfDocumentInfo</param>
        internal PdfDocumentInfo(PdfDictionary pdfObject, PdfDocument pdfDocument) {
            infoDictionary = pdfObject;
            if (pdfDocument.GetWriter() != null) {
                infoDictionary.MakeIndirect(pdfDocument);
            }
        }

        /// <summary>Create a default, empty PdfDocumentInfo and link it to the passed PdfDocument</summary>
        /// <param name="pdfDocument">document the info will belong to</param>
        internal PdfDocumentInfo(PdfDocument pdfDocument)
            : this(new PdfDictionary(), pdfDocument) {
        }

        public virtual PdfDocumentInfo SetTitle(string title) {
            return Put(PdfName.Title, new PdfString(title, PdfEncodings.UNICODE_BIG));
        }

        public virtual PdfDocumentInfo SetAuthor(string author) {
            return Put(PdfName.Author, new PdfString(author, PdfEncodings.UNICODE_BIG));
        }

        public virtual PdfDocumentInfo SetSubject(string subject) {
            return Put(PdfName.Subject, new PdfString(subject, PdfEncodings.UNICODE_BIG));
        }

        public virtual PdfDocumentInfo SetKeywords(string keywords) {
            return Put(PdfName.Keywords, new PdfString(keywords, PdfEncodings.UNICODE_BIG));
        }

        public virtual PdfDocumentInfo SetCreator(string creator) {
            return Put(PdfName.Creator, new PdfString(creator, PdfEncodings.UNICODE_BIG));
        }

        public virtual PdfDocumentInfo SetTrapped(PdfName trapped) {
            return Put(PdfName.Trapped, trapped);
        }

        public virtual string GetTitle() {
            return GetStringValue(PdfName.Title);
        }

        public virtual string GetAuthor() {
            return GetStringValue(PdfName.Author);
        }

        public virtual string GetSubject() {
            return GetStringValue(PdfName.Subject);
        }

        public virtual string GetKeywords() {
            return GetStringValue(PdfName.Keywords);
        }

        public virtual string GetCreator() {
            return GetStringValue(PdfName.Creator);
        }

        public virtual string GetProducer() {
            return GetStringValue(PdfName.Producer);
        }

        public virtual PdfName GetTrapped() {
            return infoDictionary.GetAsName(PdfName.Trapped);
        }

        public virtual PdfDocumentInfo AddCreationDate() {
            return Put(PdfName.CreationDate, new PdfDate().GetPdfObject());
        }

        public virtual PdfDocumentInfo AddModDate() {
            return Put(PdfName.ModDate, new PdfDate().GetPdfObject());
        }

        public virtual void SetMoreInfo(IDictionary<string, string> moreInfo) {
            if (moreInfo != null) {
                foreach (var entry in moreInfo) {
                    var key = entry.Key;
                    var value = entry.Value;
                    SetMoreInfo(key, value);
                }
            }
        }

        public virtual void SetMoreInfo(string key, string value) {
            var keyName = new PdfName(key);
            if (value == null) {
                infoDictionary.Remove(keyName);
                infoDictionary.SetModified();
            }
            else {
                Put(keyName, new PdfString(value, PdfEncodings.UNICODE_BIG));
            }
        }

        public virtual string GetMoreInfo(string key) {
            return GetStringValue(new PdfName(key));
        }

        internal virtual PdfDictionary GetPdfObject() {
            return infoDictionary;
        }

        internal virtual PdfDocumentInfo Put(PdfName key, PdfObject value) {
            GetPdfObject().Put(key, value);
            GetPdfObject().SetModified();
            return this;
        }

        private string GetStringValue(PdfName name) {
            var pdfString = infoDictionary.GetAsString(name);
            return pdfString != null ? pdfString.ToUnicodeString() : null;
        }
    }
}
