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
using System.IO;
using System.Text;
using IText.IO.Font.Constants;
using IText.IO.Source;
using IText.IO.Util;

namespace  IText.IO.Font {
    internal class Type1Parser {
        private const string AFM_HEADER = "StartFontMetrics";

        private string afmPath;

        private string pfbPath;

        private byte[] pfbData;

        private byte[] afmData;

        private bool isBuiltInFont;

        private RandomAccessSourceFactory sourceFactory = new RandomAccessSourceFactory();

        /// <summary>Creates a new Type1 font file.</summary>
        /// <param name="afm">the AFM file if the input is made with a <c>byte</c> array</param>
        /// <param name="pfb">the PFB file if the input is made with a <c>byte</c> array</param>
        /// <param name="metricsPath">the name of one of the 14 built-in fonts or the location of an AFM file. The file must end in '.afm'
        ///     </param>
        public Type1Parser(string metricsPath, string binaryPath, byte[] afm, byte[] pfb) {
            afmData = afm;
            pfbData = pfb;
            afmPath = metricsPath;
            pfbPath = binaryPath;
        }

        public virtual RandomAccessFileOrArray GetMetricsFile() {
            isBuiltInFont = false;
            if (StandardFonts.IsStandardFont(afmPath)) {
                isBuiltInFont = true;
                var buf = new byte[1024];
                Stream resource = null;
                try {
                    var resourcePath = FontResources.AFMS + afmPath + ".afm";
                    resource = ResourceUtil.GetResourceStream(resourcePath);
                    if (resource == null) {
                        throw new IOException("{0} was not found as resource.").SetMessageParams(resourcePath);
                    }
                    var stream = new MemoryStream();
                    int read;
                    while ((read = resource.Read(buf)) >= 0) {
                        stream.Write(buf, 0, read);
                    }
                    buf = stream.ToArray();
                }
                finally {
                    if (resource != null) {
                        try {
                            resource.Dispose();
                        }
                        catch (Exception) {
                        }
                    }
                }
                return new RandomAccessFileOrArray(sourceFactory.CreateSource(buf));
            }

            if (afmPath != null)
            {
	            if (afmPath.ToLowerInvariant().EndsWith(".afm")) {
		            return new RandomAccessFileOrArray(sourceFactory.CreateBestSource(afmPath));
	            }

	            if (afmPath.ToLowerInvariant().EndsWith(".pfm")) {
		            var ba = new MemoryStream();
		            var rf = new RandomAccessFileOrArray(sourceFactory.CreateBestSource(afmPath));
		            Pfm2afm.Convert(rf, ba);
		            rf.Close();
		            return new RandomAccessFileOrArray(sourceFactory.CreateSource(ba.ToArray()));
	            }

	            throw new IOException(IOException._1IsNotAnAfmOrPfmFontFile).SetMessageParams(afmPath);
            }

            if (afmData != null) {
	            var rf = new RandomAccessFileOrArray(sourceFactory.CreateSource(afmData));
	            if (IsAfmFile(rf)) {
		            return rf;
	            }

	            var ba = new MemoryStream();
	            try {
		            Pfm2afm.Convert(rf, ba);
	            }
	            catch (Exception) {
		            throw new IOException("Invalid afm or pfm font file.");
	            }
	            finally {
		            rf.Close();
	            }
	            return new RandomAccessFileOrArray(sourceFactory.CreateSource(ba.ToArray()));
            }

            throw new IOException("Invalid afm or pfm font file.");
        }

        public virtual RandomAccessFileOrArray GetPostscriptBinary()
        {
	        if (pfbData != null) {
                return new RandomAccessFileOrArray(sourceFactory.CreateSource(pfbData));
            }

	        if (pfbPath != null && pfbPath.ToLowerInvariant().EndsWith(".pfb")) {
		        return new RandomAccessFileOrArray(sourceFactory.CreateBestSource(pfbPath));
	        }

	        pfbPath = afmPath.JSubstring(0, afmPath.Length - 3) + "pfb";
	        return new RandomAccessFileOrArray(sourceFactory.CreateBestSource(pfbPath));
        }

        public virtual bool IsBuiltInFont() {
            return isBuiltInFont;
        }

        public virtual string GetAfmPath() {
            return afmPath;
        }

        private bool IsAfmFile(RandomAccessFileOrArray raf) {
            var builder = new StringBuilder(AFM_HEADER.Length);
            for (var i = 0; i < AFM_HEADER.Length; i++) {
                try {
                    builder.Append((char)raf.ReadByte());
                }
                catch (EndOfStreamException) {
                    raf.Seek(0);
                    return false;
                }
            }
            raf.Seek(0);
            return AFM_HEADER.Equals(builder.ToString());
        }
    }
}
