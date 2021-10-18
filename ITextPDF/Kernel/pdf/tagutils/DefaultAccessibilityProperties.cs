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
using IText.IO.Util;
using IText.Kernel.Pdf.Tagging;

namespace IText.Kernel.Pdf.Tagutils {
    public class DefaultAccessibilityProperties : AccessibilityProperties {
        protected internal string role;

        protected internal string language;

        protected internal string actualText;

        protected internal string alternateDescription;

        protected internal string expansion;

        protected internal IList<PdfStructureAttributes> attributesList = new List<PdfStructureAttributes>();

        protected internal string phoneme;

        protected internal string phoneticAlphabet;

        protected internal PdfNamespace @namespace;

        protected internal IList<TagTreePointer> refs = new List<TagTreePointer>();

        public DefaultAccessibilityProperties(string role) {
            this.role = role;
        }

        public override string GetRole() {
            return role;
        }

        public override AccessibilityProperties SetRole(string role) {
            this.role = role;
            return this;
        }

        public override string GetLanguage() {
            return language;
        }

        public override AccessibilityProperties SetLanguage(string language) {
            this.language = language;
            return this;
        }

        public override string GetActualText() {
            return actualText;
        }

        public override AccessibilityProperties SetActualText(string actualText) {
            this.actualText = actualText;
            return this;
        }

        public override string GetAlternateDescription() {
            return alternateDescription;
        }

        public override AccessibilityProperties SetAlternateDescription(string alternateDescription) {
            this.alternateDescription = alternateDescription;
            return this;
        }

        public override string GetExpansion() {
            return expansion;
        }

        public override AccessibilityProperties SetExpansion(string expansion) {
            this.expansion = expansion;
            return this;
        }

        public override AccessibilityProperties AddAttributes(PdfStructureAttributes attributes) {
            return AddAttributes(-1, attributes);
        }

        public override AccessibilityProperties AddAttributes(int index, PdfStructureAttributes attributes) {
            if (attributes != null) {
                if (index > 0) {
                    attributesList.Add(index, attributes);
                }
                else {
                    attributesList.Add(attributes);
                }
            }
            return this;
        }

        public override AccessibilityProperties ClearAttributes() {
            attributesList.Clear();
            return this;
        }

        public override IList<PdfStructureAttributes> GetAttributesList() {
            return attributesList;
        }

        public override string GetPhoneme() {
            return phoneme;
        }

        public override AccessibilityProperties SetPhoneme(string phoneme) {
            this.phoneme = phoneme;
            return this;
        }

        public override string GetPhoneticAlphabet() {
            return phoneticAlphabet;
        }

        public override AccessibilityProperties SetPhoneticAlphabet(string phoneticAlphabet) {
            this.phoneticAlphabet = phoneticAlphabet;
            return this;
        }

        public override PdfNamespace GetNamespace() {
            return @namespace;
        }

        public override AccessibilityProperties SetNamespace(PdfNamespace @namespace) {
            this.@namespace = @namespace;
            return this;
        }

        public override AccessibilityProperties AddRef(TagTreePointer treePointer) {
            refs.Add(new TagTreePointer(treePointer));
            return this;
        }

        public override IList<TagTreePointer> GetRefsList() {
            return JavaCollectionsUtil.UnmodifiableList(refs);
        }

        public override AccessibilityProperties ClearRefs() {
            refs.Clear();
            return this;
        }
    }
}
