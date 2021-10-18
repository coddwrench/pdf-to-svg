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
using IText.IO.Font.Constants;

namespace  IText.IO.Font {
    /// <summary>Base font descriptor.</summary>
    public class FontProgramDescriptor {
        private readonly string fontName;

        private readonly string fullNameLowerCase;

        private readonly string fontNameLowerCase;

        private readonly string familyNameLowerCase;

        private readonly string style;

        private readonly int macStyle;

        private readonly int weight;

        private readonly float italicAngle;

        private readonly bool isMonospace;

        private readonly ICollection<string> fullNamesAllLangs;

        private readonly ICollection<string> fullNamesEnglishOpenType;

        private readonly string familyNameEnglishOpenType;

        // Initially needed for open type fonts only.
        // The following sequence represents four triplets.
        // In each triplet items sequentially stand for platformID encodingID languageID (see open type naming table spec).
        // Each triplet is used further to determine whether the font name item is represented in English
        private static readonly string[] TT_FAMILY_ORDER = { "3", "1", "1033", "3", "0", "1033", "1", 
            "0", "0", "0", "3", "0" };

        internal FontProgramDescriptor(FontNames fontNames, float italicAngle, bool isMonospace) {
            fontName = fontNames.GetFontName();
            fontNameLowerCase = fontName.ToLowerInvariant();
            fullNameLowerCase = fontNames.GetFullName()[0][3].ToLowerInvariant();
            familyNameLowerCase = fontNames.GetFamilyName() != null && fontNames.GetFamilyName()[0][3] != null ? 
                fontNames.GetFamilyName()[0][3].ToLowerInvariant() : null;
            style = fontNames.GetStyle();
            weight = fontNames.GetFontWeight();
            macStyle = fontNames.GetMacStyle();
            this.italicAngle = italicAngle;
            this.isMonospace = isMonospace;
            familyNameEnglishOpenType = ExtractFamilyNameEnglishOpenType(fontNames);
            fullNamesAllLangs = ExtractFullFontNames(fontNames);
            fullNamesEnglishOpenType = ExtractFullNamesEnglishOpenType(fontNames);
        }

        internal FontProgramDescriptor(FontNames fontNames, FontMetrics fontMetrics)
            : this(fontNames, fontMetrics.GetItalicAngle(), fontMetrics.IsFixedPitch()) {
        }

        public virtual string GetFontName() {
            return fontName;
        }

        public virtual string GetStyle() {
            return style;
        }

        public virtual int GetFontWeight() {
            return weight;
        }

        public virtual float GetItalicAngle() {
            return italicAngle;
        }

        public virtual bool IsMonospace() {
            return isMonospace;
        }

        public virtual bool IsBold() {
            return (macStyle & FontMacStyleFlags.BOLD) != 0;
        }

        public virtual bool IsItalic() {
            return (macStyle & FontMacStyleFlags.ITALIC) != 0;
        }

        public virtual string GetFullNameLowerCase() {
            return fullNameLowerCase;
        }

        public virtual string GetFontNameLowerCase() {
            return fontNameLowerCase;
        }

        public virtual string GetFamilyNameLowerCase() {
            return familyNameLowerCase;
        }

        public virtual ICollection<string> GetFullNameAllLangs() {
            return fullNamesAllLangs;
        }

        public virtual ICollection<string> GetFullNamesEnglishOpenType() {
            return fullNamesEnglishOpenType;
        }

        internal virtual string GetFamilyNameEnglishOpenType() {
            return familyNameEnglishOpenType;
        }

        private ICollection<string> ExtractFullFontNames(FontNames fontNames) {
            ICollection<string> uniqueFullNames = new HashSet<string>();
            foreach (var fullName in fontNames.GetFullName()) {
                uniqueFullNames.Add(fullName[3].ToLowerInvariant());
            }
            return uniqueFullNames;
        }

        private string ExtractFamilyNameEnglishOpenType(FontNames fontNames) {
            if (fontNames.GetFamilyName() != null) {
                for (var k = 0; k < TT_FAMILY_ORDER.Length; k += 3) {
                    foreach (var name in fontNames.GetFamilyName()) {
                        if (TT_FAMILY_ORDER[k].Equals(name[0]) && TT_FAMILY_ORDER[k + 1].Equals(name[1]) && TT_FAMILY_ORDER[k + 2]
                            .Equals(name[2])) {
                            return name[3].ToLowerInvariant();
                        }
                    }
                }
            }
            return null;
        }

        private ICollection<string> ExtractFullNamesEnglishOpenType(FontNames fontNames) {
            if (familyNameEnglishOpenType != null) {
                ICollection<string> uniqueTtfSuitableFullNames = new HashSet<string>();
                var names = fontNames.GetFullName();
                foreach (var name in names) {
                    for (var k = 0; k < TT_FAMILY_ORDER.Length; k += 3) {
                        if (TT_FAMILY_ORDER[k].Equals(name[0]) && TT_FAMILY_ORDER[k + 1].Equals(name[1]) && TT_FAMILY_ORDER[k + 2]
                            .Equals(name[2])) {
                            uniqueTtfSuitableFullNames.Add(name[3]);
                            break;
                        }
                    }
                }
                return uniqueTtfSuitableFullNames;
            }
            return new HashSet<string>();
        }
    }
}
