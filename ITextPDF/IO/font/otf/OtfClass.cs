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

using IText.IO.Source;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Font.Otf {
    public class OtfClass {
        public const int GLYPH_BASE = 1;

        public const int GLYPH_LIGATURE = 2;

        public const int GLYPH_MARK = 3;

        //key is glyph, value is class inside all 2
        private IntHashtable mapClass = new IntHashtable();

        private OtfClass(RandomAccessFileOrArray rf, int classLocation) {
            rf.Seek(classLocation);
            var classFormat = rf.ReadUnsignedShort();
            if (classFormat == 1) {
                var startGlyph = rf.ReadUnsignedShort();
                var glyphCount = rf.ReadUnsignedShort();
                var endGlyph = startGlyph + glyphCount;
                for (var k = startGlyph; k < endGlyph; ++k) {
                    var cl = rf.ReadUnsignedShort();
                    mapClass.Put(k, cl);
                }
            }
            else {
                if (classFormat == 2) {
                    var classRangeCount = rf.ReadUnsignedShort();
                    for (var k = 0; k < classRangeCount; ++k) {
                        var glyphStart = rf.ReadUnsignedShort();
                        var glyphEnd = rf.ReadUnsignedShort();
                        var cl = rf.ReadUnsignedShort();
                        for (; glyphStart <= glyphEnd; ++glyphStart) {
                            mapClass.Put(glyphStart, cl);
                        }
                    }
                }
                else {
                    throw new System.IO.IOException("Invalid class format " + classFormat);
                }
            }
        }

        public static OtfClass Create(RandomAccessFileOrArray rf, int classLocation) {
            OtfClass otfClass;
            try {
                otfClass = new OtfClass(rf, classLocation);
            }
            catch (System.IO.IOException e) {
                var logger = LogManager.GetLogger(typeof(OtfClass));
                logger.Error(MessageFormatUtil.Format(LogMessageConstant.OPENTYPE_GDEF_TABLE_ERROR, e.Message));
                otfClass = null;
            }
            return otfClass;
        }

        public virtual int GetOtfClass(int glyph) {
            return mapClass.Get(glyph);
        }

        public virtual bool IsMarkOtfClass(int glyph) {
            return HasClass(glyph) && GetOtfClass(glyph) == GLYPH_MARK;
        }

        public virtual bool HasClass(int glyph) {
            return mapClass.ContainsKey(glyph);
        }

        public virtual int GetOtfClass(int glyph, bool strict)
        {
	        if (strict)
	        {
		        if (mapClass.ContainsKey(glyph)) {
                    return mapClass.Get(glyph);
                }

		        return -1;
	        }

	        return mapClass.Get(glyph);
        }
    }
}
