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
using IText.IO.Font.Cmap;
using IText.IO.Source;
using IText.IO.Util;
using IText.Logger;

namespace IText.IO.Font
{
	public class CMapEncoding
	{
		private static readonly IList<byte[]> IDENTITY_H_V_CODESPACE_RANGES = JavaUtil.ArraysAsList(new byte[] { 0
			, 0 }, new byte[] { (byte)0xff, (byte)0xff });

		private string cmap;

		private string uniMap;

		// true if CMap is Identity-H/V
		private bool isDirect;

		private CMapCidUni cid2Uni;

		private CMapCidByte cid2Code;

		private IntHashtable code2Cid;

		private IList<byte[]> codeSpaceRanges;

		/// <param name="cmap">CMap name.</param>
		public CMapEncoding(string cmap)
		{
			this.cmap = cmap;
			if (cmap.Equals(PdfEncodings.IDENTITY_H) || cmap.Equals(PdfEncodings.IDENTITY_V))
			{
				isDirect = true;
			}
			// Actually this constructor is only called for Identity-H/V cmaps currently.
			// Even for hypothetical case of non-Identity-H/V, let's use Identity-H/V ranges (two byte ranges) for compatibility with previous behavior
			codeSpaceRanges = IDENTITY_H_V_CODESPACE_RANGES;
		}

		/// <param name="cmap">CMap name.</param>
		/// <param name="uniMap">CMap to convert Unicode value to CID.</param>
		public CMapEncoding(string cmap, string uniMap)
		{
			this.cmap = cmap;
			this.uniMap = uniMap;
			if (cmap.Equals(PdfEncodings.IDENTITY_H) || cmap.Equals(PdfEncodings.IDENTITY_V))
			{
				cid2Uni = FontCache.GetCid2UniCmap(uniMap);
				isDirect = true;
				codeSpaceRanges = IDENTITY_H_V_CODESPACE_RANGES;
			}
			else
			{
				cid2Code = FontCache.GetCid2Byte(cmap);
				code2Cid = cid2Code.GetReversMap();
				codeSpaceRanges = cid2Code.GetCodeSpaceRanges();
			}
		}

		public CMapEncoding(string cmap, byte[] cmapBytes)
		{
			this.cmap = cmap;
			cid2Code = new CMapCidByte();
			try
			{
				CMapParser.ParseCid(cmap, cid2Code, new CMapLocationFromBytes(cmapBytes));
				code2Cid = cid2Code.GetReversMap();
				codeSpaceRanges = cid2Code.GetCodeSpaceRanges();
			}
			catch (System.IO.IOException)
			{
				LogManager.GetLogger(GetType()).Error(LogMessageConstant.FAILED_TO_PARSE_ENCODING_STREAM);
			}
		}

		public virtual bool IsDirect()
		{
			return isDirect;
		}

		public virtual bool HasUniMap()
		{
			return uniMap != null && uniMap.Length > 0;
		}

		public virtual string GetRegistry()
		{
			if (IsDirect())
			{
				return "Adobe";
			}

			return cid2Code.GetRegistry();
		}

		public virtual string GetOrdering()
		{
			if (IsDirect())
			{
				return "Identity";
			}

			return cid2Code.GetOrdering();
		}

		public virtual int GetSupplement()
		{
			if (IsDirect())
			{
				return 0;
			}

			return cid2Code.GetSupplement();
		}

		public virtual string GetUniMapName()
		{
			return uniMap;
		}

		public virtual string GetCmapName()
		{
			return cmap;
		}

		/// <summary>
		/// Checks whether the
		/// <see cref="CMapEncoding"/>
		/// was built with corresponding cmap name.
		/// </summary>
		/// <param name="cmap">a CMAP</param>
		/// <returns>true, if the CMapEncoding was built with the cmap. Otherwise false.</returns>
		public virtual bool IsBuiltWith(string cmap)
		{
			return Equals(cmap, this.cmap);
		}

		/// <param name="cid">a CID</param>
		/// <returns>CMAP code as an int</returns>
		[Obsolete(@"Will be removed in 7.2. Use GetCmapBytes(int) instead.")]
		public virtual int GetCmapCode(int cid)
		{
			if (isDirect)
			{
				return cid;
			}

			return ToInteger(cid2Code.Lookup(cid));
		}

		public virtual byte[] GetCmapBytes(int cid)
		{
			var length = GetCmapBytesLength(cid);
			var result = new byte[length];
			FillCmapBytes(cid, result, 0);
			return result;
		}

		public virtual int FillCmapBytes(int cid, byte[] array, int offset)
		{
			if (isDirect)
			{
				array[offset++] = (byte)((cid & 0xff00) >> 8);
				array[offset++] = (byte)(cid & 0xff);
			}
			else
			{
				byte[] bytes = cid2Code.Lookup(cid);
				for (var i = 0; i < bytes.Length; i++)
				{
					array[offset++] = bytes[i];
				}
			}
			return offset;
		}

		public virtual void FillCmapBytes(int cid, ByteBuffer buffer)
		{
			if (isDirect)
			{
				buffer.Append((byte)((cid & 0xff00) >> 8));
				buffer.Append((byte)(cid & 0xff));
			}
			else
			{
				byte[] bytes = cid2Code.Lookup(cid);
				buffer.Append(bytes);
			}
		}

		public virtual int GetCmapBytesLength(int cid)
		{
			if (isDirect)
			{
				return 2;
			}

			return cid2Code.Lookup(cid).Length;
		}

		public virtual int GetCidCode(int cmapCode)
		{
			if (isDirect)
			{
				return cmapCode;
			}

			return code2Cid.Get(cmapCode);
		}

		public virtual bool ContainsCodeInCodeSpaceRange(int code, int length)
		{
			for (var i = 0; i < codeSpaceRanges.Count; i += 2)
			{
				if (length == codeSpaceRanges[i].Length)
				{
					var mask = 0xff;
					var totalShift = 0;
					var low = codeSpaceRanges[i];
					var high = codeSpaceRanges[i + 1];
					var fitsIntoRange = true;
					for (var ind = length - 1; ind >= 0; ind--, totalShift += 8, mask <<= 8)
					{
						var actualByteValue = (code & mask) >> totalShift;
						if (!(actualByteValue >= (0xff & low[ind]) && actualByteValue <= (0xff & high[ind])))
						{
							fitsIntoRange = false;
						}
					}
					if (fitsIntoRange)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static int ToInteger(byte[] bytes)
		{
			var result = 0;
			foreach (var b in bytes)
			{
				result <<= 8;
				result += b & 0xff;
			}
			return result;
		}
	}
}
