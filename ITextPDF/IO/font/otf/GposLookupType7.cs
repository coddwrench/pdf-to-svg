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
using IText.IO.Font.Otf.Lookuptype7;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Font.Otf {
    /// <summary>
    /// Lookup Type 7:
    /// Contextual Positioning Subtables
    /// </summary>
    public class GposLookupType7 : OpenTableLookup {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(GposLookupType7));

        private IList<ContextualPositionTable> subTables;

        public GposLookupType7(OpenTypeFontTableReader openReader, int lookupFlag, int[] subTableLocations)
            : base(openReader, lookupFlag, subTableLocations) {
            subTables = new List<ContextualPositionTable>();
            ReadSubTables();
        }

        public override bool TransformOne(GlyphLine line) {
            var changed = false;
            var oldLineStart = line.start;
            var oldLineEnd = line.end;
            var initialLineIndex = line.idx;
            foreach (var subTable in subTables) {
                var contextRule = subTable.GetMatchingContextRule(line);
                if (contextRule == null) {
                    continue;
                }
                var lineEndBeforeTransformations = line.end;
                var posLookupRecords = contextRule.GetPosLookupRecords();
                var gidx = new GlyphIndexer();
                gidx.line = line;
                foreach (var posRecord in posLookupRecords) {
                    // There could be some skipped glyphs inside the context sequence, therefore currently GlyphIndexer and
                    // nextGlyph method are used to get to the glyph at "substRecord.sequenceIndex" index
                    gidx.idx = initialLineIndex;
                    for (var i = 0; i < posRecord.sequenceIndex; ++i) {
                        gidx.NextGlyph(openReader, lookupFlag);
                    }
                    line.idx = gidx.idx;
                    var lookupTable = openReader.GetLookupTable(posRecord.lookupListIndex);
                    changed = lookupTable.TransformOne(line) || changed;
                }
                line.idx = line.end;
                line.start = oldLineStart;
                var lenDelta = lineEndBeforeTransformations - line.end;
                line.end = oldLineEnd - lenDelta;
                return changed;
            }
            line.idx++;
            return changed;
        }

        protected internal override void ReadSubTable(int subTableLocation) {
            openReader.rf.Seek(subTableLocation);
            int substFormat = openReader.rf.ReadShort();
            switch (substFormat) {
                case 2: {
                    ReadSubTableFormat2(subTableLocation);
                    break;
                }

                case 1:
                case 3: {
                    LOGGER.Warn(MessageFormatUtil.Format(LogMessageConstant.GPOS_LOOKUP_SUBTABLE_FORMAT_NOT_SUPPORTED
                        , substFormat, 7));
                    break;
                }

                default: {
                    throw new ArgumentException("Bad subtable format identifier: " + substFormat);
                }
            }
        }

        protected internal virtual void ReadSubTableFormat2(int subTableLocation) {
            var coverageOffset = openReader.rf.ReadUnsignedShort();
            var classDefOffset = openReader.rf.ReadUnsignedShort();
            var posClassSetCount = openReader.rf.ReadUnsignedShort();
            var posClassSetOffsets = openReader.ReadUShortArray(posClassSetCount, subTableLocation);
            ICollection<int> coverageGlyphIds = new HashSet<int>(openReader.ReadCoverageFormat(subTableLocation + coverageOffset
                ));
            var classDefinition = openReader.ReadClassDefinition(subTableLocation + classDefOffset);
            var t = new PosTableLookup7Format2(openReader, lookupFlag, coverageGlyphIds, classDefinition
                );
            IList<IList<ContextualPositionRule>> subClassSets = new List<IList<ContextualPositionRule>>(posClassSetCount
                );
            for (var i = 0; i < posClassSetCount; ++i) {
                IList<ContextualPositionRule> subClassSet = null;
                if (posClassSetOffsets[i] != 0) {
                    openReader.rf.Seek(posClassSetOffsets[i]);
                    var posClassRuleCount = openReader.rf.ReadUnsignedShort();
                    var posClassRuleOffsets = openReader.ReadUShortArray(posClassRuleCount, posClassSetOffsets[i]);
                    subClassSet = new List<ContextualPositionRule>(posClassRuleCount);
                    for (var j = 0; j < posClassRuleCount; ++j) {
                        ContextualPositionRule rule;
                        openReader.rf.Seek(posClassRuleOffsets[j]);
                        var glyphCount = openReader.rf.ReadUnsignedShort();
                        var posCount = openReader.rf.ReadUnsignedShort();
                        var inputClassIds = openReader.ReadUShortArray(glyphCount - 1);
                        var posLookupRecords = openReader.ReadPosLookupRecords(posCount);
                        rule = new PosTableLookup7Format2.PosRuleFormat2(t, inputClassIds, posLookupRecords);
                        subClassSet.Add(rule);
                    }
                }
                subClassSets.Add(subClassSet);
            }
            t.SetPosClassSets(subClassSets);
            subTables.Add(t);
        }
    }
}
