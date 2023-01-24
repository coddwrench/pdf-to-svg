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

using System.Diagnostics;
using System.IO;
using IText.IO;
using IText.IO.Source;
using IText.IO.Util;
using IText.Kernel.Crypto;
using IText.Kernel.Pdf.Filters;
using IText.Logger;
using IOException = System.IO.IOException;

namespace IText.Kernel.Pdf {
    public class PdfOutputStream : OutputStream<PdfOutputStream> {
        private static readonly byte[] Stream = ByteUtils.GetIsoBytes("stream\n");

        private static readonly byte[] Endstream = ByteUtils.GetIsoBytes("\nendstream");

        private static readonly byte[] OpenDict = ByteUtils.GetIsoBytes("<<");

        private static readonly byte[] CloseDict = ByteUtils.GetIsoBytes(">>");

        private static readonly byte[] EndIndirect = ByteUtils.GetIsoBytes(" R");

        private static readonly byte[] EndIndirectWithZeroGenNr = ByteUtils.GetIsoBytes(" 0 R");

        // For internal usage only
        private byte[] _duplicateContentBuffer = null;

        /// <summary>Document associated with PdfOutputStream.</summary>
        protected internal PdfDocument Document = null;

        /// <summary>Contains the business logic for cryptography.</summary>
        protected internal PdfEncryption Crypto;

        /// <summary>Create a pdfOutputSteam writing to the passed OutputStream.</summary>
        /// <param name="outputStream">Outputstream to write to.</param>
        public PdfOutputStream(Stream outputStream)
            : base(outputStream) {
        }

        /// <summary>Write a PdfObject to the outputstream.</summary>
        /// <param name="pdfObject">PdfObject to write</param>
        /// <returns>this PdfOutPutStream</returns>
        public virtual PdfOutputStream Write(PdfObject pdfObject) {
            if (pdfObject.CheckState(PdfObject.MUST_BE_INDIRECT) && Document != null) {
                pdfObject.MakeIndirect(Document);
                pdfObject = pdfObject.GetIndirectReference();
            }
            if (pdfObject.CheckState(PdfObject.READ_ONLY)) {
                throw new PdfException(PdfException.CannotWriteObjectAfterItWasReleased);
            }
            switch (pdfObject.GetObjectType()) {
                case PdfObject.ARRAY: {
                    Write((PdfArray)pdfObject);
                    break;
                }

                case PdfObject.DICTIONARY: {
                    Write((PdfDictionary)pdfObject);
                    break;
                }

                case PdfObject.INDIRECT_REFERENCE: {
                    Write((PdfIndirectReference)pdfObject);
                    break;
                }

                case PdfObject.NAME: {
                    Write((PdfName)pdfObject);
                    break;
                }

                case PdfObject.NULL:
                case PdfObject.BOOLEAN: {
                    Write((PdfPrimitiveObject)pdfObject);
                    break;
                }

                case PdfObject.LITERAL: {
                    Write((PdfLiteral)pdfObject);
                    break;
                }

                case PdfObject.STRING: {
                    Write((PdfString)pdfObject);
                    break;
                }

                case PdfObject.NUMBER: {
                    Write((PdfNumber)pdfObject);
                    break;
                }

                case PdfObject.STREAM: {
                    Write((PdfStream)pdfObject);
                    break;
                }
            }
            return this;
        }

        /// <summary>Writes corresponding amount of bytes from a given long</summary>
        /// <param name="bytes">a source of bytes, must be &gt;= 0</param>
        /// <param name="size">expected amount of bytes</param>
        internal virtual void Write(long bytes, int size) {
            Debug.Assert(bytes >= 0);
            while (--size >= 0) {
                Write((byte)(bytes >> 8 * size & 0xff));
            }
        }

        /// <summary>Writes corresponding amount of bytes from a given int</summary>
        /// <param name="bytes">a source of bytes, must be &gt;= 0</param>
        /// <param name="size">expected amount of bytes</param>
        internal virtual void Write(int bytes, int size) {
            //safe convert to long, despite sign.
            Write(bytes & 0xFFFFFFFFL, size);
        }

