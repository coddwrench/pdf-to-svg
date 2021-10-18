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
    /// <summary>LookupType 4: Ligature Substitution Subtable</summary>
    /// <author>psoares</author>
    public class GsubLookupType4 : OpenTableLookup {
        /// <summary>The key is the first character.</summary>
        /// <remarks>
        /// The key is the first character. The first element in the int array is the
        /// output ligature
        /// </remarks>
        private IDictionary<int, IList<int[]>> ligatures;

        public GsubLookupType4(OpenTypeFontTableReader openReader, int lookupFlag, int[] subTableLocations)
            : base(openReader, lookupFlag, subTableLocations) {
            ligatures = new Dictionary<int, IList<int[]>>();
            ReadSubTables();
        }

        public override bool TransformOne(GlyphLine line) {
            //TODO >
            if (line.idx >= line.end) {
                return false;
            }
            var changed = false;
            var g = line.Get(line.idx);
            var match = false;
            if (ligatures.ContainsKey(g.GetCode()) && !openReader.IsSkip(g.GetCode(), lookupFlag)) {
                var gidx = new GlyphIndexer();
                gidx.line = line;
                var ligs = ligatures.Get(g.GetCode());
                foreach (var lig in ligs) {
                    match = true;
                    gidx.idx = line.idx;
                    for (var j = 1; j < lig.Length; ++j) {
                        gidx.NextGlyph(openReader, lookupFlag);
                        if (gidx.glyph == null || gidx.glyph.GetCode() != lig[j]) {
                            match = false;
                            break;
                        }
                    }
                    if (match) {
                        line.SubstituteManyToOne(openReader, lookupFlag, lig.Length - 1, lig[0]);
                        break;
                    }
                }
            }
            if (match) {
                changed = true;
            }
            line.idx++;
            return changed;
        }

        protected internal override void ReadSubTable(int subTableLocation) {
            openReader.rf.Seek(subTableLocation);
            // subformat - always 1
            openReader.rf.ReadShort();
            var coverage = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var ligSetCount = openReader.rf.ReadUnsignedShort();
            var ligatureSet = new int[ligSetCount];
            for (var k = 0; k < ligSetCount; ++k) {
                ligatureSet[k] = openReader.rf.ReadUnsignedShort() + subTableLocation;
            }
            var coverageGlyphIds = openReader.ReadCoverageFormat(coverage);
            for (var k = 0; k < ligSetCount; ++k) {
                openReader.rf.Seek(ligatureSet[k]);
                var ligatureCount = openReader.rf.ReadUnsignedShort();
                var ligature = new int[ligatureCount];
                for (var j = 0; j < ligatureCount; ++j) {
                    ligature[j] = openReader.rf.ReadUnsignedShort() + ligatureSet[k];
                }
                IList<int[]> components = new List<int[]>(ligatureCount);
                for (var j = 0; j < ligatureCount; ++j) {
                    openReader.rf.Seek(ligature[j]);
                    var ligGlyph = openReader.rf.ReadUnsignedShort();
                    var compCount = openReader.rf.ReadUnsignedShort();
                    var component = new int[compCount];
                    component[0] = ligGlyph;
                    for (var i = 1; i < compCount; ++i) {
                        component[i] = openReader.rf.ReadUnsignedShort();
                    }
                    components.Add(component);
                }
                ligatures.Put(coverageGlyphIds[k], components);
            }
        }
    }
}
