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
using System.Text;
using IText.IO.Util;

namespace  IText.IO.Font.Otf {
    public class GlyphLine {
        public int start;

        public int end;

        public int idx;

        protected internal IList<Glyph> glyphs;

        protected internal IList<ActualText> actualText;

        public GlyphLine() {
            glyphs = new List<Glyph>();
        }

        /// <summary>Create a new line of Glyphs.</summary>
        /// <param name="glyphs">list containing the glyphs</param>
        public GlyphLine(IList<Glyph> glyphs) {
            this.glyphs = glyphs;
            start = 0;
            end = glyphs.Count;
        }

        /// <summary>Create a new line of Glyphs from a slice of a List of Glyphs.</summary>
        /// <param name="glyphs">list of Glyphs to slice</param>
        /// <param name="start">starting index of the slice</param>
        /// <param name="end">terminating index of the slice</param>
        public GlyphLine(IList<Glyph> glyphs, int start, int end) {
            this.glyphs = glyphs;
            this.start = start;
            this.end = end;
        }

        /// <summary>Create a new line of Glyphs from a slice of a List of Glyphs, and add the actual text.</summary>
        /// <param name="glyphs">list of Glyphs to slice</param>
        /// <param name="actualText">corresponding list containing the actual text the glyphs represent</param>
        /// <param name="start">starting index of the slice</param>
        /// <param name="end">terminating index of the slice</param>
        protected internal GlyphLine(IList<Glyph> glyphs, IList<ActualText> actualText, int start, int end
            )
            : this(glyphs, start, end) {
            this.actualText = actualText;
        }

        /// <summary>Copy a line of Glyphs.</summary>
        /// <param name="other">line of Glyphs to copy</param>
        public GlyphLine(GlyphLine other) {
            glyphs = other.glyphs;
            actualText = other.actualText;
            start = other.start;
            end = other.end;
            idx = other.idx;
        }

        /// <summary>Copy a slice of a line of Glyphs</summary>
        /// <param name="other">line of Glyphs to copy</param>
        /// <param name="start">starting index of the slice</param>
        /// <param name="end">terminating index of the slice</param>
        public GlyphLine(GlyphLine other, int start, int end) {
            glyphs = other.glyphs.SubList(start, end);
            if (other.actualText != null) {
                actualText = other.actualText.SubList(start, end);
            }
            this.start = 0;
            this.end = end - start;
            idx = other.idx - start;
        }

        /// <summary>Get the unicode string representation of the GlyphLine slice.</summary>
        /// <param name="start">starting index of the slice</param>
        /// <param name="end">terminating index of the slice</param>
        /// <returns>String containing the unicode representation of the slice.</returns>
        public virtual string ToUnicodeString(int start, int end) {
            var iter = new ActualTextIterator(this, start, end);
            var str = new StringBuilder();
            while (iter.HasNext()) {
                var part = iter.Next();
                if (part.actualText != null) {
                    str.Append(part.actualText);
                }
                else {
                    for (var i = part.start; i < part.end; i++) {
                        str.Append(glyphs[i].GetUnicodeChars());
                    }
                }
            }
            return str.ToString();
        }

        public override string ToString() {
            return ToUnicodeString(start, end);
        }

        /// <summary>Copy a slice of this Glyphline.</summary>
        /// <param name="left">leftmost index of the slice</param>
        /// <param name="right">rightmost index of the slice</param>
        /// <returns>new GlyphLine containing the copied slice</returns>
        public virtual GlyphLine Copy(int left, int right) {
            var glyphLine = new GlyphLine();
            glyphLine.start = 0;
            glyphLine.end = right - left;
            glyphLine.glyphs = new List<Glyph>(glyphs.SubList(left, right));
            glyphLine.actualText = actualText == null ? null : new List<ActualText>(actualText.SubList(left, 
                right));
            return glyphLine;
        }

        public virtual Glyph Get(int index) {
            return glyphs[index];
        }

        public virtual Glyph Set(int index, Glyph glyph) {
            return glyphs[index] = glyph;
        }

        public virtual void Add(Glyph glyph) {
            glyphs.Add(glyph);
            if (actualText != null) {
                actualText.Add(null);
            }
        }

        public virtual void Add(int index, Glyph glyph) {
            glyphs.Add(index, glyph);
            if (actualText != null) {
                actualText.Add(index, null);
            }
        }

        public virtual void SetGlyphs(IList<Glyph> replacementGlyphs) {
            glyphs = new List<Glyph>(replacementGlyphs);
            start = 0;
            end = replacementGlyphs.Count;
            actualText = null;
        }