        private void Write(PdfArray pdfArray) {
            WriteByte('[');
            for (var i = 0; i < pdfArray.Size(); i++) {
                var value = pdfArray.Get(i, false);
                PdfIndirectReference indirectReference;
                if ((indirectReference = value.GetIndirectReference()) != null) {
                    Write(indirectReference);
                }
                else {
                    Write(value);
                }
                if (i < pdfArray.Size() - 1) {
                    WriteSpace();
                }
            }
            WriteByte(']');
        }

        private void Write(PdfDictionary pdfDictionary) {
            WriteBytes(OpenDict);
            foreach (var key in pdfDictionary.KeySet()) {
                var isAlreadyWriteSpace = false;
                Write(key);
                var value = pdfDictionary.Get(key, false);
                if (value == null) {
                    var logger = LogManager.GetLogger(typeof(PdfOutputStream));
                    logger.Warn(MessageFormatUtil.Format(LogMessageConstant.INVALID_KEY_VALUE_KEY_0_HAS_NULL_VALUE, key
                        ));
                    value = PdfNull.PDF_NULL;
                }
                if ((value.GetObjectType() == PdfObject.NUMBER || value.GetObjectType() == PdfObject.LITERAL || value.GetObjectType
                    () == PdfObject.BOOLEAN || value.GetObjectType() == PdfObject.NULL || value.GetObjectType() == PdfObject
                    .INDIRECT_REFERENCE || value.CheckState(PdfObject.MUST_BE_INDIRECT))) {
                    isAlreadyWriteSpace = true;
                    WriteSpace();
                }
                PdfIndirectReference indirectReference;
                if ((indirectReference = value.GetIndirectReference()) != null) {
                    if (!isAlreadyWriteSpace) {
                        WriteSpace();
                    }
                    Write(indirectReference);
                }
                else {
                    Write(value);
                }
            }
            WriteBytes(CloseDict);
        }

        private void Write(PdfIndirectReference indirectReference) {
            if (Document != null && !indirectReference.GetDocument().Equals(Document)) {
                throw new PdfException(PdfException.PdfIndirectObjectBelongsToOtherPdfDocument);
            }
            if (indirectReference.IsFree()) {
                var logger = LogManager.GetLogger(typeof(PdfOutputStream));
                logger.Error(LogMessageConstant.FLUSHED_OBJECT_CONTAINS_FREE_REFERENCE);
                Write(PdfNull.PDF_NULL);
            }
            else {
                if (indirectReference.refersTo == null && (indirectReference.CheckState(PdfObject.MODIFIED) || indirectReference
                    .GetReader() == null || !(indirectReference.GetOffset() > 0 || indirectReference.GetIndex() >= 0))) {
                    var logger = LogManager.GetLogger(typeof(PdfOutputStream));
                    logger.Error(LogMessageConstant.FLUSHED_OBJECT_CONTAINS_REFERENCE_WHICH_NOT_REFER_TO_ANY_OBJECT);
                    Write(PdfNull.PDF_NULL);
                }
                else {
                    if (indirectReference.GetGenNumber() == 0) {
                        WriteInteger(indirectReference.GetObjNumber()).WriteBytes(EndIndirectWithZeroGenNr);
                    }
                    else {
                        WriteInteger(indirectReference.GetObjNumber()).WriteSpace().WriteInteger(indirectReference.GetGenNumber())
                            .WriteBytes(EndIndirect);
                    }
                }
            }
        }

        private void Write(PdfPrimitiveObject pdfPrimitive) {
            WriteBytes(pdfPrimitive.GetInternalContent());
        }

        private void Write(PdfLiteral literal) {
            literal.SetPosition(GetCurrentPos());
            WriteBytes(literal.GetInternalContent());
        }

        private void Write(PdfString pdfString) {
            pdfString.Encrypt(Crypto);
            if (pdfString.IsHexWriting()) {
                WriteByte('<');
                WriteBytes(pdfString.GetInternalContent());
                WriteByte('>');
            }
            else {
                WriteByte('(');
                WriteBytes(pdfString.GetInternalContent());
                WriteByte(')');
            }
        }

        private void Write(PdfName name) {
            WriteByte('/');
            WriteBytes(name.GetInternalContent());
        }

