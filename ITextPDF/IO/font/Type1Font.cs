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
using IText.IO.Font.Constants;
using IText.IO.Font.Otf;
using IText.IO.Source;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Font {
    public class Type1Font : FontProgram {
        private Type1Parser fontParser;

        private string characterSet;

        /// <summary>Represents the section KernPairs in the AFM file.</summary>
        /// <remarks>
        /// Represents the section KernPairs in the AFM file.
        /// Key is uni1 &lt;&lt; 32 + uni2. Value is kerning value.
        /// </remarks>
        private IDictionary<long, int?> kernPairs = new Dictionary<long, int?>();

        /// <summary>Types of records in a PFB file.</summary>
        /// <remarks>Types of records in a PFB file. ASCII is 1 and BINARY is 2. They have to appear in the PFB file in this sequence.
        ///     </remarks>
        private static readonly int[] PFB_TYPES = { 1, 2, 1 };

        private byte[] fontStreamBytes;

        private int[] fontStreamLengths;

        protected internal static Type1Font CreateStandardFont(string name)
        {
	        if (StandardFonts.IsStandardFont(name)) {
                return new Type1Font(name, null, null, null);
            }

	        throw new IOException("{0} is not a standard type1 font.").SetMessageParams(name);
        }

        protected internal Type1Font() {
            fontNames = new FontNames();
        }

        protected internal Type1Font(string metricsPath, string binaryPath, byte[] afm, byte[] pfb)
            : this() {
            fontParser = new Type1Parser(metricsPath, binaryPath, afm, pfb);
            Process();
        }

        protected internal Type1Font(string baseFont)
            : this() {
            GetFontNames().SetFontName(baseFont);
        }

        public virtual bool IsBuiltInFont() {
            return fontParser != null && fontParser.IsBuiltInFont();
        }

        public override int GetPdfFontFlags() {
            var flags = 0;
            if (fontMetrics.IsFixedPitch()) {
                flags |= 1;
            }
            flags |= IsFontSpecific() ? 4 : 32;
            if (fontMetrics.GetItalicAngle() < 0) {
                flags |= 64;
            }
            if (fontNames.GetFontName().Contains("Caps") || fontNames.GetFontName().EndsWith("SC")) {
                flags |= 131072;
            }
            if (fontNames.IsBold() || fontNames.GetFontWeight() > 500) {
                flags |= 262144;
            }
            return flags;
        }

        public virtual string GetCharacterSet() {
            return characterSet;
        }

        /// <summary>Checks if the font has any kerning pairs.</summary>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if the font has any kerning pairs.
        /// </returns>
        public override bool HasKernPairs() {
            return kernPairs.Count > 0;
        }

        public override int GetKerning(Glyph first, Glyph second) {
            if (first.HasValidUnicode() && second.HasValidUnicode()) {
                var record = ((long)first.GetUnicode() << 32) + second.GetUnicode();
                if (kernPairs.ContainsKey(record)) {
                    return (int)kernPairs.Get(record);
                }

                return 0;
            }
            return 0;
        }

        /// <summary>Sets the kerning between two Unicode chars.</summary>
        /// <param name="first">the first unicode char.</param>
        /// <param name="second">the second unicode char.</param>
        /// <param name="kern">the kerning to apply in normalized 1000 units.</param>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if the kerning was applied,
        /// <see langword="false"/>
        /// otherwise.
        /// </returns>
        public virtual bool SetKerning(int first, int second, int kern) {
            var record = ((long)first << 32) + second;
            kernPairs.Put(record, kern);
            return true;
        }

        /// <summary>Find glyph by glyph name.</summary>
        /// <param name="name">Glyph name</param>
        /// <returns>Glyph instance if found, otherwise null.</returns>
        public virtual Glyph GetGlyph(string name) {
            var unicode = AdobeGlyphList.NameToUnicode(name);
            if (unicode != -1) {
                return GetGlyph(unicode);
            }

            return null;
        }

        public virtual byte[] GetFontStreamBytes() {
            if (fontParser.IsBuiltInFont()) {
                return null;
            }
            if (fontStreamBytes != null) {
                return fontStreamBytes;
            }
            RandomAccessFileOrArray raf = null;
            try {
                raf = fontParser.GetPostscriptBinary();
                var fileLength = (int)raf.Length();
                fontStreamBytes = new byte[fileLength - 18];
                fontStreamLengths = new int[3];
                var bytePtr = 0;
                for (var k = 0; k < 3; ++k) {
                    if (raf.Read() != 0x80) {
                        var logger = LogManager.GetLogger(typeof(Type1Font));
                        logger.Error(LogMessageConstant.START_MARKER_MISSING_IN_PFB_FILE);
                        return null;
                    }
                    if (raf.Read() != PFB_TYPES[k]) {
                        var logger = LogManager.GetLogger(typeof(Type1Font));
                        logger.Error("incorrect.segment.type.in.pfb.file");
                        return null;
                    }
                    var size = raf.Read();
                    size += raf.Read() << 8;
                    size += raf.Read() << 16;
                    size += raf.Read() << 24;
                    fontStreamLengths[k] = size;
                    while (size != 0) {
                        var got = raf.Read(fontStreamBytes, bytePtr, size);
                        if (got < 0) {
                            var logger = LogManager.GetLogger(typeof(Type1Font));
                            logger.Error("premature.end.in.pfb.file");
                            return null;
                        }
                        bytePtr += got;
                        size -= got;
                    }
                }
                return fontStreamBytes;
            }
            catch (Exception) {
                var logger = LogManager.GetLogger(typeof(Type1Font));
                logger.Error("type1.font.file.exception");
                return null;
            }
            finally {
                if (raf != null) {
                    try {
                        raf.Close();
                    }
                    catch (Exception) {
                    }
                }
            }
        }

        public virtual int[] GetFontStreamLengths() {
            return fontStreamLengths;
        }

        public override bool IsBuiltWith(string fontProgram) {
            return Equals(fontParser.GetAfmPath(), fontProgram);
        }

        protected internal virtual void Process() {
            var raf = fontParser.GetMetricsFile();
            string line;
            var startKernPairs = false;
            while (!startKernPairs && (line = raf.ReadLine()) != null) {
                var tok = new StringTokenizer(line, " ,\n\r\t\f");
                if (!tok.HasMoreTokens()) {
                    continue;
                }
                var ident = tok.NextToken();
                switch (ident) {
                    case "FontName": {
                        fontNames.SetFontName(tok.NextToken("\u00ff").Substring(1));
                        break;
                    }

                    case "FullName": {
                        var fullName = tok.NextToken("\u00ff").Substring(1);
                        fontNames.SetFullName(new[] { new[] { "", "", "", fullName } });
                        break;
                    }

                    case "FamilyName": {
                        var familyName = tok.NextToken("\u00ff").Substring(1);
                        fontNames.SetFamilyName(new[] { new[] { "", "", "", familyName } });
                        break;
                    }

                    case "Weight": {
                        fontNames.SetFontWeight(FontWeights.FromType1FontWeight(tok.NextToken("\u00ff").Substring(1)));
                        break;
                    }

                    case "ItalicAngle": {
                        fontMetrics.SetItalicAngle(float.Parse(tok.NextToken(), CultureInfo.InvariantCulture)
                            );
                        break;
                    }

                    case "IsFixedPitch": {
                        fontMetrics.SetIsFixedPitch(tok.NextToken().Equals("true"));
                        break;
                    }

                    case "CharacterSet": {
                        characterSet = tok.NextToken("\u00ff").Substring(1);
                        break;
                    }

                    case "FontBBox": {
                        var llx = (int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture);
                        var lly = (int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture);
                        var urx = (int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture);
                        var ury = (int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture);
                        fontMetrics.SetBbox(llx, lly, urx, ury);
                        break;
                    }

                    case "UnderlinePosition": {
                        fontMetrics.SetUnderlinePosition((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "UnderlineThickness": {
                        fontMetrics.SetUnderlineThickness((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "EncodingScheme": {
                        encodingScheme = tok.NextToken("\u00ff").Substring(1).Trim();
                        break;
                    }

                    case "CapHeight": {
                        fontMetrics.SetCapHeight((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "XHeight": {
                        fontMetrics.SetXHeight((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "Ascender": {
                        fontMetrics.SetTypoAscender((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "Descender": {
                        fontMetrics.SetTypoDescender((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture
                            ));
                        break;
                    }

                    case "StdHW": {
                        fontMetrics.SetStemH((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture));
                        break;
                    }

                    case "StdVW": {
                        fontMetrics.SetStemV((int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture));
                        break;
                    }

                    case "StartCharMetrics": {
                        startKernPairs = true;
                        break;
                    }
                }
            }
            if (!startKernPairs) {
                var metricsPath = fontParser.GetAfmPath();
                if (metricsPath != null) {
                    throw new IOException("startcharmetrics is missing in {0}.").SetMessageParams(metricsPath);
                }

                throw new IOException("startcharmetrics is missing in the metrics file.");
            }
            avgWidth = 0;
            var widthCount = 0;
            while ((line = raf.ReadLine()) != null) {
                var tok = new StringTokenizer(line);
                if (!tok.HasMoreTokens()) {
                    continue;
                }
                var ident = tok.NextToken();
                if (ident.Equals("EndCharMetrics")) {
                    startKernPairs = false;
                    break;
                }
                var C = -1;
                var WX = 250;
                var N = "";
                int[] B = null;
                tok = new StringTokenizer(line, ";");
                while (tok.HasMoreTokens()) {
                    var tokc = new StringTokenizer(tok.NextToken());
                    if (!tokc.HasMoreTokens()) {
                        continue;
                    }
                    ident = tokc.NextToken();
                    switch (ident) {
                        case "C": {
                            C = Convert.ToInt32(tokc.NextToken(), CultureInfo.InvariantCulture);
                            break;
                        }

                        case "WX": {
                            WX = (int)float.Parse(tokc.NextToken(), CultureInfo.InvariantCulture);
                            break;
                        }

                        case "N": {
                            N = tokc.NextToken();
                            break;
                        }

                        case "B": {
                            B = new[] { Convert.ToInt32(tokc.NextToken(), CultureInfo.InvariantCulture), Convert.ToInt32
                                (tokc.NextToken(), CultureInfo.InvariantCulture), Convert.ToInt32(tokc.NextToken(
                                ), CultureInfo.InvariantCulture), Convert.ToInt32(tokc.NextToken(), CultureInfo.InvariantCulture
                                ) };
                            break;
                        }
                    }
                }
                var unicode = AdobeGlyphList.NameToUnicode(N);
                var glyph = new Glyph(C, WX, unicode, B);
                if (C >= 0) {
                    СodeToGlyph.Put(C, glyph);
                }
                if (unicode != -1) {
                    UnicodeToGlyph.Put(unicode, glyph);
                }
                avgWidth += WX;
                widthCount++;
            }
            if (widthCount != 0) {
                avgWidth /= widthCount;
            }
            if (startKernPairs) {
                var metricsPath = fontParser.GetAfmPath();
                if (metricsPath != null) {
                    throw new IOException("endcharmetrics is missing in {0}.").SetMessageParams(metricsPath);
                }

                throw new IOException("endcharmetrics is missing in the metrics file.");
            }
            // From AdobeGlyphList:
            // nonbreakingspace;00A0
            // space;0020
            if (!UnicodeToGlyph.ContainsKey(0x00A0)) {
                var space = UnicodeToGlyph.Get(0x0020);
                if (space != null) {
                    UnicodeToGlyph.Put(0x00A0, new Glyph(space.GetCode(), space.GetWidth(), 0x00A0, space.GetBbox()));
                }
            }
            var endOfMetrics = false;
            while ((line = raf.ReadLine()) != null) {
                var tok = new StringTokenizer(line);
                if (!tok.HasMoreTokens()) {
                    continue;
                }
                var ident = tok.NextToken();
                if (ident.Equals("EndFontMetrics")) {
                    endOfMetrics = true;
                    break;
                }

                if (ident.Equals("StartKernPairs")) {
	                startKernPairs = true;
	                break;
                }
            }
            if (startKernPairs) {
                while ((line = raf.ReadLine()) != null) {
                    var tok = new StringTokenizer(line);
                    if (!tok.HasMoreTokens()) {
                        continue;
                    }
                    var ident = tok.NextToken();
                    if (ident.Equals("KPX")) {
                        var first = tok.NextToken();
                        var second = tok.NextToken();
                        int? width = (int)float.Parse(tok.NextToken(), CultureInfo.InvariantCulture);
                        var firstUni = AdobeGlyphList.NameToUnicode(first);
                        var secondUni = AdobeGlyphList.NameToUnicode(second);
                        if (firstUni != -1 && secondUni != -1) {
                            var record = ((long)firstUni << 32) + secondUni;
                            kernPairs.Put(record, width);
                        }
                    }
                    else {
                        if (ident.Equals("EndKernPairs")) {
                            startKernPairs = false;
                            break;
                        }
                    }
                }
            }
            else {
                if (!endOfMetrics) {
                    var metricsPath = fontParser.GetAfmPath();
                    if (metricsPath != null) {
                        throw new IOException("endfontmetrics is missing in {0}.").SetMessageParams(metricsPath);
                    }

                    throw new IOException("endfontmetrics is missing in the metrics file.");
                }
            }
            if (startKernPairs) {
                var metricsPath = fontParser.GetAfmPath();
                if (metricsPath != null) {
                    throw new IOException("endkernpairs is missing in {0}.").SetMessageParams(metricsPath);
                }

                throw new IOException("endkernpairs is missing in the metrics file.");
            }
            raf.Close();
            isFontSpecific = !(encodingScheme.Equals("AdobeStandardEncoding") || encodingScheme.Equals("StandardEncoding"
                ));
        }
    }
}
