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
    /// Lookup Type 5:
    /// MarkToLigature Attachment Positioning Subtable
    /// </summary>
    public class GposLookupType5 : OpenTableLookup {
        private readonly IList<MarkToLigature> marksligatures;

        public GposLookupType5(OpenTypeFontTableReader openReader, int lookupFlag, int[] subTableLocations)
            : base(openReader, lookupFlag, subTableLocations) {
            marksligatures = new List<MarkToLigature>();
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
            GlyphIndexer ligatureGlyphIndexer = null;
            foreach (var mb in marksligatures) {
                var omr = mb.marks.Get(line.Get(line.idx).GetCode());
                if (omr == null) {
                    continue;
                }
                if (ligatureGlyphIndexer == null) {
                    ligatureGlyphIndexer = new GlyphIndexer();
                    ligatureGlyphIndexer.idx = line.idx;
                    ligatureGlyphIndexer.line = line;
                    while (true) {
                        ligatureGlyphIndexer.PreviousGlyph(openReader, lookupFlag);
                        if (ligatureGlyphIndexer.glyph == null) {
                            break;
                        }
                        // not mark => ligature glyph
                        if (!mb.marks.ContainsKey(ligatureGlyphIndexer.glyph.GetCode())) {
                            break;
                        }
                    }
                    if (ligatureGlyphIndexer.glyph == null) {
                        break;
                    }
                }
                var componentAnchors = mb.ligatures.Get(ligatureGlyphIndexer.glyph.GetCode());
                if (componentAnchors == null) {
                    continue;
                }
                var markClass = omr.markClass;
                // TODO DEVSIX-3732 For complex cases like (glyph1, glyph2, mark, glyph3) and
                //  (glyph1, mark, glyph2, glyph3) when the base glyphs compose a ligature and the mark
                //  is attached to the ligature afterwards, mark should be placed in the corresponding anchor
                //  of that ligature (by finding the right component's anchor).
                //  Excerpt from Microsoft Docs: "For a given mark assigned to a particular class, the appropriate
                //  base attachment point is determined by which ligature component the mark is associated with.
                //  This is dependent on the original character string and subsequent character- or glyph-sequence
                //  processing, not the font data alone. While a text-layout client is performing any character-based
                //  preprocessing or any glyph-substitution operations using the GSUB table, the text-layout client
                //  must keep track of the associations of marks to particular ligature-glyph components."
                //  For now we do not store all the substitution info and therefore not able to follow that logic.
                //  We place the mark symbol in the last available place for now (seems to be better default than
                //  first available place).
                for (var component = componentAnchors.Count - 1; component >= 0; component--) {
                    if (componentAnchors[component][markClass] != null) {
                        var baseAnchor = componentAnchors[component][markClass];
                        var markAnchor = omr.anchor;
                        line.Set(line.idx, new Glyph(line.Get(line.idx), baseAnchor.XCoordinate - markAnchor.XCoordinate, baseAnchor
                            .YCoordinate - markAnchor.YCoordinate, 0, 0, ligatureGlyphIndexer.idx - line.idx));
                        changed = true;
                        break;
                    }
                }
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
            var ligatureCoverageLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var classCount = openReader.rf.ReadUnsignedShort();
            var markArrayLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var ligatureArrayLocation = openReader.rf.ReadUnsignedShort() + subTableLocation;
            var markCoverage = openReader.ReadCoverageFormat(markCoverageLocation);
            var ligatureCoverage = openReader.ReadCoverageFormat(ligatureCoverageLocation);
            var markRecords = OtfReadCommon.ReadMarkArray(openReader, markArrayLocation);
            var markToLigature = new MarkToLigature();
            for (var k = 0; k < markCoverage.Count; ++k) {
                markToLigature.marks.Put(markCoverage[k], markRecords[k]);
            }
            var ligatureArray = OtfReadCommon.ReadLigatureArray(openReader, classCount, ligatureArrayLocation
                );
            for (var k = 0; k < ligatureCoverage.Count; ++k) {
                markToLigature.ligatures.Put(ligatureCoverage[k], ligatureArray[k]);
            }
            marksligatures.Add(markToLigature);
        }

        public class MarkToLigature {
            public readonly IDictionary<int, OtfMarkRecord> marks = new Dictionary<int, OtfMarkRecord>();

            // Glyph id to list of components, each component has a separate list of attachment points
            // defined for different mark classes
            public readonly IDictionary<int, IList<GposAnchor[]>> ligatures = new Dictionary<int, IList<GposAnchor[]>>
                ();
        }
    }
}
