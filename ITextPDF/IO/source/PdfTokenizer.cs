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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Source
{
	public class PdfTokenizer : IDisposable
	{
		public enum PdfTokenType
		{
			Number,
			String,
			Name,
			Comment,
			StartArray,
			EndArray,
			StartDic,
			EndDic,
			Ref,
			Obj,
			EndObj,
			Other,
			EndOfFile
		}

		public static readonly bool[] delims = { true, true, false, false, false, false, false, false,
			false, false, true, true, false, true, true, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, true, false, false, false, false, true
			, false, false, true, true, false, false, false, false, false, true, false, false, false, false, false
			, false, false, false, false, false, false, false, true, false, true, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, true, false, true, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
			, false, false, false, false, false, false, false, false };

		public static readonly byte[] Obj = ByteUtils.GetIsoBytes("obj");

		public static readonly byte[] R = ByteUtils.GetIsoBytes("R");

		public static readonly byte[] Xref = ByteUtils.GetIsoBytes("xref");

		public static readonly byte[] Startxref = ByteUtils.GetIsoBytes("startxref");

		public static readonly byte[] Stream = ByteUtils.GetIsoBytes("stream");

		public static readonly byte[] Trailer = ByteUtils.GetIsoBytes("trailer");

		public static readonly byte[] N = ByteUtils.GetIsoBytes("n");

		public static readonly byte[] F = ByteUtils.GetIsoBytes("f");

		public static readonly byte[] Null = ByteUtils.GetIsoBytes("null");

		public static readonly byte[] True = ByteUtils.GetIsoBytes("true");

		public static readonly byte[] False = ByteUtils.GetIsoBytes("false");

		protected internal PdfTokenType type;

		protected internal int reference;

		protected internal int generation;

		protected internal bool hexString;

		protected internal ByteBuffer outBuf;

		private readonly RandomAccessFileOrArray file;

		/// <summary>Streams are closed automatically.</summary>
		private bool closeStream = true;

		/// <summary>
		/// Creates a PdfTokenizer for the specified
		/// <see cref="RandomAccessFileOrArray"/>.
		/// </summary>
		/// <remarks>
		/// Creates a PdfTokenizer for the specified
		/// <see cref="RandomAccessFileOrArray"/>.
		/// The beginning of the file is read to determine the location of the header, and the data source is adjusted
		/// as necessary to account for any junk that occurs in the byte source before the header
		/// </remarks>
		/// <param name="file">the source</param>
		public PdfTokenizer(RandomAccessFileOrArray file)
		{
			this.file = file;
			outBuf = new ByteBuffer();
		}

		public virtual void Seek(long pos)
		{
			file.Seek(pos);
		}

		public virtual void ReadFully(byte[] bytes)
		{
			file.ReadFully(bytes);
		}

		public virtual long GetPosition()
		{
			return file.GetPosition();
		}

		public virtual void Close()
		{
			if (closeStream)
			{
				file.Close();
			}
		}

		public virtual long Length()
		{
			return file.Length();
		}

		public virtual int Read()
		{
			return file.Read();
		}

		public virtual string ReadString(int size)
		{
			var buf = new StringBuilder();
			int ch;
			while ((size--) > 0)
			{
				ch = Read();
				if (ch == -1)
				{
					break;
				}
				buf.Append((char)ch);
			}
			return buf.ToString();
		}

		public virtual PdfTokenType GetTokenType()
		{
			return type;
		}

		public virtual byte[] GetByteContent()
		{
			return outBuf.ToByteArray();
		}

		public virtual string GetStringValue()
		{
			return JavaUtil.GetStringForBytes(outBuf.GetInternalBuffer(), 0, outBuf.Size());
		}

		public virtual byte[] GetDecodedStringContent()
		{
			return DecodeStringContent(outBuf.GetInternalBuffer(), 0, outBuf.Size() - 1, IsHexString());
		}

		public virtual bool TokenValueEqualsTo(byte[] cmp)
		{
			if (cmp == null)
			{
				return false;
			}
			var size = cmp.Length;
			if (outBuf.Size() != size)
			{
				return false;
			}
			for (var i = 0; i < size; i++)
			{
				if (cmp[i] != outBuf.GetInternalBuffer()[i])
				{
					return false;
				}
			}
			return true;
		}

		public virtual int GetObjNr()
		{
			return reference;
		}

		public virtual int GetGenNr()
		{
			return generation;
		}

		public virtual void BackOnePosition(int ch)
		{
			if (ch != -1)
			{
				file.PushBack((byte)ch);
			}
		}

		public virtual int GetHeaderOffset()
		{
			var str = ReadString(1024);
			var idx = str.IndexOf("%PDF-", StringComparison.Ordinal);
			if (idx < 0)
			{
				idx = str.IndexOf("%FDF-", StringComparison.Ordinal);
				if (idx < 0)
				{
					throw new IOException(IOException.PdfHeaderNotFound, this);
				}
			}
			return idx;
		}

		public virtual string CheckPdfHeader()
		{
			file.Seek(0);
			var str = ReadString(1024);
			var idx = str.IndexOf("%PDF-", StringComparison.Ordinal);
			if (idx != 0)
			{
				throw new IOException(IOException.PdfHeaderNotFound, this);
			}
			return str.JSubstring(idx + 1, idx + 8);
		}

		public virtual void CheckFdfHeader()
		{
			file.Seek(0);
			var str = ReadString(1024);
			var idx = str.IndexOf("%FDF-", StringComparison.Ordinal);
			if (idx != 0)
			{
				throw new IOException(IOException.FdfStartxrefNotFound, this);
			}
		}

		public virtual long GetStartxref()
		{
			var arrLength = 1024;
			var fileLength = file.Length();
			var pos = fileLength - arrLength;
			if (pos < 1)
			{
				pos = 1;
			}
			while (pos > 0)
			{
				file.Seek(pos);
				var str = ReadString(arrLength);
				var idx = str.LastIndexOf("startxref");
				if (idx >= 0)
				{
					return pos + idx;
				}
				// 9 = "startxref".length()
				pos = pos - arrLength + 9;
			}
			throw new IOException(IOException.PdfStartxrefNotFound, this);
		}

		public virtual void NextValidToken()
		{
			var level = 0;
			byte[] n1 = null;
			byte[] n2 = null;
			long ptr = 0;
			while (NextToken())
			{
				if (type == PdfTokenType.Comment)
				{
					continue;
				}
				switch (level)
				{
					case 0:
						{
							if (type != PdfTokenType.Number)
							{
								return;
							}
							ptr = file.GetPosition();
							n1 = GetByteContent();
							++level;
							break;
						}

					case 1:
						{
							if (type != PdfTokenType.Number)
							{
								file.Seek(ptr);
								type = PdfTokenType.Number;
								outBuf.Reset().Append(n1);
								return;
							}
							n2 = GetByteContent();
							++level;
							break;
						}

					case 2:
						{
							if (type == PdfTokenType.Other)
							{
								if (TokenValueEqualsTo(R))
								{
									Debug.Assert(n2 != null);
									type = PdfTokenType.Ref;
									try
									{
										reference = Convert.ToInt32(JavaUtil.GetStringForBytes(n1), CultureInfo.InvariantCulture
											);
										generation = Convert.ToInt32(JavaUtil.GetStringForBytes(n2), CultureInfo.InvariantCulture
											);
									}
									catch (Exception)
									{
										//warn about incorrect reference number
										//Exception: NumberFormatException for java, FormatException or OverflowException for .NET
										var logger = LogManager.GetLogger(typeof(PdfTokenizer));
										logger.Error(MessageFormatUtil.Format(LogMessageConstant.INVALID_INDIRECT_REFERENCE, JavaUtil.GetStringForBytes
											(n1), JavaUtil.GetStringForBytes(n2)));
										reference = -1;
										generation = 0;
									}
									return;
								}

								if (TokenValueEqualsTo(Obj))
								{
									Debug.Assert(n2 != null);
									type = PdfTokenType.Obj;
									reference = Convert.ToInt32(JavaUtil.GetStringForBytes(n1), CultureInfo.InvariantCulture
									);
									generation = Convert.ToInt32(JavaUtil.GetStringForBytes(n2), CultureInfo.InvariantCulture
									);
									return;
								}
							}
							file.Seek(ptr);
							type = PdfTokenType.Number;
							outBuf.Reset().Append(n1);
							return;
						}
				}
			}
			// if the level 1 check returns EOF,
			// then we are still looking at a number - set the type back to Number
			if (level == 1)
			{
				type = PdfTokenType.Number;
				outBuf.Reset().Append(n1);
			}
		}

		// if we hit here, the file is either corrupt (stream ended unexpectedly),
		// or the last token ended exactly at the end of a stream.  This last
		// case can occur inside an Object Stream.
		public virtual bool NextToken()
		{
			int ch;
			outBuf.Reset();
			do
			{
				ch = file.Read();
			}
			while (ch != -1 && IsWhitespace(ch));
			if (ch == -1)
			{
				type = PdfTokenType.EndOfFile;
				return false;
			}
			switch (ch)
			{
				case '[':
					{
						type = PdfTokenType.StartArray;
						break;
					}

				case ']':
					{
						type = PdfTokenType.EndArray;
						break;
					}

				case '/':
					{
						type = PdfTokenType.Name;
						while (true)
						{
							ch = file.Read();
							if (delims[ch + 1])
							{
								break;
							}
							outBuf.Append(ch);
						}
						BackOnePosition(ch);
						break;
					}

				case '>':
					{
						ch = file.Read();
						if (ch != '>')
						{
							ThrowError(IOException.GtNotExpected);
						}
						type = PdfTokenType.EndDic;
						break;
					}

				case '<':
					{
						var v1 = file.Read();
						if (v1 == '<')
						{
							type = PdfTokenType.StartDic;
							break;
						}
						type = PdfTokenType.String;
						hexString = true;
						var v2 = 0;
						while (true)
						{
							while (IsWhitespace(v1))
							{
								v1 = file.Read();
							}
							if (v1 == '>')
							{
								break;
							}
							outBuf.Append(v1);
							v1 = ByteBuffer.GetHex(v1);
							if (v1 < 0)
							{
								break;
							}
							v2 = file.Read();
							while (IsWhitespace(v2))
							{
								v2 = file.Read();
							}
							if (v2 == '>')
							{
								break;
							}
							outBuf.Append(v2);
							v2 = ByteBuffer.GetHex(v2);
							if (v2 < 0)
							{
								break;
							}
							v1 = file.Read();
						}
						if (v1 < 0 || v2 < 0)
						{
							ThrowError(IOException.ErrorReadingString);
						}
						break;
					}

				case '%':
					{
						type = PdfTokenType.Comment;
						do
						{
							ch = file.Read();
						}
						while (ch != -1 && ch != '\r' && ch != '\n');
						break;
					}

				case '(':
					{
						type = PdfTokenType.String;
						hexString = false;
						var nesting = 0;
						while (true)
						{
							ch = file.Read();
							if (ch == -1)
							{
								break;
							}
							if (ch == '(')
							{
								++nesting;
							}
							else
							{
								if (ch == ')')
								{
									--nesting;
									if (nesting == -1)
									{
										break;
									}
								}
								else
								{
									if (ch == '\\')
									{
										outBuf.Append('\\');
										ch = file.Read();
										if (ch < 0)
										{
											break;
										}
									}
								}
							}
							outBuf.Append(ch);
						}
						if (ch == -1)
						{
							ThrowError(IOException.ErrorReadingString);
						}
						break;
					}

				default:
					{
						if (ch == '-' || ch == '+' || ch == '.' || (ch >= '0' && ch <= '9'))
						{
							type = PdfTokenType.Number;
							var isReal = false;
							var numberOfMinuses = 0;
							if (ch == '-')
							{
								// Take care of number like "--234". If Acrobat can read them so must we.
								do
								{
									++numberOfMinuses;
									ch = file.Read();
								}
								while (ch == '-');
								outBuf.Append('-');
							}
							else
							{
								outBuf.Append(ch);
								// We don't need to check if the number is real over here
								// as we need to know that fact only in case if there are any minuses.
								ch = file.Read();
							}
							while (ch >= '0' && ch <= '9')
							{
								outBuf.Append(ch);
								ch = file.Read();
							}
							if (ch == '.')
							{
								isReal = true;
								outBuf.Append(ch);
								ch = file.Read();
								//verify if there is minus after '.'
								//In that case just ignore minus chars and everything after as Adobe Reader does
								var numberOfMinusesAfterDot = 0;
								if (ch == '-')
								{
									numberOfMinusesAfterDot++;
									ch = file.Read();
								}
								while (ch >= '0' && ch <= '9')
								{
									if (numberOfMinusesAfterDot == 0)
									{
										outBuf.Append(ch);
									}
									ch = file.Read();
								}
							}
							if (numberOfMinuses > 1 && !isReal)
							{
								// Numbers of integer type and with more than one minus before them
								// are interpreted by Acrobat as zero.
								outBuf.Reset();
								outBuf.Append('0');
							}
						}
						else
						{
							type = PdfTokenType.Other;
							do
							{
								outBuf.Append(ch);
								ch = file.Read();
							}
							while (!delims[ch + 1]);
						}
						if (ch != -1)
						{
							BackOnePosition(ch);
						}
						break;
					}
			}
			return true;
		}

		public virtual long GetLongValue()
		{
			return Convert.ToInt64(GetStringValue(), CultureInfo.InvariantCulture);
		}

		public virtual int GetIntValue()
		{
			return Convert.ToInt32(GetStringValue(), CultureInfo.InvariantCulture);
		}

		public virtual bool IsHexString()
		{
			return hexString;
		}

		public virtual bool IsCloseStream()
		{
			return closeStream;
		}

		public virtual void SetCloseStream(bool closeStream)
		{
			this.closeStream = closeStream;
		}

		public virtual RandomAccessFileOrArray GetSafeFile()
		{
			return file.CreateView();
		}

		/// <summary>Resolve escape symbols or hexadecimal symbols.</summary>
		/// <remarks>
		/// Resolve escape symbols or hexadecimal symbols.
		/// <para />
		/// NOTE Due to PdfReference 1.7 part 3.2.3 String value contain ASCII characters,
		/// so we can convert it directly to byte array.
		/// </remarks>
		/// <param name="content">string bytes to be decoded</param>
		/// <param name="from">given start index</param>
		/// <param name="to">given end index</param>
		/// <param name="hexWriting">
		/// true if given string is hex-encoded, e.g. '&lt;69546578…&gt;'.
		/// False otherwise, e.g. '((iText( some version)…)'
		/// </param>
		/// <returns>
		/// byte[] for decrypting or for creating
		/// <see cref="System.String"/>.
		/// </returns>
		protected internal static byte[] DecodeStringContent(byte[] content, int from, int to, bool hexWriting)
		{
			var buffer = new ByteBuffer(to - from + 1);
			// <6954657874ae...>
			if (hexWriting)
			{
				for (var i = from; i <= to;)
				{
					var v1 = ByteBuffer.GetHex(content[i++]);
					if (i > to)
					{
						buffer.Append(v1 << 4);
						break;
					}
					int v2 = content[i++];
					v2 = ByteBuffer.GetHex(v2);
					buffer.Append((v1 << 4) + v2);
				}
			}
			else
			{
				// ((iText\( some version)...)
				for (var i = from; i <= to;)
				{
					int ch = content[i++];
					if (ch == '\\')
					{
						var lineBreak = false;
						ch = content[i++];
						switch (ch)
						{
							case 'n':
								{
									ch = '\n';
									break;
								}

							case 'r':
								{
									ch = '\r';
									break;
								}

							case 't':
								{
									ch = '\t';
									break;
								}

							case 'b':
								{
									ch = '\b';
									break;
								}

							case 'f':
								{
									ch = '\f';
									break;
								}

							case '(':
							case ')':
							case '\\':
								{
									break;
								}

							case '\r':
								{
									lineBreak = true;
									if (i <= to && content[i++] != '\n')
									{
										i--;
									}
									break;
								}

							case '\n':
								{
									lineBreak = true;
									break;
								}

							default:
								{
									if (ch < '0' || ch > '7')
									{
										break;
									}
									var octal = ch - '0';
									ch = content[i++];
									if (ch < '0' || ch > '7')
									{
										i--;
										ch = octal;
										break;
									}
									octal = (octal << 3) + ch - '0';
									ch = content[i++];
									if (ch < '0' || ch > '7')
									{
										i--;
										ch = octal;
										break;
									}
									octal = (octal << 3) + ch - '0';
									ch = octal & 0xff;
									break;
								}
						}
						if (lineBreak)
						{
							continue;
						}
					}
					else
					{
						if (ch == '\r')
						{
							// in this case current char is '\n' and we have to skip next '\n' if it presents.
							ch = '\n';
							if (i <= to && content[i++] != '\n')
							{
								i--;
							}
						}
					}
					buffer.Append(ch);
				}
			}
			return buffer.ToByteArray();
		}

		/// <summary>Resolve escape symbols or hexadecimal symbols.</summary>
		/// <remarks>
		/// Resolve escape symbols or hexadecimal symbols.
		/// <br />
		/// NOTE Due to PdfReference 1.7 part 3.2.3 String value contain ASCII characters,
		/// so we can convert it directly to byte array.
		/// </remarks>
		/// <param name="content">string bytes to be decoded</param>
		/// <param name="hexWriting">
		/// true if given string is hex-encoded, e.g. '&lt;69546578…&gt;'.
		/// False otherwise, e.g. '((iText( some version)…)'
		/// </param>
		/// <returns>
		/// byte[] for decrypting or for creating
		/// <see cref="System.String"/>.
		/// </returns>
		public static byte[] DecodeStringContent(byte[] content, bool hexWriting)
		{
			return DecodeStringContent(content, 0, content.Length - 1, hexWriting);
		}

		/// <summary>Is a certain character a whitespace? Currently checks on the following: '0', '9', '10', '12', '13', '32'.
		///     </summary>
		/// <remarks>
		/// Is a certain character a whitespace? Currently checks on the following: '0', '9', '10', '12', '13', '32'.
		/// <br />
		/// The same as calling
		/// <see cref="IsWhitespace(int, bool)">isWhiteSpace(ch, true)</see>.
		/// </remarks>
		/// <param name="ch">int</param>
		/// <returns>boolean</returns>
		public static bool IsWhitespace(int ch)
		{
			return IsWhitespace(ch, true);
		}

		/// <summary>Checks whether a character is a whitespace.</summary>
		/// <remarks>Checks whether a character is a whitespace. Currently checks on the following: '0', '9', '10', '12', '13', '32'.
		///     </remarks>
		/// <param name="ch">int</param>
		/// <param name="isWhitespace">boolean</param>
		/// <returns>boolean</returns>
		protected internal static bool IsWhitespace(int ch, bool isWhitespace)
		{
			return ((isWhitespace && ch == 0) || ch == 9 || ch == 10 || ch == 12 || ch == 13 || ch == 32);
		}

		protected internal static bool IsDelimiter(int ch)
		{
			return (ch == '(' || ch == ')' || ch == '<' || ch == '>' || ch == '[' || ch == ']' || ch == '/' || ch == '%'
				);
		}

		protected internal static bool IsDelimiterWhitespace(int ch)
		{
			return delims[ch + 1];
		}

		/// <summary>Helper method to handle content errors.</summary>
		/// <remarks>
		/// Helper method to handle content errors. Add file position to
		/// <c>PdfRuntimeException</c>.
		/// </remarks>
		/// <param name="error">message.</param>
		/// <param name="messageParams">error params.</param>
		public virtual void ThrowError(string error, params object[] messageParams)
		{
			try
			{
				throw new IOException(IOException.ErrorAtFilePointer1, new IOException(error).SetMessageParams
					(messageParams)).SetMessageParams(file.GetPosition());
			}
			catch (System.IO.IOException)
			{
				throw new IOException(IOException.ErrorAtFilePointer1, new IOException(error).SetMessageParams
					(messageParams)).SetMessageParams(error, "no position");
			}
		}

		/// <summary>
		/// Checks whether
		/// <paramref name="line"/>
		/// equals to 'trailer'.
		/// </summary>
		/// <param name="line">for check.</param>
		/// <returns>true, if line is equals tio 'trailer', otherwise false.</returns>
		public static bool CheckTrailer(ByteBuffer line)
		{
			if (Trailer.Length > line.Size())
			{
				return false;
			}
			for (var i = 0; i < Trailer.Length; i++)
			{
				if (Trailer[i] != line.Get(i))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Reads data into the provided byte[].</summary>
		/// <remarks>
		/// Reads data into the provided byte[]. Checks on leading whitespace.
		/// See
		/// <see cref="IsWhitespace(int)">isWhiteSpace(int)</see>
		/// or
		/// <see cref="IsWhitespace(int, bool)">isWhiteSpace(int, boolean)</see>
		/// for a list of whitespace characters.
		/// <br />
		/// The same as calling
		/// <see cref="ReadLineSegment(ByteBuffer, bool)">readLineSegment(input, true)</see>.
		/// </remarks>
		/// <param name="buffer">
		/// a
		/// <see cref="ByteBuffer"/>
		/// to which the result of reading will be saved
		/// </param>
		/// <returns>true, if something was read or if the end of the input stream is not reached</returns>
		public virtual bool ReadLineSegment(ByteBuffer buffer)
		{
			return ReadLineSegment(buffer, true);
		}

		/// <summary>Reads data into the provided byte[].</summary>
		/// <remarks>
		/// Reads data into the provided byte[]. Checks on leading whitespace.
		/// See
		/// <see cref="IsWhitespace(int)">isWhiteSpace(int)</see>
		/// or
		/// <see cref="IsWhitespace(int, bool)">isWhiteSpace(int, boolean)</see>
		/// for a list of whitespace characters.
		/// </remarks>
		/// <param name="buffer">
		/// a
		/// <see cref="ByteBuffer"/>
		/// to which the result of reading will be saved
		/// </param>
		/// <param name="isNullWhitespace">
		/// boolean to indicate whether '0' is whitespace or not.
		/// If in doubt, use true or overloaded method
		/// <see cref="ReadLineSegment(ByteBuffer)">readLineSegment(input)</see>
		/// </param>
		/// <returns>true, if something was read or if the end of the input stream is not reached</returns>
		public virtual bool ReadLineSegment(ByteBuffer buffer, bool isNullWhitespace)
		{
			int c;
			var eol = false;
			// ssteward, pdftk-1.10, 040922:
			// skip initial whitespace; added this because PdfReader.rebuildXref()
			// assumes that line provided by readLineSegment does not have init. whitespace;
			while (IsWhitespace((c = Read()), isNullWhitespace))
			{
			}
			var prevWasWhitespace = false;
			while (!eol)
			{
				switch (c)
				{
					case -1:
					case '\n':
						{
							eol = true;
							break;
						}

					case '\r':
						{
							eol = true;
							var cur = GetPosition();
							if ((Read()) != '\n')
							{
								Seek(cur);
							}
							break;
						}

					case 9:
					//whitespaces
					case 12:
					case 32:
						{
							if (prevWasWhitespace)
							{
								break;
							}
							prevWasWhitespace = true;
							buffer.Append((byte)c);
							break;
						}

					default:
						{
							prevWasWhitespace = false;
							buffer.Append((byte)c);
							break;
						}
				}
				// break loop? do it before we read() again
				if (eol || buffer.Size() == buffer.Capacity())
				{
					eol = true;
				}
				else
				{
					c = Read();
				}
			}
			if (buffer.Size() == buffer.Capacity())
			{
				eol = false;
				while (!eol)
				{
					switch (c = Read())
					{
						case -1:
						case '\n':
							{
								eol = true;
								break;
							}

						case '\r':
							{
								eol = true;
								var cur = GetPosition();
								if ((Read()) != '\n')
								{
									Seek(cur);
								}
								break;
							}
					}
				}
			}
			return !(c == -1 && buffer.IsEmpty());
		}

		/// <summary>Check whether line starts with object declaration.</summary>
		/// <param name="lineTokenizer">tokenizer, built by single line.</param>
		/// <returns>object number and generation if check is successful, otherwise - null.</returns>
		public static int[] CheckObjectStart(PdfTokenizer lineTokenizer)
		{
			try
			{
				lineTokenizer.Seek(0);
				if (!lineTokenizer.NextToken() || lineTokenizer.GetTokenType() != PdfTokenType.Number)
				{
					return null;
				}
				var num = lineTokenizer.GetIntValue();
				if (!lineTokenizer.NextToken() || lineTokenizer.GetTokenType() != PdfTokenType.Number)
				{
					return null;
				}
				var gen = lineTokenizer.GetIntValue();
				if (!lineTokenizer.NextToken())
				{
					return null;
				}
				if (!JavaUtil.ArraysEquals(Obj, lineTokenizer.GetByteContent()))
				{
					return null;
				}
				return new[] { num, gen };
			}
			catch (Exception)
			{
			}
			// empty on purpose
			return null;
		}

		[Obsolete(@"Will be removed in 7.2. This inner class is not used anywhere")]
		protected internal class ReusableRandomAccessSource : IRandomAccessSource
		{
			private ByteBuffer buffer;

			public ReusableRandomAccessSource(ByteBuffer buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentException("Passed byte buffer can not be null.");
				}
				this.buffer = buffer;
			}

			public virtual int Get(long offset)
			{
				if (offset >= buffer.Size())
				{
					return -1;
				}
				return 0xff & buffer.GetInternalBuffer()[(int)offset];
			}

			public virtual int Get(long offset, byte[] bytes, int off, int len)
			{
				if (buffer == null)
				{
					throw new InvalidOperationException("Already closed");
				}
				if (offset >= buffer.Size())
				{
					return -1;
				}
				if (offset + len > buffer.Size())
				{
					len = (int)(buffer.Size() - offset);
				}
				Array.Copy(buffer.GetInternalBuffer(), (int)offset, bytes, off, len);
				return len;
			}

			public virtual long Length()
			{
				return buffer.Size();
			}

			public virtual void Close()
			{
				buffer = null;
			}
		}

		void IDisposable.Dispose()
		{
			Close();
		}
	}
}
