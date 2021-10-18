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
using System.Linq;
using System.Text.RegularExpressions;
using IText.IO.Util;

namespace IText.Kernel.Utils {
    /// <summary>
    /// Class representing a page range, for instance a page range can contain pages
    /// 5, then pages 10 through 15, then page 18, then page 21 and so on.
    /// </summary>
    public class PageRange {
        private static readonly Regex SEQUENCE_PATTERN = StringUtil.RegexCompile("(\\d+)-(\\d+)?");

        private static readonly Regex SINGLE_PAGE_PATTERN = StringUtil.RegexCompile("(\\d+)");

        private IList<IPageRangePart> sequences = new List<IPageRangePart>();

        /// <summary>
        /// Constructs an empty
        /// <see cref="PageRange"/>
        /// instance.
        /// </summary>
        public PageRange() {
        }

        /// <summary>
        /// Constructs a
        /// <see cref="PageRange"/>
        /// instance from a range in a string form,
        /// for example: "1-12, 15, 45-66".
        /// </summary>
        /// <remarks>
        /// Constructs a
        /// <see cref="PageRange"/>
        /// instance from a range in a string form,
        /// for example: "1-12, 15, 45-66". More advanced forms are also available,
        /// for example:
        /// - "3-" to indicate from page 3 to the last page
        /// - "odd" for all odd pages
        /// - "even" for all even pages
        /// - "3- &amp; odd" for all odd pages starting from page 3
        /// A complete example for pages 1 to 5, page 8 then odd pages starting from
        /// page 9: "1-5, 8, odd &amp; 9-".
        /// </remarks>
        /// <param name="pageRange">a String of page ranges</param>
        public PageRange(string pageRange) {
            pageRange = StringUtil.ReplaceAll(pageRange, "\\s+", "");
            foreach (var pageRangePart in StringUtil.Split(pageRange, ",")) {
                var cond = GetRangeObject(pageRangePart);
                if (cond != null) {
                    sequences.Add(cond);
                }
            }
        }

        private static IPageRangePart GetRangeObject(string rangeDef)
        {
	        if (rangeDef.Contains("&")) {
                IList<IPageRangePart> conditions = new List<IPageRangePart>();
                foreach (var pageRangeCond in StringUtil.Split(rangeDef, "&")) {
                    var cond = GetRangeObject(pageRangeCond);
                    if (cond != null) {
                        conditions.Add(cond);
                    }
                }
                if (conditions.Count > 0) {
                    return new PageRangePartAnd(conditions.ToArray(new IPageRangePart[] {  }));
                }

                return null;
	        }

	        Matcher matcher;
	        if ((matcher = Matcher.Match(SEQUENCE_PATTERN, rangeDef)).Matches()) {
		        var start = Convert.ToInt32(matcher.Group(1), CultureInfo.InvariantCulture);
		        if (matcher.Group(2) != null) {
			        return new PageRangePartSequence(start, Convert.ToInt32(matcher.Group(2), CultureInfo.InvariantCulture
			        ));
		        }

		        return new PageRangePartAfter(start);
	        }

	        if ((matcher = Matcher.Match(SINGLE_PAGE_PATTERN, rangeDef)).Matches()) {
		        return new PageRangePartSingle(Convert.ToInt32(matcher.Group(1), CultureInfo.InvariantCulture
		        ));
	        }

	        if ("odd".EqualsIgnoreCase(rangeDef)) {
		        return PageRangePartOddEven.ODD;
	        }

	        if ("even".EqualsIgnoreCase(rangeDef)) {
		        return PageRangePartOddEven.EVEN;
	        }
	        return null;
        }

        /// <summary>Adds any page range part to this page range.</summary>
        /// <remarks>
        /// Adds any page range part to this page range. Users may define and plug in
        /// custom implementations for behavior not found in the standard library.
        /// </remarks>
        /// <param name="part">
        /// a custom implementation of
        /// <see cref="IPageRangePart"/>
        /// </param>
        /// <returns>this range, already modified</returns>
        public virtual PageRange AddPageRangePart(IPageRangePart part) {
            sequences.Add(part);
            return this;
        }