        /// <summary>Add a line to the current one.</summary>
        /// <remarks>
        /// Add a line to the current one.
        /// The glyphs from the start till the end points will be copied.
        /// The same is true for the actual text.
        /// </remarks>
        /// <param name="other">the line that should be added to the current one</param>
        public virtual void Add(GlyphLine other) {
            if (other.actualText != null) {
                if (actualText == null) {
                    actualText = new List<ActualText>(glyphs.Count);
                    for (var i = 0; i < glyphs.Count; i++) {
                        actualText.Add(null);
                    }
                }
                actualText.AddAll(other.actualText.SubList(other.start, other.end));
            }
            glyphs.AddAll(other.glyphs.SubList(other.start, other.end));
            if (null != actualText) {
                while (actualText.Count < glyphs.Count) {
                    actualText.Add(null);
                }
            }
        }

        /// <summary>Replaces the current content with the other line's content.</summary>
        /// <param name="other">the line with the content to be set to the current one</param>
        public virtual void ReplaceContent(GlyphLine other) {
            glyphs.Clear();
            glyphs.AddAll(other.glyphs);
            if (other.actualText != null) {
                if (actualText == null) {
                    actualText = new List<ActualText>();
                }
                else {
                    actualText.Clear();
                }
                actualText.AddAll(other.actualText);
            }
            else {
                actualText = null;
            }
            start = other.start;
            end = other.end;
        }

        public virtual int Size() {
            return glyphs.Count;
        }

        public virtual void SubstituteManyToOne(OpenTypeFontTableReader tableReader, int lookupFlag, int rightPartLen
            , int substitutionGlyphIndex) {
            var gidx = new OpenTableLookup.GlyphIndexer();
            gidx.line = this;
            gidx.idx = idx;
            var chars = new StringBuilder();
            var currentGlyph = glyphs[idx];
            if (currentGlyph.GetChars() != null) {
                chars.Append(currentGlyph.GetChars());
            }
            else {
                if (currentGlyph.HasValidUnicode()) {
                    chars.Append(TextUtil.ConvertFromUtf32(currentGlyph.GetUnicode()));
                }
            }
            for (var j = 0; j < rightPartLen; ++j) {
                gidx.NextGlyph(tableReader, lookupFlag);
                currentGlyph = glyphs[gidx.idx];
                if (currentGlyph.GetChars() != null) {
                    chars.Append(currentGlyph.GetChars());
                }
                else {
                    if (currentGlyph.HasValidUnicode()) {
                        chars.Append(TextUtil.ConvertFromUtf32(currentGlyph.GetUnicode()));
                    }
                }
                RemoveGlyph(gidx.idx--);
            }
            var newChars = new char[chars.Length];
            chars.GetChars(0, chars.Length, newChars, 0);
            var newGlyph = tableReader.GetGlyph(substitutionGlyphIndex);
            newGlyph.SetChars(newChars);
            glyphs[idx] = newGlyph;
            end -= rightPartLen;
        }

        public virtual void SubstituteOneToOne(OpenTypeFontTableReader tableReader, int substitutionGlyphIndex) {
            var oldGlyph = glyphs[idx];
            var newGlyph = tableReader.GetGlyph(substitutionGlyphIndex);
            if (oldGlyph.GetChars() != null) {
                newGlyph.SetChars(oldGlyph.GetChars());
            }
            else {
                if (newGlyph.HasValidUnicode()) {
                    newGlyph.SetChars(TextUtil.ConvertFromUtf32(newGlyph.GetUnicode()));
                }
                else {
                    if (oldGlyph.HasValidUnicode()) {
                        newGlyph.SetChars(TextUtil.ConvertFromUtf32(oldGlyph.GetUnicode()));
                    }
                }
            }
            glyphs[idx] = newGlyph;
        }

        public virtual void SubstituteOneToMany(OpenTypeFontTableReader tableReader, int[] substGlyphIds) {
            //sequence length shall be at least 1
            var substCode = substGlyphIds[0];
            var oldGlyph = glyphs[idx];
            var glyph = tableReader.GetGlyph(substCode);
            glyphs[idx] = glyph;
            if (substGlyphIds.Length > 1) {
                IList<Glyph> additionalGlyphs = new List<Glyph>(substGlyphIds.Length - 1);
                for (var i = 1; i < substGlyphIds.Length; ++i) {
                    substCode = substGlyphIds[i];
                    glyph = tableReader.GetGlyph(substCode);
                    additionalGlyphs.Add(glyph);
                }
                AddAllGlyphs(idx + 1, additionalGlyphs);
                if (null != actualText) {
                    if (null == actualText[idx]) {
                        actualText[idx] = new ActualText(oldGlyph.GetUnicodeString());
                    }
                    for (var i = 0; i < additionalGlyphs.Count; i++) {
                        actualText[idx + 1 + i] = actualText[idx];
                    }
                }
                idx += substGlyphIds.Length - 1;
                end += substGlyphIds.Length - 1;
            }
        }