        private void Write(PdfNumber pdfNumber) {
            if (pdfNumber.HasContent()) {
                WriteBytes(pdfNumber.GetInternalContent());
            }
            else {
                if (pdfNumber.IsDoubleNumber()) {
                    WriteDouble(pdfNumber.GetValue());
                }
                else {
                    WriteInteger(pdfNumber.IntValue());
                }
            }
        }

        private bool IsNotMetadataPdfStream(PdfStream pdfStream) {
            return pdfStream.GetAsName(PdfName.Type) == null || (pdfStream.GetAsName(PdfName.Type) != null && !pdfStream
                .GetAsName(PdfName.Type).Equals(PdfName.Metadata));
        }

        private bool IsXRefStream(PdfStream pdfStream) {
            return PdfName.XRef.Equals(pdfStream.GetAsName(PdfName.Type));
        }

        private void Write(PdfStream pdfStream) {
            try {
                var userDefinedCompression = pdfStream.GetCompressionLevel() != CompressionConstants.UNDEFINED_COMPRESSION;
                if (!userDefinedCompression) {
                    var defaultCompressionLevel = Document != null ? Document.GetWriter().GetCompressionLevel() : CompressionConstants
                        .DEFAULT_COMPRESSION;
                    pdfStream.SetCompressionLevel(defaultCompressionLevel);
                }
                var toCompress = pdfStream.GetCompressionLevel() != CompressionConstants.NO_COMPRESSION;
                var allowCompression = !pdfStream.ContainsKey(PdfName.Filter) && IsNotMetadataPdfStream(pdfStream);
                if (pdfStream.GetInputStream() != null) {
                    Stream fout = this;
                    DeflaterOutputStream def = null;
                    OutputStreamEncryption ose = null;
                    if (Crypto != null && (!Crypto.IsEmbeddedFilesOnly() || Document.DoesStreamBelongToEmbeddedFile(pdfStream)
                        )) {
                        fout = ose = Crypto.GetEncryptionStream(fout);
                    }
                    if (toCompress && (allowCompression || userDefinedCompression)) {
                        UpdateCompressionFilter(pdfStream);
                        fout = def = new DeflaterOutputStream(fout, pdfStream.GetCompressionLevel(), 0x8000);
                    }
                    Write((PdfDictionary)pdfStream);
                    WriteBytes(Stream);
                    var beginStreamContent = GetCurrentPos();
                    var buf = new byte[4192];
                    while (true) {
                        var n = pdfStream.GetInputStream().CustomRead(buf);
                        if (n <= 0) {
                            break;
                        }
                        fout.Write(buf, 0, n);
                    }
                    if (def != null) {
                        def.Finish();
                    }
                    if (ose != null) {
                        ose.Finish();
                    }
                    var length = pdfStream.GetAsNumber(PdfName.Length);
                    length.SetValue((int)(GetCurrentPos() - beginStreamContent));
                    pdfStream.UpdateLength(length.IntValue());
                    WriteBytes(Endstream);
                }
                else {
                    //When document is opened in stamping mode the output stream can be uninitialized.
                    //We have to initialize it and write all data from streams input to streams output.
                    if (pdfStream.GetOutputStream() == null && pdfStream.GetIndirectReference().GetReader() != null) {
                        // If new specific compression is set for stream,
                        // then compressed stream should be decoded and written with new compression settings
                        var bytes = pdfStream.GetIndirectReference().GetReader().ReadStreamBytes(pdfStream, false);
                        if (userDefinedCompression) {
                            bytes = DecodeFlateBytes(pdfStream, bytes);
                        }
                        pdfStream.InitOutputStream(new ByteArrayOutputStream(bytes.Length));
                        pdfStream.GetOutputStream().Write(bytes);
                    }
                    Debug.Assert(pdfStream.GetOutputStream() != null, "PdfStream lost OutputStream");
                    ByteArrayOutputStream byteArrayStream;
                    try {
                        if (toCompress && !ContainsFlateFilter(pdfStream) && (allowCompression || userDefinedCompression)) {
                            // compress
                            UpdateCompressionFilter(pdfStream);
                            byteArrayStream = new ByteArrayOutputStream();
                            var zip = new DeflaterOutputStream(byteArrayStream, pdfStream.GetCompressionLevel());
                            if (pdfStream is PdfObjectStream) {
                                var objectStream = (PdfObjectStream)pdfStream;
                                ((ByteArrayOutputStream)objectStream.GetIndexStream().GetOutputStream()).WriteTo(zip);
                                ((ByteArrayOutputStream)objectStream.GetOutputStream().GetOutputStream()).WriteTo(zip);
                            }
                            else {
                                Debug.Assert(pdfStream.GetOutputStream() != null, "Error in outputStream");
                                ((ByteArrayOutputStream)pdfStream.GetOutputStream().GetOutputStream()).WriteTo(zip);
                            }
                            zip.Finish();
                        }
                        else {
                            if (pdfStream is PdfObjectStream) {
                                var objectStream = (PdfObjectStream)pdfStream;
                                byteArrayStream = new ByteArrayOutputStream();
                                ((ByteArrayOutputStream)objectStream.GetIndexStream().GetOutputStream()).WriteTo(byteArrayStream);
                                ((ByteArrayOutputStream)objectStream.GetOutputStream().GetOutputStream()).WriteTo(byteArrayStream);
                            }
                            else {
                                Debug.Assert(pdfStream.GetOutputStream() != null, "Error in outputStream");
                                byteArrayStream = (ByteArrayOutputStream)pdfStream.GetOutputStream().GetOutputStream();
                            }
                        }
                        if (CheckEncryption(pdfStream)) {
                            var encodedStream = new ByteArrayOutputStream();
                            var ose = Crypto.GetEncryptionStream(encodedStream);
                            byteArrayStream.WriteTo(ose);
                            ose.Finish();
                            byteArrayStream = encodedStream;
                        }
                    }
                    catch (IOException ioe) {
                        throw new PdfException(PdfException.IoException, ioe);
                    }
                    pdfStream.Put(PdfName.Length, new PdfNumber(byteArrayStream.Length));
                    pdfStream.UpdateLength((int)byteArrayStream.Length);
                    Write((PdfDictionary)pdfStream);
                    WriteBytes(Stream);
                    byteArrayStream.WriteTo(this);
                    byteArrayStream.Dispose();
                    WriteBytes(Endstream);
                }
            }
            catch (IOException e) {
                throw new PdfException(PdfException.CannotWriteToPdfStream, e, pdfStream);
            }
        }

