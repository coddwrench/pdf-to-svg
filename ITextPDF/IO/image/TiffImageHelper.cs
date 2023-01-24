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
using IText.IO.Codec;
using IText.IO.Colors;
using IText.IO.Font;
using IText.IO.Source;
using IText.IO.Util;

namespace  IText.IO.Image {
    internal class TiffImageHelper {
        private class TiffParameters {
            internal TiffParameters(TiffImageData image) {
                this.image = image;
            }

            internal TiffImageData image;

            //ByteArrayOutputStream stream;
            internal bool jpegProcessing;

            internal IDictionary<string, object> additional;
        }

        /// <summary>Processes the ImageData as a TIFF image.</summary>
        /// <param name="image">image to process.</param>
        public static void ProcessImage(ImageData image) {
            if (image.GetOriginalType() != ImageType.TIFF) {
                throw new ArgumentException("TIFF image expected");
            }
            try {
                IRandomAccessSource ras;
                if (image.GetData() == null) {
                    image.LoadData();
                }
                ras = new RandomAccessSourceFactory().CreateSource(image.GetData());
                var raf = new RandomAccessFileOrArray(ras);
                var tiff = new TiffParameters((TiffImageData)image);
                ProcessTiffImage(raf, tiff);
                raf.Close();
                if (!tiff.jpegProcessing) {
                    RawImageHelper.UpdateImageAttributes(tiff.image, tiff.additional);
                }
            }
            catch (System.IO.IOException e) {
                throw new IOException(IOException.TiffImageException, e);
            }
        }

