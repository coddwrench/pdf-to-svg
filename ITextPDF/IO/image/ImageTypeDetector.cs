/*
This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: iText Software.

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://itextpdf.com/sales.  For AGPL licensing, see below.

AGPL licensing:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using IText.IO.Util;

namespace  IText.IO.Image {
    /// <summary>Helper class that detects image type by magic bytes</summary>
    public sealed class ImageTypeDetector {
        private static readonly byte[] gif = { (byte)'G', (byte)'I', (byte)'F' };

        private static readonly byte[] jpeg = { 0xFF, 0xD8 };

        private static readonly byte[] jpeg2000_1 = { 0x00, 0x00, 0x00, 0x0c };

        private static readonly byte[] jpeg2000_2 = { 0xff, 0x4f, 0xff, 0x51 };

        private static readonly byte[] png = { 137, 80, 78, 71 };

        private static readonly byte[] wmf = { 0xD7, 0xCD };

        private static readonly byte[] bmp = { (byte)'B', (byte)'M' };

        private static readonly byte[] tiff_1 = { (byte)'M', (byte)'M', 0, 42 };

        private static readonly byte[] tiff_2 = { (byte)'I', (byte)'I', 42, 0 };

        private static readonly byte[] jbig2 = { 0x97, (byte)'J', (byte)'B', (byte)'2', (byte)'\r'
            , (byte)'\n', 0x1a, (byte)'\n' };

        private ImageTypeDetector() {
        }

        /// <summary>Detect image type by magic bytes given the byte array source.</summary>
        /// <param name="source">image bytes</param>
        /// <returns>
        /// detected image type, see
        /// <see cref="ImageType"/>
        /// . Returns
        /// <see cref="ImageType.NONE"/>
        /// if image type is unknown
        /// </returns>
        public static ImageType DetectImageType(byte[] source) {
            var header = ReadImageType(source);
            return DetectImageTypeByHeader(header);
        }

        /// <summary>Detect image type by magic bytes given the source URL.</summary>
        /// <param name="source">image URL</param>
        /// <returns>
        /// detected image type, see
        /// <see cref="ImageType"/>
        /// . Returns
        /// <see cref="ImageType.NONE"/>
        /// if image type is unknown
        /// </returns>
        public static ImageType DetectImageType(Uri source) {
            var header = ReadImageType(source);
            return DetectImageTypeByHeader(header);
        }

        /// <summary>Detect image type by magic bytes given the input stream.</summary>
        /// <param name="stream">image stream</param>
        /// <returns>
        /// detected image type, see
        /// <see cref="ImageType"/>
        /// . Returns
        /// <see cref="ImageType.NONE"/>
        /// if image type is unknown
        /// </returns>
        public static ImageType DetectImageType(Stream stream) {
            var header = ReadImageType(stream);
            return DetectImageTypeByHeader(header);
        }

        private static ImageType DetectImageTypeByHeader(byte[] header) {
            if (ImageTypeIs(header, gif)) {
                return ImageType.GIF;
            }

            if (ImageTypeIs(header, jpeg)) {
	            return ImageType.JPEG;
            }

            if (ImageTypeIs(header, jpeg2000_1) || ImageTypeIs(header, jpeg2000_2)) {
	            return ImageType.JPEG2000;
            }

            if (ImageTypeIs(header, png)) {
	            return ImageType.PNG;
            }

            if (ImageTypeIs(header, bmp)) {
	            return ImageType.BMP;
            }

            if (ImageTypeIs(header, tiff_1) || ImageTypeIs(header, tiff_2)) {
	            return ImageType.TIFF;
            }

            if (ImageTypeIs(header, jbig2)) {
	            return ImageType.JBIG2;
            }

            if (ImageTypeIs(header, wmf)) {
	            return ImageType.WMF;
            }
            return ImageType.NONE;
        }

        private static bool ImageTypeIs(byte[] imageType, byte[] compareWith) {
            for (var i = 0; i < compareWith.Length; i++) {
                if (imageType[i] != compareWith[i]) {
                    return false;
                }
            }
            return true;
        }

        private static byte[] ReadImageType(Uri source) {
            try {
                using (var stream = UrlUtil.OpenStream(source)) {
                    return ReadImageType(stream);
                }
            }
            catch (System.IO.IOException e) {
                throw new IOException(IOException.IoException, e);
            }
        }

        private static byte[] ReadImageType(Stream stream) {
            try {
                var bytes = new byte[8];
                stream.Read(bytes);
                return bytes;
            }
            catch (System.IO.IOException e) {
                throw new IOException(IOException.IoException, e);
            }
        }

        private static byte[] ReadImageType(byte[] source) {
            try {
                Stream stream = new MemoryStream(source);
                var bytes = new byte[8];
                stream.Read(bytes);
                return bytes;
            }
            catch (System.IO.IOException) {
                return null;
            }
        }
    }
}
