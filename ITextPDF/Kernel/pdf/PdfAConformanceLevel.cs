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

using IText.Kernel.XMP;
using IText.Kernel.XMP.Properties;

namespace IText.Kernel.Pdf {
    /// <summary>Enumeration of all the PDF/A conformance levels.</summary>
    public class PdfAConformanceLevel {
        public static readonly PdfAConformanceLevel PDF_A_1A = new PdfAConformanceLevel
            ("1", "A");

        public static readonly PdfAConformanceLevel PDF_A_1B = new PdfAConformanceLevel
            ("1", "B");

        public static readonly PdfAConformanceLevel PDF_A_2A = new PdfAConformanceLevel
            ("2", "A");

        public static readonly PdfAConformanceLevel PDF_A_2B = new PdfAConformanceLevel
            ("2", "B");

        public static readonly PdfAConformanceLevel PDF_A_2U = new PdfAConformanceLevel
            ("2", "U");

        public static readonly PdfAConformanceLevel PDF_A_3A = new PdfAConformanceLevel
            ("3", "A");

        public static readonly PdfAConformanceLevel PDF_A_3B = new PdfAConformanceLevel
            ("3", "B");

        public static readonly PdfAConformanceLevel PDF_A_3U = new PdfAConformanceLevel
            ("3", "U");

        private readonly string conformance;

        private readonly string part;

        private PdfAConformanceLevel(string part, string conformance) {
            this.conformance = conformance;
            this.part = part;
        }

        public virtual string GetConformance() {
            return conformance;
        }

        public virtual string GetPart() {
            return part;
        }

        public static PdfAConformanceLevel GetConformanceLevel(string part, string conformance) {
            var lowLetter = conformance.ToUpperInvariant();
            var aLevel = "A".Equals(lowLetter);
            var bLevel = "B".Equals(lowLetter);
            var uLevel = "U".Equals(lowLetter);
            switch (part) {
                case "1": {
                    if (aLevel) {
                        return PDF_A_1A;
                    }
                    if (bLevel) {
                        return PDF_A_1B;
                    }
                    break;
                }

                case "2": {
                    if (aLevel) {
                        return PDF_A_2A;
                    }
                    if (bLevel) {
                        return PDF_A_2B;
                    }
                    if (uLevel) {
                        return PDF_A_2U;
                    }
                    break;
                }

                case "3": {
                    if (aLevel) {
                        return PDF_A_3A;
                    }
                    if (bLevel) {
                        return PDF_A_3B;
                    }
                    if (uLevel) {
                        return PDF_A_3U;
                    }
                    break;
                }
            }
            return null;
        }

        public static PdfAConformanceLevel GetConformanceLevel(XMPMeta meta) {
            XMPProperty conformanceXmpProperty = null;
            XMPProperty partXmpProperty = null;
            try {
                conformanceXmpProperty = meta.GetProperty(XMPConst.NS_PDFA_ID, XMPConst.CONFORMANCE);
                partXmpProperty = meta.GetProperty(XMPConst.NS_PDFA_ID, XMPConst.PART);
            }
            catch (XMPException) {
            }
            if (conformanceXmpProperty == null || partXmpProperty == null) {
                return null;
            }

            var conformance = conformanceXmpProperty.GetValue();
            var part = partXmpProperty.GetValue();
            return GetConformanceLevel(part, conformance);
        }
    }
}