        /// <summary>Adds a page sequence to the range.</summary>
        /// <param name="startPageNumber">the starting page number of the sequence</param>
        /// <param name="endPageNumber">the finishing page number of the sequence</param>
        /// <returns>this range, already modified</returns>
        public virtual PageRange AddPageSequence(int startPageNumber, int endPageNumber) {
            return AddPageRangePart(new PageRangePartSequence(startPageNumber, endPageNumber));
        }

        /// <summary>Adds a single page to the range.</summary>
        /// <param name="pageNumber">the page number to add</param>
        /// <returns>this range, already modified</returns>
        public virtual PageRange AddSinglePage(int pageNumber) {
            return AddPageRangePart(new PageRangePartSingle(pageNumber));
        }

        /// <summary>Gets the list of pages that have been added to the range so far.</summary>
        /// <param name="nbPages">
        /// number of pages of the document to get the pages, to list
        /// only the pages eligible for this document.
        /// </param>
        /// <returns>
        /// the list containing page numbers added to the range matching this
        /// document
        /// </returns>
        public virtual IList<int> GetQualifyingPageNums(int nbPages) {
            IList<int> allPages = new List<int>();
            foreach (var sequence in sequences) {
                allPages.AddAll(sequence.GetAllPagesInRange(nbPages));
            }
            return allPages;
        }