        protected internal virtual bool CheckEncryption(PdfStream pdfStream)
        {
	        if (Crypto == null || (Crypto.IsEmbeddedFilesOnly() && !Document.DoesStreamBelongToEmbeddedFile(pdfStream)
                )) {
                return false;
            }

	        if (IsXRefStream(pdfStream)) {
		        // The cross-reference stream shall not be encrypted
		        return false;
	        }

	        var filter = pdfStream.Get(PdfName.Filter, true);
	        if (filter != null)
	        {
		        if (PdfName.Crypt.Equals(filter)) {
			        return false;
		        }

		        if (filter.GetObjectType() == PdfObject.ARRAY) {
			        var filters = (PdfArray)filter;
			        if (!filters.IsEmpty() && PdfName.Crypt.Equals(filters.Get(0, true))) {
				        return false;
			        }
		        }
	        }
	        return true;
        }

        protected internal virtual bool ContainsFlateFilter(PdfStream pdfStream) {
            var filter = pdfStream.Get(PdfName.Filter);
            if (filter != null) {
                if (filter.GetObjectType() == PdfObject.NAME) {
                    if (PdfName.FlateDecode.Equals(filter)) {
                        return true;
                    }
                }
                else {
                    if (filter.GetObjectType() == PdfObject.ARRAY) {
                        if (((PdfArray)filter).Contains(PdfName.FlateDecode)) {
                            return true;
                        }
                    }
                    else {
                        throw new PdfException(PdfException.FilterIsNotANameOrArray);
                    }
                }
            }
            return false;
        }

