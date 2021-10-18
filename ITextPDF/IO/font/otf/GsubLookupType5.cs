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
using IText.IO.Font.Otf.Lookuptype5;

namespace  IText.IO.Font.Otf {
    /// <summary>LookupType 5: Contextual Substitution Subtable</summary>
    public class GsubLookupType5 : OpenTableLookup {
        protected internal IList<ContextualSubTable> subTables;

        protected internal GsubLookupType5(OpenTypeFontTableReader openReader, int lookupFlag, int[] subTableLocations
            )
            : base(openReader, lookupFlag, subTableLocations) {
            subTables = new List<ContextualSubTable>();
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
                var lineEndBeforeSubstitutions = line.end;
                var substLookupRecords = contextRule.GetSubstLookupRecords();
                var gidx = new GlyphIndexer();
                gidx.line = line;
                foreach (var substRecord in substLookupRecords) {
                    // There could be some skipped glyphs inside the context sequence, therefore currently GlyphIndexer and
                    // nextGlyph method are used to get to the glyph at "substRecord.sequenceIndex" index
                    gidx.idx = initialLineIndex;
                    for (var i = 0; i < substRecord.sequenceIndex; ++i) {
                        gidx.NextGlyph(openReader, lookupFlag);
                    }
                    line.idx = gidx.idx;
                    var lookupTable = openReader.GetLookupTable(substRecord.lookupListIndex);
                    changed = lookupTable.TransformOne(line) || changed;
                }
                line.idx = line.end;
                line.start = oldLineStart;
                var lenDelta = lineEndBeforeSubstitutions - line.end;
                line.end = oldLineEnd - lenDelta;
                return changed;
            }
            ++line.idx;
            return changed;
        }

        protected internal override void ReadSubTable(int subTableLocation) {
            openReader.rf.Seek(subTableLocation);
            int substFormat = openReader.rf.ReadShort();
            if (substFormat == 1) {
                ReadSubTableFormat1(subTableLocation);
            }
            else {
                if (substFormat == 2) {
                    ReadSubTableFormat2(subTableLocation);
                }
                else {
                    if (substFormat == 3) {
                        ReadSubTableFormat3(subTableLocation);
                    }
                    else {
                        throw new ArgumentException("Bad substFormat: " + substFormat);
                    }
                }
            }
        }

        protected internal virtual void ReadSubTableFormat1(int subTableLocation) {
            IDictionary<int, IList<ContextualSubstRule>> substMap = new Dictionary<int, IList<ContextualSubstRule>>();
            var coverageOffset = openReader.rf.ReadUnsignedShort();
            var subRuleSetCount = openReader.rf.ReadUnsignedShort();
            var subRuleSetOffsets = openReader.ReadUShortArray(subRuleSetCount, subTableLocation);
            var coverageGlyphIds = openReader.ReadCoverageFormat(subTableLocation + coverageOffset);
            for (var i = 0; i < subRuleSetCount; ++i) {
                openReader.rf.Seek(subRuleSetOffsets[i]);
                var subRuleCount = openReader.rf.ReadUnsignedShort();
                var subRuleOffsets = openReader.ReadUShortArray(subRuleCount, subRuleSetOffsets[i]);
                IList<ContextualSubstRule> subRuleSet = new List<ContextualSubstRule>(subRuleCount);
                for (var j = 0; j < subRuleCount; ++j) {
                    openReader.rf.Seek(subRuleOffsets[j]);
                    var glyphCount = openReader.rf.ReadUnsignedShort();
                    var substCount = openReader.rf.ReadUnsignedShort();
                    var inputGlyphIds = openReader.ReadUShortArray(glyphCount - 1);
                    var substLookupRecords = openReader.ReadSubstLookupRecords(substCount);
                    subRuleSet.Add(new SubTableLookup5Format1.SubstRuleFormat1(inputGlyphIds, substLookupRecords));
                }
                substMap.Put(coverageGlyphIds[i], subRuleSet);
            }
            subTables.Add(new SubTableLookup5Format1(openReader, lookupFlag, substMap));
        }

        protected internal virtual void ReadSubTableFormat2(int subTableLocation) {
            var coverageOffset = openReader.rf.ReadUnsignedShort();
            var classDefOffset = openReader.rf.ReadUnsignedShort();
            var subClassSetCount = openReader.rf.ReadUnsignedShort();
            var subClassSetOffsets = openReader.ReadUShortArray(subClassSetCount, subTableLocation);
            ICollection<int> coverageGlyphIds = new HashSet<int>(openReader.ReadCoverageFormat(subTableLocation + coverageOffset
                ));
            var classDefinition = openReader.ReadClassDefinition(subTableLocation + classDefOffset);
            var t = new SubTableLookup5Format2(openReader, lookupFlag, coverageGlyphIds, classDefinition
                );
            IList<IList<ContextualSubstRule>> subClassSets = new List<IList<ContextualSubstRule>>(subClassSetCount);
            for (var i = 0; i < subClassSetCount; ++i) {
                IList<ContextualSubstRule> subClassSet = null;
                if (subClassSetOffsets[i] != 0) {
                    openReader.rf.Seek(subClassSetOffsets[i]);
                    var subClassRuleCount = openReader.rf.ReadUnsignedShort();
                    var subClassRuleOffsets = openReader.ReadUShortArray(subClassRuleCount, subClassSetOffsets[i]);
                    subClassSet = new List<ContextualSubstRule>(subClassRuleCount);
                    for (var j = 0; j < subClassRuleCount; ++j) {
                        ContextualSubstRule rule;
                        openReader.rf.Seek(subClassRuleOffsets[j]);
                        var glyphCount = openReader.rf.ReadUnsignedShort();
                        var substCount = openReader.rf.ReadUnsignedShort();
                        var inputClassIds = openReader.ReadUShortArray(glyphCount - 1);
                        var substLookupRecords = openReader.ReadSubstLookupRecords(substCount);
                        rule = new SubTableLookup5Format2.SubstRuleFormat2(t, inputClassIds, substLookupRecords);
                        subClassSet.Add(rule);
                    }
                }
                subClassSets.Add(subClassSet);
            }
            t.SetSubClassSets(subClassSets);
            subTables.Add(t);
        }

        protected internal virtual void ReadSubTableFormat3(int subTableLocation) {
            var glyphCount = openReader.rf.ReadUnsignedShort();
            var substCount = openReader.rf.ReadUnsignedShort();
            var coverageOffsets = openReader.ReadUShortArray(glyphCount, subTableLocation);
            var substLookupRecords = openReader.ReadSubstLookupRecords(substCount);
            IList<ICollection<int>> coverages = new List<ICollection<int>>(glyphCount);
            openReader.ReadCoverages(coverageOffsets, coverages);
            var rule = new SubTableLookup5Format3.SubstRuleFormat3(coverages, substLookupRecords
                );
            subTables.Add(new SubTableLookup5Format3(openReader, lookupFlag, rule));
        }
    }
}
