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
using System.IO;
using IText.IO.Source;
using IText.Logger;

namespace  IText.IO.Codec
{
	/// <summary>
	/// Class to read a JBIG2 file at a basic level: understand all the segments,
	/// understand what segments belong to which pages, how many pages there are,
	/// what the width and height of each page is, and global segments if there
	/// are any.
	/// </summary>
	/// <remarks>
	/// Class to read a JBIG2 file at a basic level: understand all the segments,
	/// understand what segments belong to which pages, how many pages there are,
	/// what the width and height of each page is, and global segments if there
	/// are any.  Or: the minimum required to be able to take a normal sequential
	/// or random-access organized file, and be able to embed JBIG2 pages as images
	/// in a PDF.
	/// TODO: the indeterminate-segment-size value of dataLength, else?
	/// </remarks>
	public class Jbig2SegmentReader
	{
		//see 7.4.2.
		public const int SymbolDictionary = 0;

		//see 7.4.3.
		public const int IntermediateTextRegion = 4;

		//see 7.4.3.//see 7.4.3.
		public const int ImmediateTextRegion = 6;

		//see 7.4.3.
		public const int ImmediateLosslessTextRegion = 7;

		//see 7.4.4.
		public const int PatternDictionary = 16;

		//see 7.4.5.
		public const int IntermediateHalftoneRegion = 20;

		//see 7.4.5.
		public const int ImmediateHalftoneRegion = 22;

		//see 7.4.5.
		public const int ImmediateLosslessHalftoneRegion = 23;

		//see 7.4.6.
		public const int IntermediateGenericRegion = 36;

		//see 7.4.6.
		public const int ImmediateGenericRegion = 38;

		//see 7.4.6.
		public const int ImmediateLosslessGenericRegion = 39;

		//see 7.4.7.
		public const int IntermediateGenericRefinementRegion = 40;

		//see 7.4.7.
		public const int ImmediateGenericRefinementRegion = 42;

		//see 7.4.7.
		public const int ImmediateLosslessGenericRefinementRegion = 43;

		//see 7.4.8.
		public const int PageInformation = 48;

		//see 7.4.9.
		public const int EndOfPage = 49;

		//see 7.4.10.
		public const int EndOfStripe = 50;

		//see 7.4.11.
		public const int EndOfFile = 51;

		//see 7.4.12.
		public const int Profiles = 52;

		//see 7.4.13.
		public const int Tables = 53;

		//see 7.4.14.
		public const int Extension = 62;

		private readonly IDictionary<int, Jbig2Segment> _segments = new SortedDictionary<int, Jbig2Segment
			>();

		private readonly IDictionary<int, Jbig2Page> _pages = new SortedDictionary<int, Jbig2Page
			>();

		private readonly ICollection<Jbig2Segment> _globals = new SortedSet<Jbig2Segment
			>();

		private RandomAccessFileOrArray _ra;

		private bool _sequential;

		private bool _numberOfPagesKnown;

		private int _numberOfPages = -1;

		private bool _read;

		/// <summary>Inner class that holds information about a JBIG2 segment.</summary>
		public class Jbig2Segment : IComparable<Jbig2Segment>
		{
			public readonly int SegmentNumber;

			public long DataLength = -1;

			public int Page = -1;

			public int[] ReferredToSegmentNumbers;

			public bool[] SegmentRetentionFlags;

			public int Type = -1;

			public bool DeferredNonRetain;

			public int CountOfReferredToSegments = -1;

			public byte[] Data;

			public byte[] HeaderData;

			public bool PageAssociationSize;

			public int PageAssociationOffset = -1;

			public Jbig2Segment(int segmentNumber)
			{
				SegmentNumber = segmentNumber;
			}

			public virtual int CompareTo(Jbig2Segment s)
			{
				return SegmentNumber - s.SegmentNumber;
			}
		}

		/// <summary>Inner class that holds information about a JBIG2 page.</summary>
		public class Jbig2Page
		{
			public readonly int Page;

			private readonly Jbig2SegmentReader _sr;

			private readonly IDictionary<int, Jbig2Segment> _segs = new SortedDictionary<int, Jbig2Segment
				>();

			public int PageBitmapWidth = -1;

			public int PageBitmapHeight = -1;

			public Jbig2Page(int page, Jbig2SegmentReader sr)
			{
				this.Page = page;
				this._sr = sr;
			}

