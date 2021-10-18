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
using IText.IO.Util;

namespace IText.IO
{
	/// <summary>Exception class for exceptions in io module.</summary>
	public class IOException : Exception
	{
		public const string AllFillBitsPrecedingEolCodeMustBe0 = "All fill bits preceding eol code must be 0.";

		public const string BadEndiannessTag0x4949Or0x4d4d = "Bad endianness tag: 0x4949 or 0x4d4d.";

		public const string BadMagicNumberShouldBe42 = "Bad magic number. Should be 42.";

		public const string BitsPerComponentMustBe1_2_4or8 = "Bits per component must be 1, 2, 4 or 8.";

		public const string BitsPerSample1IsNotSupported = "Bits per sample {0} is not supported.";

		public const string BmpImageException = "Bmp image exception.";

		public const string BytesCanBeAssignedToByteArrayOutputStreamOnly = "Bytes can be assigned to ByteArrayOutputStream only.";

		public const string BytesCanBeResetInByteArrayOutputStreamOnly = "Bytes can be reset in ByteArrayOutputStream only.";

		public const string CannotFind1Frame = "Cannot find frame number {0} (zero-based)";

		public const string CannotGetTiffImageColor = "Cannot get TIFF image color.";

		public const string CannotHandleBoxSizesHigherThan2_32 = "Cannot handle box sizes higher than 2^32.";

		public const string CannotInflateTiffImage = "Cannot inflate TIFF image.";

		public const string CannotReadTiffImage = "Cannot read TIFF image.";

		public const string CannotWriteByte = "Cannot write byte.";

		public const string CannotWriteBytes = "Cannot write bytes.";

		public const string CannotWriteFloatNumber = "Cannot write float number.";

		public const string CannotWriteIntNumber = "Cannot write int number.";

		public const string CcittCompressionTypeMustBeCcittg4Ccittg3_1dOrCcittg3_2d = "CCITT compression type must be CCITTG4, CCITTG3_1D or CCITTG3_2D.";

		public const string CharacterCodeException = "Character code exception.";

		public const string Cmap1WasNotFound = "The CMap {0} was not found.";

		public const string ColorDepthIsNotSupported = "The color depth {0} is not supported.";

		public const string ColorSpaceIsNotSupported = "The color space {0} is not supported.";

		public const string ComponentsMustBe1_3Or4 = "Components must be 1, 3 or 4.";

		public const string Compression1IsNotSupported = "Compression {0} is not supported.";

		public const string CompressionJpegIsOnlySupportedWithASingleStripThisImageHas1Strips = "Compression jpeg is only supported with a single strip. This image has {0} strips.";

		public const string DirectoryNumberIsTooLarge = "Directory number is too large.";

		public const string EolCodeWordEncounteredInBlackRun = "EOL code word encountered in Black run.";

		public const string EolCodeWordEncounteredInWhiteRun = "EOL code word encountered in White run.";

		public const string ErrorAtFilePointer1 = "Error at file pointer {0}.";

		public const string ErrorReadingString = "Error reading string.";

		public const string ErrorWithJpMarker = "Error with JP marker.";

		public const string ExpectedFtypMarker = "Expected FTYP marker.";

		public const string ExpectedIhdrMarker = "Expected IHDR marker.";

		public const string ExpectedJp2hMarker = "Expected JP2H marker.";

		public const string ExpectedJpMarker = "Expected JP marker.";

		public const string ExpectedTrailingZeroBitsForByteAlignedLines = "Expected trailing zero bits for byte-aligned lines";

		public const string ExtraSamplesAreNotSupported = "Extra samples are not supported.";

		public const string FdfStartxrefNotFound = "FDF startxref not found.";

		public const string FirstScanlineMustBe1dEncoded = "First scanline must be 1D encoded.";

		public const string FontFile1NotFound = "Font file {0} not found.";

		public const string GifImageException = "GIF image exception.";

		public const string GifSignatureNotFound = "GIF signature not found.";

		public const string GtNotExpected = "'>' not expected.";

		public const string IccProfileContains0ComponentsWhileImageDataContains1Components = "ICC profile contains {0} components, while the image data contains {1} components.";

		public const string IllegalValueForPredictorInTiffFile = "Illegal value for predictor in TIFF file.";

		public const string ImageFormatCannotBeRecognized = "Image format cannot be recognized.";

		public const string ImageIsNotMaskYouMustCallImageDataMakeMask = "Image is not a mask. You must call ImageData#makeMask().";

		public const string ImageMaskCannotContainAnotherImageMask = "Image mask cannot contain another image mask.";

		public const string IncompletePalette = "Incomplete palette.";

		public const string InvalidBmpFileCompression = "Invalid BMP file compression.";

		public const string InvalidCodeEncountered = "Invalid code encountered.";

		public const string InvalidCodeEncounteredWhileDecoding2dGroup3CompressedData = "Invalid code encountered while decoding 2D group 3 compressed data.";

		public const string InvalidCodeEncounteredWhileDecoding2dGroup4CompressedData = "Invalid code encountered while decoding 2D group 4 compressed data.";

		public const string InvalidIccProfile = "Invalid ICC profile.";

		public const string InvalidJpeg2000File = "Invalid JPEG2000 file.";

		public const string InvalidWoff2File = "Invalid WOFF2 font file.";

		public const string InvalidWoffFile = "Invalid WOFF font file.";

		public const string InvalidMagicValueForBmpFileMustBeBM = "Invalid magic value for bmp file. Must be 'BM'";

		public const string InvalidTtcFile = "{0} is not a valid TTC file.";

		public const string IoException = "I/O exception.";

		public const string Jbig2ImageException = "JBIG2 image exception.";

		public const string Jpeg2000ImageException = "JPEG2000 image exception.";