        public virtual GlyphLine Filter(IGlyphLineFilter filter) {
            var anythingFiltered = false;
            IList<Glyph> filteredGlyphs = new List<Glyph>(end - start);
            IList<ActualText> filteredActualText = actualText != null ? new List<ActualText>(end -
                 start) : null;
            for (var i = start; i < end; i++) {
                if (filter.Accept(glyphs[i])) {
                    filteredGlyphs.Add(glyphs[i]);
                    if (filteredActualText != null) {
                        filteredActualText.Add(actualText[i]);
                    }
                }
                else {
                    anythingFiltered = true;
                }
            }
            if (anythingFiltered) {
                return new GlyphLine(filteredGlyphs, filteredActualText, 0, filteredGlyphs.Count);
            }

            return this;
        }

        public virtual void SetActualText(int left, int right, string text) {
            if (this.actualText == null) {
                this.actualText = new List<ActualText>(glyphs.Count);
                for (var i = 0; i < glyphs.Count; i++) {
                    this.actualText.Add(null);
                }
            }
            var actualText = new ActualText(text);
            for (var i = left; i < right; i++) {
                this.actualText[i] = actualText;
            }
        }

        public virtual IEnumerator<GlyphLinePart> Iterator() {
            return new ActualTextIterator(this);
        }

        public override bool Equals(object obj) {
            if (this == obj) {
                return true;
            }
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            var other = (GlyphLine)obj;
            if (end - start != other.end - other.start) {
                return false;
            }
            if (actualText == null && other.actualText != null || actualText != null && other.actualText == null) {
                return false;
            }
            for (var i = start; i < end; i++) {
                var otherPos = other.start + i - start;
                var myGlyph = Get(i);
                var otherGlyph = other.Get(otherPos);
                if (myGlyph == null && otherGlyph != null || myGlyph != null && !myGlyph.Equals(otherGlyph)) {
                    return false;
                }
                var myAT = actualText == null ? null : actualText[i];
                var otherAT = other.actualText == null ? null : other.actualText[otherPos];
                if (myAT == null && otherAT != null || myAT != null && !myAT.Equals(otherAT)) {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode() {
            var result = 0;
            result = 31 * result + start;
            result = 31 * result + end;
            for (var i = start; i < end; i++) {
                result = 31 * result + glyphs[i].GetHashCode();
            }
            if (null != actualText) {
                for (var i = start; i < end; i++) {
                    result = 31 * result;
                    if (null != actualText[i]) {
                        result += actualText[i].GetHashCode();
                    }
                }
            }
            return result;
        }

        private void RemoveGlyph(int index) {
            glyphs.JRemoveAt(index);
            if (actualText != null) {
                actualText.JRemoveAt(index);
            }
        }

        private void AddAllGlyphs(int index, IList<Glyph> additionalGlyphs) {
            glyphs.AddAll(index, additionalGlyphs);
            if (actualText != null) {
                for (var i = 0; i < additionalGlyphs.Count; i++) {
                    actualText.Add(index, null);
                }
            }
        }

        public interface IGlyphLineFilter {
            bool Accept(Glyph glyph);
        }

        public class GlyphLinePart {
            public int start;

            public int end;

            // Might be null if it's not necessary
            public string actualText;

            public bool reversed;

            public GlyphLinePart(int start, int end)
                : this(start, end, null) {
            }

            public GlyphLinePart(int start, int end, string actualText) {
                this.start = start;
                this.end = end;
                this.actualText = actualText;
            }

            public virtual GlyphLinePart SetReversed(bool reversed) {
                this.reversed = reversed;
                return this;
            }
        }

        protected internal class ActualText {
            public string value;

            public ActualText(string value) {
                this.value = value;
            }

            public override bool Equals(object obj) {
                if (this == obj) {
                    return true;
                }
                if (obj == null || GetType() != obj.GetType()) {
                    return false;
                }
                var other = (ActualText)obj;
                return value == null && other.value == null || value.Equals(other.value);
            }

            public override int GetHashCode() {
                return 31 * value.GetHashCode();
            }
        }
    }
}