			/// <summary>
			/// return as a single byte array the header-data for each segment in segment number
			/// order, EMBEDDED organization, but I am putting the needed segments in SEQUENTIAL organization.
			/// </summary>
			/// <remarks>
			/// return as a single byte array the header-data for each segment in segment number
			/// order, EMBEDDED organization, but I am putting the needed segments in SEQUENTIAL organization.
			/// if for_embedding, skip the segment types that are known to be not for acrobat.
			/// </remarks>
			/// <param name="forEmbedding">True if the bytes represents embedded data, false otherwise</param>
			/// <returns>a byte array</returns>
			public virtual byte[] GetData(bool forEmbedding)
			{
				var os = new MemoryStream();
				foreach (var sn in _segs.Keys)
				{
					var s = _segs.Get(sn);
					// pdf reference 1.4, section 3.3.6 Jbig2Decode Filter
					// D.3 Embedded organisation
					if (forEmbedding && (s.Type == EndOfFile || s.Type == EndOfPage))
					{
						continue;
					}
					if (forEmbedding)
					{
						// change the page association to page 1
						var headerDataEmb = CopyByteArray(s.HeaderData);
						if (s.PageAssociationSize)
						{
							headerDataEmb[s.PageAssociationOffset] = 0x0;
							headerDataEmb[s.PageAssociationOffset + 1] = 0x0;
							headerDataEmb[s.PageAssociationOffset + 2] = 0x0;
							headerDataEmb[s.PageAssociationOffset + 3] = 0x1;
						}
						else
						{
							headerDataEmb[s.PageAssociationOffset] = 0x1;
						}
						os.Write(headerDataEmb);
					}
					else
					{
						os.Write(s.HeaderData);
					}
					os.Write(s.Data);
				}
				os.Dispose();
				return os.ToArray();
			}

			public virtual void AddSegment(Jbig2Segment s)
			{
				_segs.Put(s.SegmentNumber, s);
			}
		}

		public Jbig2SegmentReader(RandomAccessFileOrArray ra)
		{
			this._ra = ra;
		}

		public static byte[] CopyByteArray(byte[] b)
		{
			var bc = new byte[b.Length];
			Array.Copy(b, 0, bc, 0, b.Length);
			return bc;
		}

		public virtual void Read()
		{
			if (_read)
			{
				throw new InvalidOperationException("already.attempted.a.read.on.this.jbig2.file");
			}
			_read = true;
			ReadFileHeader();
			// Annex D
			if (_sequential)
			{
				// D.1
				do
				{
					var tmp = ReadHeader();
					ReadSegment(tmp);
					_segments.Put(tmp.SegmentNumber, tmp);
				}
				while (_ra.GetPosition() < _ra.Length());
			}
			else
			{
				// D.2
				Jbig2Segment tmp;
				do
				{
					tmp = ReadHeader();
					_segments.Put(tmp.SegmentNumber, tmp);
				}
				while (tmp.Type != EndOfFile);
				foreach (var integer in _segments.Keys)
				{
					ReadSegment(_segments.Get(integer));
				}
			}
		}

		internal virtual void ReadSegment(Jbig2Segment s)
		{
			var ptr = (int)_ra.GetPosition();
			if (s.DataLength == 0xffffffffl)
			{
				// TODO figure this bit out, 7.2.7
				return;
			}
			var data = new byte[(int)s.DataLength];
			_ra.Read(data);
			s.Data = data;
			if (s.Type == PageInformation)
			{
				var last = (int)_ra.GetPosition();
				_ra.Seek(ptr);
				var pageBitmapWidth = _ra.ReadInt();
				var pageBitmapHeight = _ra.ReadInt();
				_ra.Seek(last);
				var p = _pages.Get(s.Page);
				if (p == null)
				{
					throw new IOException("Referring to widht or height of a page we haven't seen yet: {0}").SetMessageParams
						(s.Page);
				}
				p.PageBitmapWidth = pageBitmapWidth;
				p.PageBitmapHeight = pageBitmapHeight;
			}
		}