		public const string JpegImageException = "JPEG image exception.";

		public const string MissingTagsForOjpegCompression = "Missing tag(s) for OJPEG compression";

		public const string NValueIsNotSupported = "N value {1} is not supported.";

		public const string NotAtTrueTypeFile = "{0} is not a true type file";

		public const string PageNumberMustBeGtEq1 = "Page number must be >= 1.";

		public const string PdfHeaderNotFound = "PDF header not found.";

		public const string PdfStartxrefNotFound = "PDF startxref not found.";

		public const string Photometric1IsNotSupported = "Photometric {0} is not supported.";

		public const string PlanarImagesAreNotSupported = "Planar images are not supported.";

		public const string PngImageException = "PNG image exception.";

		public const string PrematureEofWhileReadingJpeg = "Premature EOF while reading JPEG.";

		public const string ScanlineMustBeginWithEolCodeWord = "Scanline must begin with EOL code word.";

		public const string TableDoesNotExist = "Table {0} does not exist.";

		public const string TableDoesNotExistsIn = "Table {0} does not exist in {1}";

		public const string ThisImageCanNotBeAnImageMask = "This image can not be an image mask.";

		public const string Tiff50StyleLzwCodesAreNotSupported = "TIFF 5.0-style LZW codes are not supported.";

		public const string TiffFillOrderTagMustBeEither1Or2 = "TIFF_FILL_ORDER tag must be either 1 or 2.";

		public const string TiffImageException = "TIFF image exception.";

		public const string TilesAreNotSupported = "Tiles are not supported.";

		public const string TransparencyLengthMustBeEqualTo2WithCcittImages = "Transparency length must be equal to 2 with CCITT images";

		public const string TtcIndexDoesNotExistInThisTtcFile = "TTC index doesn't exist in this TTC file.";

		public const string TypeOfFont1IsNotRecognized = "Type of font {0} is not recognized.";

		public const string TypeOfFontIsNotRecognized = "Type of font is not recognized.";

		public const string UnexpectedCloseBracket = "Unexpected close bracket.";

		public const string UnexpectedGtGt = "Unexpected '>>'.";

		public const string UnknownCompressionType1 = "Unknown compression type {0}.";

		public const string UnknownIOException = "Unknown I/O exception.";

		public const string UnknownPngFilter = "Unknown PNG filter.";

		public const string UnsupportedBoxSizeEqEq0 = "Unsupported box size == 0.";

		public const string UnsupportedEncodingException = "Unsupported encoding exception.";

		public const string _1BitSamplesAreNotSupportedForHorizontalDifferencingPredictor = "{0} bit samples are not supported for horizontal differencing predictor.";

		public const string _1CorruptedJfifMarker = "{0} corrupted jfif marker.";

		public const string _1IsNotAValidJpegFile = "{0} is not a valid jpeg file.";

		public const string _1IsNotAnAfmOrPfmFontFile = "{0} is not an afm or pfm font file.";

		public const string _1MustHave8BitsPerComponent = "{0} must have 8 bits per component.";

		public const string _1NotFoundAsFileOrResource = "{0} not found as file or resource.";

		public const string _1UnsupportedJpegMarker2 = "{0} unsupported jpeg marker {1}.";

		/// <summary>Object for more details</summary>
		protected internal object obj;

		private IList<object> messageParams;

		/// <summary>Creates a new IOException.</summary>
		/// <param name="message">the detail message.</param>
		public IOException(string message)
			: base(message)
		{
		}

		/// <summary>Creates a new IOException.</summary>
		/// <param name="cause">
		/// the cause (which is saved for later retrieval by
		/// <see cref="System.Exception.InnerException()"/>
		/// method).
		/// </param>
		public IOException(Exception cause)
			: this(UnknownIOException, cause)
		{
		}

		/// <summary>Creates a new IOException.</summary>
		/// <param name="message">the detail message.</param>
		/// <param name="obj">an object for more details.</param>
		public IOException(string message, object obj)
			: this(message)
		{
			this.obj = obj;
		}

		/// <summary>Creates a new IOException.</summary>
		/// <param name="message">the detail message.</param>
		/// <param name="cause">
		/// the cause (which is saved for later retrieval by
		/// <see cref="System.Exception.InnerException()"/>
		/// method).
		/// </param>
		public IOException(string message, Exception cause)
			: base(message, cause)
		{
		}

		/// <summary>Creates a new instance of IOException.</summary>
		/// <param name="message">the detail message.</param>
		/// <param name="cause">
		/// the cause (which is saved for later retrieval by
		/// <see cref="System.Exception.InnerException()"/>
		/// method).
		/// </param>
		/// <param name="obj">an object for more details.</param>
		public IOException(string message, Exception cause, object obj)
			: this(message, cause)
		{
			this.obj = obj;
		}

		/// <summary><inheritDoc/></summary>
		public override string Message
		{
			get
			{
				if (messageParams == null || messageParams.Count == 0)
				{
					return base.Message;
				}

				return MessageFormatUtil.Format(base.Message, GetMessageParams());
			}
		}

		/// <summary>Gets additional params for Exception message.</summary>
		/// <returns>params for exception message.</returns>
		protected internal virtual object[] GetMessageParams()
		{
			var parameters = new object[messageParams.Count];
			for (var i = 0; i < messageParams.Count; i++)
			{
				parameters[i] = messageParams[i];
			}
			return parameters;
		}

		/// <summary>Sets additional params for Exception message.</summary>
		/// <param name="messageParams">additional params.</param>
		/// <returns>object itself.</returns>
		public virtual IOException SetMessageParams(params object[] messageParams)
		{
			this.messageParams = new List<object>();
			this.messageParams.AddAll(messageParams);
			return this;
		}
	}
}