        protected internal virtual void UpdateCompressionFilter(PdfStream pdfStream) {
            var filter = pdfStream.Get(PdfName.Filter);
            if (filter == null) {
                pdfStream.Put(PdfName.Filter, PdfName.FlateDecode);
            }
            else {
                var filters = new PdfArray();
                filters.Add(PdfName.FlateDecode);
                if (filter is PdfArray) {
                    filters.AddAll((PdfArray)filter);
                }
                else {
                    filters.Add(filter);
                }
                var decodeParms = pdfStream.Get(PdfName.DecodeParms);
                if (decodeParms != null) {
                    if (decodeParms is PdfDictionary) {
                        var array = new PdfArray();
                        array.Add(new PdfNull());
                        array.Add(decodeParms);
                        pdfStream.Put(PdfName.DecodeParms, array);
                    }
                    else {
                        if (decodeParms is PdfArray) {
                            ((PdfArray)decodeParms).Add(0, new PdfNull());
                        }
                        else {
                            throw new PdfException(PdfException.DecodeParameterType1IsNotSupported).SetMessageParams(decodeParms.GetType
                                ().ToString());
                        }
                    }
                }
                pdfStream.Put(PdfName.Filter, filters);
            }
        }

        protected internal virtual byte[] DecodeFlateBytes(PdfStream stream, byte[] bytes) {
            var filterObject = stream.Get(PdfName.Filter);
            if (filterObject == null) {
                return bytes;
            }
            // check if flateDecode filter is on top
            PdfName filterName;
            PdfArray filtersArray = null;
            if (filterObject is PdfName) {
                filterName = (PdfName)filterObject;
            }
            else {
                if (filterObject is PdfArray) {
                    filtersArray = (PdfArray)filterObject;
                    filterName = filtersArray.GetAsName(0);
                }
                else {
                    throw new PdfException(PdfException.FilterIsNotANameOrArray);
                }
            }
            if (!PdfName.FlateDecode.Equals(filterName)) {
                return bytes;
            }
            // get decode params if present
            PdfDictionary decodeParams;
            PdfArray decodeParamsArray = null;
            var decodeParamsObject = stream.Get(PdfName.DecodeParms);
            if (decodeParamsObject == null) {
                decodeParams = null;
            }
            else {
                if (decodeParamsObject.GetObjectType() == PdfObject.DICTIONARY) {
                    decodeParams = (PdfDictionary)decodeParamsObject;
                }
                else {
                    if (decodeParamsObject.GetObjectType() == PdfObject.ARRAY) {
                        decodeParamsArray = (PdfArray)decodeParamsObject;
                        decodeParams = decodeParamsArray.GetAsDictionary(0);
                    }
                    else {
                        throw new PdfException(PdfException.DecodeParameterType1IsNotSupported).SetMessageParams(decodeParamsObject
                            .GetType().ToString());
                    }
                }
            }
            // decode
            var res = FlateDecodeFilter.FlateDecode(bytes, true);
            if (res == null) {
                res = FlateDecodeFilter.FlateDecode(bytes, false);
            }
            bytes = FlateDecodeFilter.DecodePredictor(res, decodeParams);
            //remove filter and decode params
            filterObject = null;
            if (filtersArray != null) {
                filtersArray.Remove(0);
                if (filtersArray.Size() == 1) {
                    filterObject = filtersArray.Get(0);
                }
                else {
                    if (!filtersArray.IsEmpty()) {
                        filterObject = filtersArray;
                    }
                }
            }
            decodeParamsObject = null;
            if (decodeParamsArray != null) {
                decodeParamsArray.Remove(0);
                if (decodeParamsArray.Size() == 1 && decodeParamsArray.Get(0).GetObjectType() != PdfObject.NULL) {
                    decodeParamsObject = decodeParamsArray.Get(0);
                }
                else {
                    if (!decodeParamsArray.IsEmpty()) {
                        decodeParamsObject = decodeParamsArray;
                    }
                }
            }
            if (filterObject == null) {
                stream.Remove(PdfName.Filter);
            }
            else {
                stream.Put(PdfName.Filter, filterObject);
            }
            if (decodeParamsObject == null) {
                stream.Remove(PdfName.DecodeParms);
            }
            else {
                stream.Put(PdfName.DecodeParms, decodeParamsObject);
            }
            return bytes;
        }
    }
}