		internal virtual Jbig2Segment ReadHeader()
		{
			var ptr = (int)_ra.GetPosition();
			// 7.2.1
			var segmentNumber = _ra.ReadInt();
			var s = new Jbig2Segment(segmentNumber);
			// 7.2.3
			var segmentHeaderFlags = _ra.Read();
			var deferredNonRetain = (segmentHeaderFlags & 0x80) == 0x80;
			s.DeferredNonRetain = deferredNonRetain;
			var pageAssociationSize = (segmentHeaderFlags & 0x40) == 0x40;
			var segmentType = segmentHeaderFlags & 0x3f;
			s.Type = segmentType;
			//7.2.4
			var referredToByte0 = _ra.Read();
			var countOfReferredToSegments = (referredToByte0 & 0xE0) >> 5;
			int[] referredToSegmentNumbers = null;
			bool[] segmentRetentionFlags = null;
			if (countOfReferredToSegments == 7)
			{
				// at least five bytes
				_ra.Seek(_ra.GetPosition() - 1);
				countOfReferredToSegments = _ra.ReadInt() & 0x1fffffff;
				segmentRetentionFlags = new bool[countOfReferredToSegments + 1];
				var i = 0;
				var referredToCurrentByte = 0;
				do
				{
					var j = i % 8;
					if (j == 0)
					{
						referredToCurrentByte = _ra.Read();
					}
					segmentRetentionFlags[i] = (0x1 << j & referredToCurrentByte) >> j == 0x1;
					i++;
				}
				while (i <= countOfReferredToSegments);
			}
			else
			{
				if (countOfReferredToSegments <= 4)
				{
					// only one byte
					segmentRetentionFlags = new bool[countOfReferredToSegments + 1];
					referredToByte0 &= 0x1f;
					for (var i = 0; i <= countOfReferredToSegments; i++)
					{
						segmentRetentionFlags[i] = (0x1 << i & referredToByte0) >> i == 0x1;
					}
				}
				else
				{
					if (countOfReferredToSegments == 5 || countOfReferredToSegments == 6)
					{
						throw new IOException("Count of referred-to segments has forbidden value in the header for segment {0} starting at {1}"
							).SetMessageParams(segmentNumber, ptr);
					}
				}
			}
			s.SegmentRetentionFlags = segmentRetentionFlags;
			s.CountOfReferredToSegments = countOfReferredToSegments;
			// 7.2.5
			referredToSegmentNumbers = new int[countOfReferredToSegments + 1];
			for (var i = 1; i <= countOfReferredToSegments; i++)
			{
				if (segmentNumber <= 256)
				{
					referredToSegmentNumbers[i] = _ra.Read();
				}
				else
				{
					if (segmentNumber <= 65536)
					{
						referredToSegmentNumbers[i] = _ra.ReadUnsignedShort();
					}
					else
					{
						// TODO wtf ack
						referredToSegmentNumbers[i] = (int)_ra.ReadUnsignedInt();
					}
				}
			}
			s.ReferredToSegmentNumbers = referredToSegmentNumbers;
			// 7.2.6
			int segmentPageAssociation;
			var pageAssociationOffset = (int)_ra.GetPosition() - ptr;
			if (pageAssociationSize)
			{
				segmentPageAssociation = _ra.ReadInt();
			}
			else
			{
				segmentPageAssociation = _ra.Read();
			}
			if (segmentPageAssociation < 0)
			{
				throw new IOException("Page {0} is invalid for segment {1} starting at {2}").SetMessageParams(segmentPageAssociation
					, segmentNumber, ptr);
			}
			s.Page = segmentPageAssociation;
			// so we can change the page association at embedding time.
			s.PageAssociationSize = pageAssociationSize;
			s.PageAssociationOffset = pageAssociationOffset;
			if (segmentPageAssociation > 0 && !_pages.ContainsKey(segmentPageAssociation))
			{
				_pages.Put(segmentPageAssociation, new Jbig2Page(segmentPageAssociation, this));
			}
			if (segmentPageAssociation > 0)
			{
				_pages.Get(segmentPageAssociation).AddSegment(s);
			}
			else
			{
				_globals.Add(s);
			}
			// 7.2.7
			var segmentDataLength = _ra.ReadUnsignedInt();
			// TODO the 0xffffffff value that might be here, and how to understand those afflicted segments
			s.DataLength = segmentDataLength;
			var endPtr = (int)_ra.GetPosition();
			_ra.Seek(ptr);
			var headerData = new byte[endPtr - ptr];
			_ra.Read(headerData);
			s.HeaderData = headerData;
			return s;
		}

		internal virtual void ReadFileHeader()
		{
			_ra.Seek(0);
			var idstring = new byte[8];
			_ra.Read(idstring);
			var refidstring = new byte[] { 0x97, 0x4A, 0x42, 0x32, 0x0D, 0x0A, 0x1A, 0x0A };
			for (var i = 0; i < idstring.Length; i++)
			{
				if (idstring[i] != refidstring[i])
				{
					throw new IOException("File header idstring is not good at byte {0}").SetMessageParams(i);
				}
			}
			var fileheaderflags = _ra.Read();
			_sequential = (fileheaderflags & 0x1) == 0x1;
			_numberOfPagesKnown = (fileheaderflags & 0x2) == 0x0;
			if ((fileheaderflags & 0xfc) != 0x0)
			{
				throw new IOException("File header flags bits from 2 to 7 should be 0, some not");
			}
			if (_numberOfPagesKnown)
			{
				_numberOfPages = _ra.ReadInt();
			}
		}

		public virtual int NumberOfPages()
		{
			return _pages.Count;
		}

		public virtual int GetPageHeight(int i)
		{
			return _pages.Get(i).PageBitmapHeight;
		}

		public virtual int GetPageWidth(int i)
		{
			return _pages.Get(i).PageBitmapWidth;
		}

		public virtual Jbig2Page GetPage(int page)
		{
			return _pages.Get(page);
		}

		public virtual byte[] GetGlobal(bool forEmbedding)
		{
			var os = new MemoryStream();
			byte[] streamBytes = null;
			try
			{
				foreach (object element in _globals)
				{
					var s = (Jbig2Segment)element;
					if (forEmbedding && (s.Type == EndOfFile || s.Type == EndOfPage))
					{
						continue;
					}
					os.Write(s.HeaderData);
					os.Write(s.Data);
				}
				if (os.Length > 0)
				{
					streamBytes = os.ToArray();
				}
				os.Dispose();
			}
			catch (System.IO.IOException e)
			{
				var logger = LogManager.GetLogger(typeof(Jbig2SegmentReader));
				logger.Debug(e.Message);
			}
			return streamBytes;
		}

		public override string ToString()
		{
			if (_read)
			{
				return "Jbig2SegmentReader: number of pages: " + NumberOfPages();
			}

			return "Jbig2SegmentReader in indeterminate state.";
		}
	}
}