        /// <summary>Checks if a given page is present in the range built so far.</summary>
        /// <param name="pageNumber">the page number to check</param>
        /// <returns>
        /// <c>true</c> if the page is present in this range,
        /// <c>false</c> otherwise
        /// </returns>
        public virtual bool IsPageInRange(int pageNumber) {
            foreach (var sequence in sequences) {
                if (sequence.IsPageInRange(pageNumber)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary><inheritDoc/></summary>
        public override bool Equals(object obj) {
            if (!(obj is PageRange)) {
                return false;
            }
            var other = (PageRange)obj;
            return sequences.SequenceEqual(other.sequences);
        }

        /// <summary><inheritDoc/></summary>
        public override int GetHashCode() {
            var hashCode = 0;
            foreach (var part in sequences) {
                hashCode += part.GetHashCode();
            }
            return hashCode;
        }

        /// <summary>Inner interface for range parts definition</summary>
        public interface IPageRangePart {
            //public List<Integer> getAllPages();
            IList<int> GetAllPagesInRange(int nbPages);

            bool IsPageInRange(int pageNumber);
        }

        /// <summary>Class for range part containing a single page</summary>
        public class PageRangePartSingle : IPageRangePart {
            private readonly int page;

            public PageRangePartSingle(int page) {
                this.page = page;
            }

            public virtual IList<int> GetAllPagesInRange(int nbPages)
            {
	            if (page <= nbPages) {
                    return JavaCollectionsUtil.SingletonList(page);
                }

	            return JavaCollectionsUtil.EmptyList<int>();
            }

            public virtual bool IsPageInRange(int pageNumber) {
                return page == pageNumber;
            }

            /// <summary><inheritDoc/></summary>
            public override bool Equals(object obj) {
                if (!(obj is PageRangePartSingle)) {
                    return false;
                }
                var other = (PageRangePartSingle)obj;
                return page == other.page;
            }

            /// <summary><inheritDoc/></summary>
            public override int GetHashCode() {
                return page;
            }
        }

        /// <summary>
        /// Class for range part containing a range of pages represented by a start
        /// and an end page
        /// </summary>
        public class PageRangePartSequence : IPageRangePart {
            private readonly int start;

            private readonly int end;

            public PageRangePartSequence(int start, int end) {
                this.start = start;
                this.end = end;
            }

            public virtual IList<int> GetAllPagesInRange(int nbPages) {
                IList<int> allPages = new List<int>();
                for (var pageInRange = start; pageInRange <= end && pageInRange <= nbPages; pageInRange++) {
                    allPages.Add(pageInRange);
                }
                return allPages;
            }

            public virtual bool IsPageInRange(int pageNumber) {
                return start <= pageNumber && pageNumber <= end;
            }

            /// <summary><inheritDoc/></summary>
            public override bool Equals(object obj) {
                if (!(obj is PageRangePartSequence)) {
                    return false;
                }
                var other = (PageRangePartSequence)obj;
                return start == other.start && end == other.end;
            }

            /// <summary><inheritDoc/></summary>
            public override int GetHashCode() {
                return start * 31 + end;
            }
        }

        /// <summary>
        /// Class for range part containing a range of pages for all pages after a
        /// given start page
        /// </summary>
        public class PageRangePartAfter : IPageRangePart {
            private readonly int start;

            public PageRangePartAfter(int start) {
                this.start = start;
            }

            public virtual IList<int> GetAllPagesInRange(int nbPages) {
                IList<int> allPages = new List<int>();
                for (var pageInRange = start; pageInRange <= nbPages; pageInRange++) {
                    allPages.Add(pageInRange);
                }
                return allPages;
            }

            public virtual bool IsPageInRange(int pageNumber) {
                return start <= pageNumber;
            }

            /// <summary><inheritDoc/></summary>
            public override bool Equals(object obj) {
                if (!(obj is PageRangePartAfter)) {
                    return false;
                }
                var other = (PageRangePartAfter)obj;
                return start == other.start;
            }

            /// <summary><inheritDoc/></summary>
            public override int GetHashCode() {
                return start * 31 + -1;
            }
        }

        /// <summary>Class for range part for all even or odd pages.</summary>
        /// <remarks>
        /// Class for range part for all even or odd pages. The class contains only 2
        /// instances, one for odd pages and one for even pages.
        /// </remarks>
        public class PageRangePartOddEven : IPageRangePart {
            private readonly bool isOdd;

            private readonly int mod;

            public static readonly PageRangePartOddEven ODD = new PageRangePartOddEven(true);

            public static readonly PageRangePartOddEven EVEN = new PageRangePartOddEven(false);

            private PageRangePartOddEven(bool isOdd) {
                this.isOdd = isOdd;
                if (isOdd) {
                    mod = 1;
                }
                else {
                    mod = 0;
                }
            }

            public virtual IList<int> GetAllPagesInRange(int nbPages) {
                IList<int> allPages = new List<int>();
                for (var pageInRange = (mod == 0 ? 2 : mod); pageInRange <= nbPages; pageInRange += 2) {
                    allPages.Add(pageInRange);
                }
                return allPages;
            }

            public virtual bool IsPageInRange(int pageNumber) {
                return pageNumber % 2 == mod;
            }

            /// <summary><inheritDoc/></summary>
            public override bool Equals(object obj) {
                if (!(obj is PageRangePartOddEven)) {
                    return false;
                }
                var other = (PageRangePartOddEven)obj;
                return isOdd == other.isOdd;
            }

            /// <summary><inheritDoc/></summary>
            public override int GetHashCode() {
                if (isOdd) {
                    return 127;
                }
                return 128;
            }
        }

        /// <summary>Class for range part based on several range parts.</summary>
        /// <remarks>
        /// Class for range part based on several range parts. A 'and' is performed
        /// between all conditions. This allows for example to configure odd pages
        /// between page 19 and 25.
        /// </remarks>
        public class PageRangePartAnd : IPageRangePart {
            private readonly IList<IPageRangePart> conditions = new List<IPageRangePart>();

            public PageRangePartAnd(params IPageRangePart[] conditions) {
                this.conditions.AddAll(JavaUtil.ArraysAsList(conditions));
            }

            public virtual IList<int> GetAllPagesInRange(int nbPages) {
                IList<int> allPages = new List<int>();
                if (!conditions.IsEmpty()) {
                    allPages.AddAll(conditions[0].GetAllPagesInRange(nbPages));
                }
                foreach (var cond in conditions) {
                    allPages.RetainAll(cond.GetAllPagesInRange(nbPages));
                }
                return allPages;
            }

            public virtual bool IsPageInRange(int pageNumber) {
                foreach (var cond in conditions) {
                    if (!cond.IsPageInRange(pageNumber)) {
                        return false;
                    }
                }
                return true;
            }

            /// <summary><inheritDoc/></summary>
            public override bool Equals(object obj) {
                if (!(obj is PageRangePartAnd)) {
                    return false;
                }
                var other = (PageRangePartAnd)obj;
                return conditions.SequenceEqual(other.conditions);
            }

            /// <summary><inheritDoc/></summary>
            public override int GetHashCode() {
                var hashCode = 0;
                foreach (var part in conditions) {
                    hashCode += part.GetHashCode();
                }
                return hashCode;
            }
        }
    }
}
