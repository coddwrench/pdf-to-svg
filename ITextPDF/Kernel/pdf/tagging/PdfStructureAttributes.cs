/*
This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: iText Software.

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

using IText.IO.Font;

namespace IText.Kernel.Pdf.Tagging {
    public class PdfStructureAttributes : PdfObjectWrapper<PdfDictionary> {
        public PdfStructureAttributes(PdfDictionary attributesDict)
            : base(attributesDict) {
        }

        public PdfStructureAttributes(string owner)
            : base(new PdfDictionary()) {
            GetPdfObject().Put(PdfName.O, PdfStructTreeRoot.ConvertRoleToPdfName(owner));
        }

        public PdfStructureAttributes(PdfNamespace @namespace)
            : base(new PdfDictionary()) {
            GetPdfObject().Put(PdfName.O, PdfName.NSO);
            GetPdfObject().Put(PdfName.NS, @namespace.GetPdfObject());
        }

        public virtual PdfStructureAttributes AddEnumAttribute(string attributeName, string
             attributeValue) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            GetPdfObject().Put(name, new PdfName(attributeValue));
            SetModified();
            return this;
        }

        public virtual PdfStructureAttributes AddTextAttribute(string attributeName, string
             attributeValue) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            GetPdfObject().Put(name, new PdfString(attributeValue, PdfEncodings.UNICODE_BIG));
            SetModified();
            return this;
        }

        public virtual PdfStructureAttributes AddIntAttribute(string attributeName, int attributeValue
            ) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            GetPdfObject().Put(name, new PdfNumber(attributeValue));
            SetModified();
            return this;
        }

        public virtual PdfStructureAttributes AddFloatAttribute(string attributeName, float
             attributeValue) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            GetPdfObject().Put(name, new PdfNumber(attributeValue));
            SetModified();
            return this;
        }

        public virtual string GetAttributeAsEnum(string attributeName) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            var attrVal = GetPdfObject().GetAsName(name);
            return attrVal != null ? attrVal.GetValue() : null;
        }

        public virtual string GetAttributeAsText(string attributeName) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            var attrVal = GetPdfObject().GetAsString(name);
            return attrVal != null ? attrVal.ToUnicodeString() : null;
        }

        public virtual int? GetAttributeAsInt(string attributeName) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            var attrVal = GetPdfObject().GetAsNumber(name);
            return attrVal != null ? attrVal.IntValue() : (int?)null;
        }

        public virtual float? GetAttributeAsFloat(string attributeName) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            var attrVal = GetPdfObject().GetAsNumber(name);
            return attrVal != null ? attrVal.FloatValue() : (float?)null;
        }

        public virtual PdfStructureAttributes RemoveAttribute(string attributeName) {
            var name = PdfStructTreeRoot.ConvertRoleToPdfName(attributeName);
            GetPdfObject().Remove(name);
            SetModified();
            return this;
        }

        protected internal override bool IsWrappedObjectMustBeIndirect() {
            return false;
        }
    }
}
