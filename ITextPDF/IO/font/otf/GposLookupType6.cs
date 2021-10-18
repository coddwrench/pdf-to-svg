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

namespace  IText.IO.Font.Otf {
    /// <summary>
    /// Lookup Type 6:
    /// MarkToMark Attachment Positioning Subtable
    /// </summary>
    public class GposLookupType6 : OpenTableLookup {
        private readonly IList<MarkToBaseMark> marksbases;

        public GposLookupType6(OpenTypeFontTableReader openReader, int lookupFlag, int[] subTableLocations)
            : base(openReader, lookupFlag, subTableLocations) {
            marksbases = new List<MarkToBaseMark>();
            ReadSubTables();
        }

        public override bool TransformOne(GlyphLine line) {
            if (line.idx >= line.end) {
                return false;
            }
            if (openReader.IsSkip(line.Get(line.idx).GetCode(), lookupFlag)) {
                line.idx++;
                return false;
            }
            var changed = false;
            GlyphIndexer gi = null;
            foreach (var mb in marksbases) {
                var omr = mb.marks.Get(line.Get(line.idx).GetCode());
                if (omr == null) {
                    continue;
                }
                if (gi == null) {
                    gi = new GlyphIndexer();
                    gi.idx = line.idx;
                    gi.line = line;
                    while (true) {
                        var prev = gi.idx;
                        // avoid attaching this mark glyph to another very distant mark glyph
                        var foundBaseGlyph = false;
                        gi.PreviousGlyph(openReader, lookupFlag);
                        if (gi.idx != -1) {
                            for (var i = gi.idx; i < prev; i++) {
                                if (openReader.GetGlyphClass(line.Get(i).GetCode()) == OtfClass.GLYPH_BASE) {
                                    foundBaseGlyph = true;
                                    break;
                                }
                            }
                        }
                        if (foundBaseGlyph) {
                            gi.glyph = null;
                            break;
                        }
                        if (gi.glyph == null) {
                            break;
                        }
                        if (mb.baseMarks.ContainsKey(gi.glyph.GetCode())) {
                            break;
                        }
                    }
                    if (gi.glyph == null) {
                        break;
                    }
                }
                var gpas = mb.baseMarks.Get(gi.glyph.GetCode());
                if (gpas == null) {
                    continue;
                }
                var markClass = omr.markClass;
                var baseAnchor = gpas[markClass];
                var markAnchor = omr.anchor;
                line.Set(line.idx, new Glyph(line.Get(line.idx), -markAnchor.XCoordinate + baseAnchor.XCoordinate, -markAnchor
                    .YCoordinate + baseAnchor.YCoordinate, 0, 0, gi.idx - line.idx));
                changed = true;
                break;
            }
            line.idx++;
            return changed;
        }

        protected internal override void ReadSubTable(int subTableLocation) {
            openReader.rf.Seek(subTableLocation);
            // skip format, always 1
            openReader.rf.ReadUnsignedShort();
            var markCoverageLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var baseCoverageLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var classCount = openReader.rf.ReadUnsignedShort();
            var markArrayLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var baseArrayLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var markCoverage = openReader.ReadCoverageFormat(markCoverageLocation);
            var baseCoverage = openReader.ReadCoverageFormat(baseCoverageLocation);
            var markRecords = OtfReadCommon.ReadMarkArray(openReader, markArrayLocation);
            var markToBaseMark = new MarkToBaseMark();
            for (var k = 0; k < markCoverage.Count; ++k) {
                markToBaseMark.marks.Put(markCoverage[k], markRecords[k]);
            }
            var baseArray = OtfReadCommon.ReadBaseArray(openReader, classCount, baseArrayLocation);
            for (var k = 0; k < baseCoverage.Count; ++k) {
                markToBaseMark.baseMarks.Put(baseCoverage[k], baseArray[k]);
            }
            marksbases.Add(markToBaseMark);
        }

        private class MarkToBaseMark {
            public readonly IDictionary<int, OtfMarkRecord> marks = new Dictionary<int, OtfMarkRecord>();

            public readonly IDictionary<int, GposAnchor[]> baseMarks = new Dictionary<int, GposAnchor[]>();
        }
    }
}