        private static void ProcessTiffImage(RandomAccessFileOrArray s, TiffParameters tiff) {
            var recoverFromImageError = tiff.image.IsRecoverFromImageError();
            var page = tiff.image.GetPage();
            var direct = tiff.image.IsDirect();
            if (page < 1) {
                throw new IOException(IOException.PageNumberMustBeGtEq1);
            }
            try {
                var dir = new TIFFDirectory(s, page - 1);
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_TILEWIDTH)) {
                    throw new IOException(IOException.TilesAreNotSupported);
                }
                var compression = TIFFConstants.COMPRESSION_NONE;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_COMPRESSION)) {
                    compression = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_COMPRESSION);
                }
                switch (compression) {
                    case TIFFConstants.COMPRESSION_CCITTRLEW:
                    case TIFFConstants.COMPRESSION_CCITTRLE:
                    case TIFFConstants.COMPRESSION_CCITTFAX3:
                    case TIFFConstants.COMPRESSION_CCITTFAX4: {
                        break;
                    }

                    default: {
                        ProcessTiffImageColor(dir, s, tiff);
                        return;
                    }
                }
                float rotation = 0;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ORIENTATION)) {
                    var rot = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_ORIENTATION);
                    if (rot == TIFFConstants.ORIENTATION_BOTRIGHT || rot == TIFFConstants.ORIENTATION_BOTLEFT) {
                        rotation = (float)Math.PI;
                    }
                    else {
                        if (rot == TIFFConstants.ORIENTATION_LEFTTOP || rot == TIFFConstants.ORIENTATION_LEFTBOT) {
                            rotation = (float)(Math.PI / 2.0);
                        }
                        else {
                            if (rot == TIFFConstants.ORIENTATION_RIGHTTOP || rot == TIFFConstants.ORIENTATION_RIGHTBOT) {
                                rotation = -(float)(Math.PI / 2.0);
                            }
                        }
                    }
                }
                long tiffT4Options = 0;
                long tiffT6Options = 0;
                var fillOrder = 1;
                var h = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_IMAGELENGTH);
                var w = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_IMAGEWIDTH);
                float XYRatio = 0;
                var resolutionUnit = TIFFConstants.RESUNIT_INCH;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_RESOLUTIONUNIT)) {
                    resolutionUnit = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_RESOLUTIONUNIT);
                }
                var dpiX = GetDpi(dir.GetField(TIFFConstants.TIFFTAG_XRESOLUTION), resolutionUnit);
                var dpiY = GetDpi(dir.GetField(TIFFConstants.TIFFTAG_YRESOLUTION), resolutionUnit);
                if (resolutionUnit == TIFFConstants.RESUNIT_NONE) {
                    if (dpiY != 0) {
                        XYRatio = dpiX / (float)dpiY;
                    }
                    dpiX = 0;
                    dpiY = 0;
                }
                var rowsStrip = h;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ROWSPERSTRIP)) {
                    rowsStrip = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_ROWSPERSTRIP);
                }
                if (rowsStrip <= 0 || rowsStrip > h) {
                    rowsStrip = h;
                }
                var offset = GetArrayLongShort(dir, TIFFConstants.TIFFTAG_STRIPOFFSETS);
                var size = GetArrayLongShort(dir, TIFFConstants.TIFFTAG_STRIPBYTECOUNTS);
                // some TIFF producers are really lousy, so...
                if ((size == null || (size.Length == 1 && (size[0] == 0 || size[0] + offset[0] > s.Length()))) && h == rowsStrip
                    ) {
                    size = new[] { s.Length() - (int)offset[0] };
                }
                var reverse = false;
                TIFFField fillOrderField = dir.GetField(TIFFConstants.TIFFTAG_FILLORDER);
                if (fillOrderField != null) {
                    fillOrder = fillOrderField.GetAsInt(0);
                }
                reverse = (fillOrder == TIFFConstants.FILLORDER_LSB2MSB);
                var parameters = 0;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_PHOTOMETRIC)) {
                    long photo = dir.GetFieldAsLong(TIFFConstants.TIFFTAG_PHOTOMETRIC);
                    if (photo == TIFFConstants.PHOTOMETRIC_MINISBLACK) {
                        parameters |= RawImageData.CCITT_BLACKIS1;
                    }
                }
                var imagecomp = 0;
                switch (compression) {
                    case TIFFConstants.COMPRESSION_CCITTRLEW:
                    case TIFFConstants.COMPRESSION_CCITTRLE: {
                        imagecomp = RawImageData.CCITTG3_1D;
                        parameters |= RawImageData.CCITT_ENCODEDBYTEALIGN | RawImageData.CCITT_ENDOFBLOCK;
                        break;
                    }

                    case TIFFConstants.COMPRESSION_CCITTFAX3: {
                        imagecomp = RawImageData.CCITTG3_1D;
                        parameters |= RawImageData.CCITT_ENDOFLINE | RawImageData.CCITT_ENDOFBLOCK;
                        TIFFField t4OptionsField = dir.GetField(TIFFConstants.TIFFTAG_GROUP3OPTIONS);
                        if (t4OptionsField != null) {
                            tiffT4Options = t4OptionsField.GetAsLong(0);
                            if ((tiffT4Options & TIFFConstants.GROUP3OPT_2DENCODING) != 0) {
                                imagecomp = RawImageData.CCITTG3_2D;
                            }
                            if ((tiffT4Options & TIFFConstants.GROUP3OPT_FILLBITS) != 0) {
                                parameters |= RawImageData.CCITT_ENCODEDBYTEALIGN;
                            }
                        }
                        break;
                    }

                    case TIFFConstants.COMPRESSION_CCITTFAX4: {
                        imagecomp = RawImageData.CCITTG4;
                        TIFFField t6OptionsField = dir.GetField(TIFFConstants.TIFFTAG_GROUP4OPTIONS);
                        if (t6OptionsField != null) {
                            tiffT6Options = t6OptionsField.GetAsLong(0);
                        }
                        break;
                    }
                }
                //single strip, direct
                if (direct && rowsStrip == h) {
                    var im = new byte[(int)size[0]];
                    s.Seek(offset[0]);
                    s.ReadFully(im);
                    RawImageHelper.UpdateRawImageParameters(tiff.image, w, h, false, imagecomp, parameters, im, null);
                    tiff.image.SetInverted(true);
                }
                else {
                    var rowsLeft = h;
                    var g4 = new Ccittg4Encoder(w);
                    for (var k = 0; k < offset.Length; ++k) {
                        var im = new byte[(int)size[k]];
                        s.Seek(offset[k]);
                        s.ReadFully(im);
                        var height = Math.Min(rowsStrip, rowsLeft);
                        var decoder = new TIFFFaxDecoder(fillOrder, w, height);
                        decoder.SetRecoverFromImageError(recoverFromImageError);
                        var outBuf = new byte[(w + 7) / 8 * height];
                        switch (compression) {
                            case TIFFConstants.COMPRESSION_CCITTRLEW:
                            case TIFFConstants.COMPRESSION_CCITTRLE: {
                                decoder.Decode1D(outBuf, im, 0, height);
                                g4.Fax4Encode(outBuf, height);
                                break;
                            }

                            case TIFFConstants.COMPRESSION_CCITTFAX3: {
                                try {
                                    decoder.Decode2D(outBuf, im, 0, height, tiffT4Options);
                                }
                                catch (Exception e) {
                                    // let's flip the fill bits and try again...
                                    tiffT4Options ^= TIFFConstants.GROUP3OPT_FILLBITS;
                                    try {
                                        decoder.Decode2D(outBuf, im, 0, height, tiffT4Options);
                                    }
                                    catch (Exception) {
                                        if (!recoverFromImageError) {
                                            throw e;
                                        }
                                        if (rowsStrip == 1) {
                                            throw e;
                                        }
                                        // repeat of reading the tiff directly (the if section of this if else structure)
                                        // copy pasted to avoid making a method with 10 tiff
                                        im = new byte[(int)size[0]];
                                        s.Seek(offset[0]);
                                        s.ReadFully(im);
                                        RawImageHelper.UpdateRawImageParameters(tiff.image, w, h, false, imagecomp, parameters, im, null);
                                        tiff.image.SetInverted(true);
                                        tiff.image.SetDpi(dpiX, dpiY);
                                        tiff.image.SetXYRatio(XYRatio);
                                        if (rotation != 0) {
                                            tiff.image.SetRotation(rotation);
                                        }
                                        return;
                                    }
                                }
                                g4.Fax4Encode(outBuf, height);
                                break;
                            }

                            case TIFFConstants.COMPRESSION_CCITTFAX4: {
                                try {
                                    decoder.DecodeT6(outBuf, im, 0, height, tiffT6Options);
                                }
                                catch (IOException e) {
                                    if (!recoverFromImageError) {
                                        throw;
                                    }
                                }
                                g4.Fax4Encode(outBuf, height);
                                break;
                            }
                        }
                        rowsLeft -= rowsStrip;
                    }
                    byte[] g4pic = g4.Close();
                    RawImageHelper.UpdateRawImageParameters(tiff.image, w, h, false, RawImageData.CCITTG4, parameters & RawImageData
                        .CCITT_BLACKIS1, g4pic, null);
                }
                tiff.image.SetDpi(dpiX, dpiY);
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ICCPROFILE)) {
                    try {
                        TIFFField fd = dir.GetField(TIFFConstants.TIFFTAG_ICCPROFILE);
                        var icc_prof = IccProfile.GetInstance(fd.GetAsBytes());
                        if (icc_prof.GetNumComponents() == 1) {
                            tiff.image.SetProfile(icc_prof);
                        }
                    }
                    catch (Exception) {
                    }
                }
                //empty
                if (rotation != 0) {
                    tiff.image.SetRotation(rotation);
                }
            }
            catch (Exception) {
                throw new IOException(IOException.CannotReadTiffImage);
            }
        }

        private static void ProcessTiffImageColor(TIFFDirectory dir, RandomAccessFileOrArray s, TiffParameters
             tiff) {
            try {
                var compression = TIFFConstants.COMPRESSION_NONE;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_COMPRESSION)) {
                    compression = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_COMPRESSION);
                }
                var predictor = 1;
                TIFFLZWDecoder lzwDecoder = null;
                switch (compression) {
                    case TIFFConstants.COMPRESSION_NONE:
                    case TIFFConstants.COMPRESSION_LZW:
                    case TIFFConstants.COMPRESSION_PACKBITS:
                    case TIFFConstants.COMPRESSION_DEFLATE:
                    case TIFFConstants.COMPRESSION_ADOBE_DEFLATE:
                    case TIFFConstants.COMPRESSION_OJPEG:
                    case TIFFConstants.COMPRESSION_JPEG: {
                        break;
                    }

                    default: {
                        throw new IOException(IOException.Compression1IsNotSupported).SetMessageParams(compression
                            );
                    }
                }
                var photometric = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_PHOTOMETRIC);
                switch (photometric) {
                    case TIFFConstants.PHOTOMETRIC_MINISWHITE:
                    case TIFFConstants.PHOTOMETRIC_MINISBLACK:
                    case TIFFConstants.PHOTOMETRIC_RGB:
                    case TIFFConstants.PHOTOMETRIC_SEPARATED:
                    case TIFFConstants.PHOTOMETRIC_PALETTE: {
                        break;
                    }

                    default: {
                        if (compression != TIFFConstants.COMPRESSION_OJPEG && compression != TIFFConstants.COMPRESSION_JPEG) {
                            throw new IOException(IOException.Photometric1IsNotSupported).SetMessageParams(photometric
                                );
                        }
                        break;
                    }
                }
                float rotation = 0;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ORIENTATION)) {
                    var rot = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_ORIENTATION);
                    if (rot == TIFFConstants.ORIENTATION_BOTRIGHT || rot == TIFFConstants.ORIENTATION_BOTLEFT) {
                        rotation = (float)Math.PI;
                    }
                    else {
                        if (rot == TIFFConstants.ORIENTATION_LEFTTOP || rot == TIFFConstants.ORIENTATION_LEFTBOT) {
                            rotation = (float)(Math.PI / 2.0);
                        }
                        else {
                            if (rot == TIFFConstants.ORIENTATION_RIGHTTOP || rot == TIFFConstants.ORIENTATION_RIGHTBOT) {
                                rotation = -(float)(Math.PI / 2.0);
                            }
                        }
                    }
                }
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_PLANARCONFIG) && dir.GetFieldAsLong(TIFFConstants.TIFFTAG_PLANARCONFIG
                    ) == TIFFConstants.PLANARCONFIG_SEPARATE) {
                    throw new IOException(IOException.PlanarImagesAreNotSupported);
                }
                var extraSamples = 0;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_EXTRASAMPLES)) {
                    extraSamples = 1;
                }
                var samplePerPixel = 1;
                // 1,3,4
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_SAMPLESPERPIXEL)) {
                    samplePerPixel = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_SAMPLESPERPIXEL);
                }
                var bitsPerSample = 1;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_BITSPERSAMPLE)) {
                    bitsPerSample = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_BITSPERSAMPLE);
                }
                switch (bitsPerSample) {
                    case 1:
                    case 2:
                    case 4:
                    case 8: {
                        break;
                    }

                    default: {
                        throw new IOException(IOException.BitsPerSample1IsNotSupported).SetMessageParams(bitsPerSample
                            );
                    }
                }
                var h = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_IMAGELENGTH);
                var w = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_IMAGEWIDTH);
                int dpiX;
                int dpiY;
                var resolutionUnit = TIFFConstants.RESUNIT_INCH;
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_RESOLUTIONUNIT)) {
                    resolutionUnit = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_RESOLUTIONUNIT);
                }
                dpiX = GetDpi(dir.GetField(TIFFConstants.TIFFTAG_XRESOLUTION), resolutionUnit);
                dpiY = GetDpi(dir.GetField(TIFFConstants.TIFFTAG_YRESOLUTION), resolutionUnit);
                var fillOrder = 1;
                TIFFField fillOrderField = dir.GetField(TIFFConstants.TIFFTAG_FILLORDER);
                if (fillOrderField != null) {
                    fillOrder = fillOrderField.GetAsInt(0);
                }
                var reverse = (fillOrder == TIFFConstants.FILLORDER_LSB2MSB);
                var rowsStrip = h;
                // another hack for broken tiffs
                if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ROWSPERSTRIP)) {
                    rowsStrip = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_ROWSPERSTRIP);
                }
                if (rowsStrip <= 0 || rowsStrip > h) {
                    rowsStrip = h;
                }
                var offset = GetArrayLongShort(dir, TIFFConstants.TIFFTAG_STRIPOFFSETS);
                var size = GetArrayLongShort(dir, TIFFConstants.TIFFTAG_STRIPBYTECOUNTS);
                // some TIFF producers are really lousy, so...
                if ((size == null || (size.Length == 1 && (size[0] == 0 || size[0] + offset[0] > s.Length()))) && h == rowsStrip
                    ) {
                    size = new[] { s.Length() - (int)offset[0] };
                }
                if (compression == TIFFConstants.COMPRESSION_LZW || compression == TIFFConstants.COMPRESSION_DEFLATE || compression
                     == TIFFConstants.COMPRESSION_ADOBE_DEFLATE) {
                    TIFFField predictorField = dir.GetField(TIFFConstants.TIFFTAG_PREDICTOR);
                    if (predictorField != null) {
                        predictor = predictorField.GetAsInt(0);
                        if (predictor != 1 && predictor != 2) {
                            throw new IOException(IOException.IllegalValueForPredictorInTiffFile);
                        }
                        if (predictor == 2 && bitsPerSample != 8) {
                            throw new IOException(IOException._1BitSamplesAreNotSupportedForHorizontalDifferencingPredictor
                                ).SetMessageParams(bitsPerSample);
                        }
                    }
                }
                if (compression == TIFFConstants.COMPRESSION_LZW) {
                    lzwDecoder = new TIFFLZWDecoder(w, predictor, samplePerPixel);
                }
                var rowsLeft = h;
                ByteArrayOutputStream stream = null;
                ByteArrayOutputStream mstream = null;
                DeflaterOutputStream zip = null;
                DeflaterOutputStream mzip = null;
                if (extraSamples > 0) {
                    mstream = new ByteArrayOutputStream();
                    mzip = new DeflaterOutputStream(mstream);
                }
                Ccittg4Encoder g4 = null;
                if (bitsPerSample == 1 && samplePerPixel == 1 && photometric != TIFFConstants.PHOTOMETRIC_PALETTE) {
                    g4 = new Ccittg4Encoder(w);
                }
                else {
                    stream = new ByteArrayOutputStream();
                    if (compression != TIFFConstants.COMPRESSION_OJPEG && compression != TIFFConstants.COMPRESSION_JPEG) {
                        zip = new DeflaterOutputStream(stream);
                    }
                }
                if (compression == TIFFConstants.COMPRESSION_OJPEG) {
                    // Assume that the TIFFTAG_JPEGIFBYTECOUNT tag is optional, since it's obsolete and
                    // is often missing
                    if ((!dir.IsTagPresent(TIFFConstants.TIFFTAG_JPEGIFOFFSET))) {
                        throw new IOException(IOException.MissingTagsForOjpegCompression);
                    }
                    var jpegOffset = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_JPEGIFOFFSET);
                    var jpegLength = (int)s.Length() - jpegOffset;
                    if (dir.IsTagPresent(TIFFConstants.TIFFTAG_JPEGIFBYTECOUNT)) {
                        jpegLength = (int)dir.GetFieldAsLong(TIFFConstants.TIFFTAG_JPEGIFBYTECOUNT) + (int)size[0];
                    }
                    var jpeg = new byte[Math.Min(jpegLength, (int)s.Length() - jpegOffset)];
                    var posFilePointer = (int)s.GetPosition();
                    posFilePointer += jpegOffset;
                    s.Seek(posFilePointer);
                    s.ReadFully(jpeg);
                    tiff.image.data = jpeg;
                    tiff.image.SetOriginalType(ImageType.JPEG);
                    JpegImageHelper.ProcessImage(tiff.image);
                    tiff.jpegProcessing = true;
                }
                else {
                    if (compression == TIFFConstants.COMPRESSION_JPEG) {
                        if (size.Length > 1) {
                            throw new IOException(IOException.CompressionJpegIsOnlySupportedWithASingleStripThisImageHas1Strips
                                ).SetMessageParams(size.Length);
                        }
                        var jpeg = new byte[(int)size[0]];
                        s.Seek(offset[0]);
                        s.ReadFully(jpeg);
                        // if quantization and/or Huffman tables are stored separately in the tiff,
                        // we need to add them to the jpeg data
                        TIFFField jpegtables = dir.GetField(TIFFConstants.TIFFTAG_JPEGTABLES);
                        if (jpegtables != null) {
                            var temp = jpegtables.GetAsBytes();
                            var tableoffset = 0;
                            var tablelength = temp.Length;
                            // remove FFD8 from start
                            if (temp[0] == 0xFF && temp[1] == 0xD8) {
                                tableoffset = 2;
                                tablelength -= 2;
                            }
                            // remove FFD9 from end
                            if (temp[temp.Length - 2] == 0xFF && temp[temp.Length - 1] == 0xD9) {
                                tablelength -= 2;
                            }
                            var tables = new byte[tablelength];
                            Array.Copy(temp, tableoffset, tables, 0, tablelength);
                            // TODO insert after JFIF header, instead of at the start
                            var jpegwithtables = new byte[jpeg.Length + tables.Length];
                            Array.Copy(jpeg, 0, jpegwithtables, 0, 2);
                            Array.Copy(tables, 0, jpegwithtables, 2, tables.Length);
                            Array.Copy(jpeg, 2, jpegwithtables, tables.Length + 2, jpeg.Length - 2);
                            jpeg = jpegwithtables;
                        }
                        tiff.image.data = jpeg;
                        tiff.image.SetOriginalType(ImageType.JPEG);
                        JpegImageHelper.ProcessImage(tiff.image);
                        tiff.jpegProcessing = true;
                        if (photometric == TIFFConstants.PHOTOMETRIC_RGB) {
                            tiff.image.SetColorTransform(0);
                        }
                    }
                    else {
                        for (var k = 0; k < offset.Length; ++k) {
                            var im = new byte[(int)size[k]];
                            s.Seek(offset[k]);
                            s.ReadFully(im);
                            var height = Math.Min(rowsStrip, rowsLeft);
                            byte[] outBuf = null;
                            if (compression != TIFFConstants.COMPRESSION_NONE) {
                                outBuf = new byte[(w * bitsPerSample * samplePerPixel + 7) / 8 * height];
                            }
                            if (reverse) {
                                TIFFFaxDecoder.ReverseBits(im);
                            }
                            switch (compression) {
                                case TIFFConstants.COMPRESSION_DEFLATE:
                                case TIFFConstants.COMPRESSION_ADOBE_DEFLATE: {
                                    FilterUtil.InflateData(im, outBuf);
                                    ApplyPredictor(outBuf, predictor, w, height, samplePerPixel);
                                    break;
                                }

                                case TIFFConstants.COMPRESSION_NONE: {
                                    outBuf = im;
                                    break;
                                }

                                case TIFFConstants.COMPRESSION_PACKBITS: {
                                    DecodePackbits(im, outBuf);
                                    break;
                                }

                                case TIFFConstants.COMPRESSION_LZW: {
                                    lzwDecoder.Decode(im, outBuf, height);
                                    break;
                                }
                            }
                            if (bitsPerSample == 1 && samplePerPixel == 1 && photometric != TIFFConstants.PHOTOMETRIC_PALETTE) {
                                g4.Fax4Encode(outBuf, height);
                            }
                            else {
                                if (extraSamples > 0) {
                                    ProcessExtraSamples(zip, mzip, outBuf, samplePerPixel, bitsPerSample, w, height);
                                }
                                else {
                                    zip.Write(outBuf);
                                }
                            }
                            rowsLeft -= rowsStrip;
                        }
                        if (bitsPerSample == 1 && samplePerPixel == 1 && photometric != TIFFConstants.PHOTOMETRIC_PALETTE) {
                            RawImageHelper.UpdateRawImageParameters(tiff.image, w, h, false, RawImageData.CCITTG4, photometric == TIFFConstants
                                .PHOTOMETRIC_MINISBLACK ? RawImageData.CCITT_BLACKIS1 : 0, g4.Close(), null);
                        }
                        else {
                            zip.Dispose();
                            RawImageHelper.UpdateRawImageParameters(tiff.image, w, h, samplePerPixel - extraSamples, bitsPerSample, stream
                                .ToArray());
                            tiff.image.SetDeflated(true);
                        }
                    }
                }
                tiff.image.SetDpi(dpiX, dpiY);
                if (compression != TIFFConstants.COMPRESSION_OJPEG && compression != TIFFConstants.COMPRESSION_JPEG) {
                    if (dir.IsTagPresent(TIFFConstants.TIFFTAG_ICCPROFILE)) {
                        try {
                            TIFFField fd = dir.GetField(TIFFConstants.TIFFTAG_ICCPROFILE);
                            var icc_prof = IccProfile.GetInstance(fd.GetAsBytes());
                            if (samplePerPixel - extraSamples == icc_prof.GetNumComponents()) {
                                tiff.image.SetProfile(icc_prof);
                            }
                        }
                        catch (Exception) {
                        }
                    }
                    //empty
                    if (dir.IsTagPresent(TIFFConstants.TIFFTAG_COLORMAP)) {
                        TIFFField fd = dir.GetField(TIFFConstants.TIFFTAG_COLORMAP);
                        var rgb = fd.GetAsChars();
                        var palette = new byte[rgb.Length];
                        var gColor = rgb.Length / 3;
                        var bColor = gColor * 2;
                        for (var k = 0; k < gColor; ++k) {
                            //there is no sense in >>> for unsigned char
                            palette[k * 3] = (byte)(rgb[k] >> 8);
                            palette[k * 3 + 1] = (byte)(rgb[k + gColor] >> 8);
                            palette[k * 3 + 2] = (byte)(rgb[k + bColor] >> 8);
                        }
                        // Colormap components are supposed to go from 0 to 655535 but,
                        // as usually, some tiff producers just put values from 0 to 255.
                        // Let's check for these broken tiffs.
                        var colormapBroken = true;
                        for (var k = 0; k < palette.Length; ++k) {
                            if (palette[k] != 0) {
                                colormapBroken = false;
                                break;
                            }
                        }
                        if (colormapBroken) {
                            for (var k = 0; k < gColor; ++k) {
                                palette[k * 3] = (byte)rgb[k];
                                palette[k * 3 + 1] = (byte)rgb[k + gColor];
                                palette[k * 3 + 2] = (byte)rgb[k + bColor];
                            }
                        }
                        var indexed = new object[4];
                        indexed[0] = "/Indexed";
                        indexed[1] = "/DeviceRGB";
                        indexed[2] = gColor - 1;
                        indexed[3] = PdfEncodings.ConvertToString(palette, null);
                        tiff.additional = new Dictionary<string, object>();
                        tiff.additional.Put("ColorSpace", indexed);
                    }
                }
                if (photometric == TIFFConstants.PHOTOMETRIC_MINISWHITE) {
                    tiff.image.SetInverted(true);
                }
                if (rotation != 0) {
                    tiff.image.SetRotation(rotation);
                }
                if (extraSamples > 0) {
                    mzip.Dispose();
                    var mimg = (RawImageData)ImageDataFactory.CreateRawImage(null);
                    RawImageHelper.UpdateRawImageParameters(mimg, w, h, 1, bitsPerSample, mstream.ToArray());
                    mimg.MakeMask();
                    mimg.SetDeflated(true);
                    tiff.image.SetImageMask(mimg);
                }
            }
            catch (Exception) {
                throw new IOException(IOException.CannotGetTiffImageColor);
            }
        }

        private static int GetDpi(TIFFField fd, int resolutionUnit) {
            if (fd == null) {
                return 0;
            }
            long[] res = fd.GetAsRational(0);
            var frac = res[0] / (float)res[1];
            var dpi = 0;
            switch (resolutionUnit) {
                case TIFFConstants.RESUNIT_INCH:
                case TIFFConstants.RESUNIT_NONE: {
                    dpi = (int)(frac + 0.5);
                    break;
                }

                case TIFFConstants.RESUNIT_CENTIMETER: {
                    dpi = (int)(frac * 2.54 + 0.5);
                    break;
                }
            }
            return dpi;
        }

        private static void ProcessExtraSamples(DeflaterOutputStream zip, DeflaterOutputStream mzip, byte[] outBuf
            , int samplePerPixel, int bitsPerSample, int width, int height) {
            if (bitsPerSample == 8) {
                var mask = new byte[width * height];
                var mptr = 0;
                var optr = 0;
                var total = width * height * samplePerPixel;
                for (var k = 0; k < total; k += samplePerPixel) {
                    for (var s = 0; s < samplePerPixel - 1; ++s) {
                        outBuf[optr++] = outBuf[k + s];
                    }
                    mask[mptr++] = outBuf[k + samplePerPixel - 1];
                }
                zip.Write(outBuf, 0, optr);
                mzip.Write(mask, 0, mptr);
            }
            else {
                throw new IOException(IOException.ExtraSamplesAreNotSupported);
            }
        }

        private static long[] GetArrayLongShort(TIFFDirectory dir, int tag) {
            TIFFField field = dir.GetField(tag);
            if (field == null) {
                return null;
            }
            long[] offset;
            if (field.GetFieldType() == TIFFField.TIFF_LONG) {
                offset = field.GetAsLongs();
            }
            else {
                // must be short
                var temp = field.GetAsChars();
                offset = new long[temp.Length];
                for (var k = 0; k < temp.Length; ++k) {
                    offset[k] = temp[k];
                }
            }
            return offset;
        }

        // Uncompress packbits compressed image data.
        private static void DecodePackbits(byte[] data, byte[] dst) {
            var srcCount = 0;
            var dstCount = 0;
            byte repeat;
            byte b;
            try {
                while (dstCount < dst.Length) {
                    b = data[srcCount++];
                    // In Java b <= 127 is always true and the same is for .NET and b >= 0 expression,
                    // checking both for the sake of consistency.
                    if (b >= 0 && b <= 127) {
                        // literal run packet
                        for (var i = 0; i < (b + 1); i++) {
                            dst[dstCount++] = data[srcCount++];
                        }
                    }
                    else {
                        // It seems that in Java and .NET (b & 0x80) != 0 would always be true here, however still checking it
                        // to be more explicit.
                        if ((b & 0x80) != 0 && b != 0x80) {
                            // 2 byte encoded run packet
                            repeat = data[srcCount++];
                            // (~b & 0xff) + 2 is getting -b + 1 via bitwise operations,
                            // treating b as signed byte. This approach works both for Java and .NET.
                            // This is because `~x == (-x) - 1` for signed number values.
                            for (var i = 0; i < (~b & 0xff) + 2; i++) {
                                dst[dstCount++] = repeat;
                            }
                        }
                        else {
                            // no-op packet. Do nothing
                            srcCount++;
                        }
                    }
                }
            }
            catch (Exception) {
            }
        }

        // do nothing
        private static void ApplyPredictor(byte[] uncompData, int predictor, int w, int h, int samplesPerPixel) {
            if (predictor != 2) {
                return;
            }
            int count;
            for (var j = 0; j < h; j++) {
                count = samplesPerPixel * (j * w + 1);
                for (var i = samplesPerPixel; i < w * samplesPerPixel; i++) {
                    uncompData[count] += uncompData[count - samplesPerPixel];
                    count++;
                }
            }
        }
    }
}
