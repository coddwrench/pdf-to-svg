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
using System.Security.Cryptography;
using IText.Kernel.Pdf;

namespace IText.Kernel.Crypto.Securityhandler {
    public class StandardHandlerUsingStandard128 : StandardHandlerUsingStandard40 {
        public StandardHandlerUsingStandard128(PdfDictionary encryptionDictionary, byte[] userPassword, byte[] ownerPassword
            , int permissions, bool encryptMetadata, bool embeddedFilesOnly, byte[] documentId)
            : base(encryptionDictionary, userPassword, ownerPassword, permissions, encryptMetadata, embeddedFilesOnly, 
                documentId) {
        }

        public StandardHandlerUsingStandard128(PdfDictionary encryptionDictionary, byte[] password, byte[] documentId
            , bool encryptMetadata)
            : base(encryptionDictionary, password, documentId, encryptMetadata) {
        }

        protected internal override void CalculatePermissions(int permissions) {
            permissions |= PermsMask1ForRevision3OrGreater;
            permissions &= PermsMask2;
            this.Permissions = permissions;
        }

        protected internal override byte[] ComputeOwnerKey(byte[] userPad, byte[] ownerPad) {

            var ownerKey = new byte[32];
            using var md5 = MD5.Create();
            var digest = md5.ComputeHash(ownerPad);
            var mkey = new byte[KeyLength / 8];
            
            // only use for the input as many bit as the key consists of
            for (var k = 0; k < 50; ++k)
            {
                var newDigest = md5.ComputeHash(digest);
                Array.Copy(newDigest, 0, digest, 0, mkey.Length);
            }

            Array.Copy(userPad, 0, ownerKey, 0, 32);
            for (var i = 0; i < 20; ++i)
            {
                for (var j = 0; j < mkey.Length; ++j)
                {
                    mkey[j] = (byte)(digest[j] ^ i);
                }
                Arcfour.PrepareARCFOURKey(mkey);
                Arcfour.EncryptARCFOUR(ownerKey);
            }
            return ownerKey;
        }

        protected internal override void ComputeGlobalEncryptionKey(byte[] userPad, byte[] ownerKey,
            bool encryptMetadata
        )
        {
            using var md5 = MD5.Create();

            MasterKey = new byte[KeyLength / 8];

            var tempDigest = new byte[userPad.Length];
            md5.TransformBlock(userPad, 0, userPad.Length, tempDigest, 0);
            md5.TransformBlock(ownerKey, 0, ownerKey.Length, tempDigest, 0);

            var ext = new byte[4];
            ext[0] = (byte) Permissions;
            ext[1] = (byte) (Permissions >> 8);
            ext[2] = (byte) (Permissions >> 16);
            ext[3] = (byte) (Permissions >> 24);

            md5.TransformBlock(ext, 0, ext.Length, tempDigest, 0);

            if (DocumentId != null)
                md5.TransformBlock(DocumentId, 0, DocumentId.Length, tempDigest, 0);

            if (!encryptMetadata)
                md5.TransformBlock(MetadataPad, 0, MetadataPad.Length, tempDigest, 0);

            md5.TransformFinalBlock(new byte[] { }, 0, 0);

            var digest = new byte[MasterKey.Length];
            Array.Copy(md5.Hash, 0, digest, 0, MasterKey.Length);
            // only use the really needed bits as input for the hash
            for (var k = 0; k < 50; ++k)
            {
                var newDigest = md5.ComputeHash(digest);
                Array.Copy(newDigest, 0, digest, 0, MasterKey.Length);
            }

            Array.Copy(digest, 0, MasterKey, 0, MasterKey.Length);
        }

        protected internal override byte[] ComputeUserKey() {

            using var md5 = MD5.Create();

            var userKey = new byte[32];

            var tempDigest = new byte[Pad.Length];
            md5.TransformBlock(Pad, 0, Pad.Length, tempDigest, 0);
            md5.TransformFinalBlock(DocumentId, 0, DocumentId.Length);
            var digest = md5.Hash;
            
            Array.Copy(digest, 0, userKey, 0, 16);
            for (var k = 16; k < 32; ++k)
            {
                userKey[k] = 0;
            }
            for (var i = 0; i < 20; ++i)
            {
                for (var j = 0; j < MasterKey.Length; ++j)
                {
                    digest[j] = (byte)(MasterKey[j] ^ i);
                }
                Arcfour.PrepareARCFOURKey(digest, 0, MasterKey.Length);
                Arcfour.EncryptARCFOUR(userKey, 0, 16);
            }
            return userKey;
        }

		protected internal override void SetSpecificHandlerDicEntries(PdfDictionary encryptionDictionary, bool encryptMetadata
            , bool embeddedFilesOnly) {
            if (encryptMetadata) {
                encryptionDictionary.Put(PdfName.R, new PdfNumber(3));
                encryptionDictionary.Put(PdfName.V, new PdfNumber(2));
            }
            else {
                encryptionDictionary.Put(PdfName.EncryptMetadata, PdfBoolean.FALSE);
                encryptionDictionary.Put(PdfName.R, new PdfNumber(4));
                encryptionDictionary.Put(PdfName.V, new PdfNumber(4));
                var stdcf = new PdfDictionary();
                stdcf.Put(PdfName.Length, new PdfNumber(16));
                if (embeddedFilesOnly) {
                    stdcf.Put(PdfName.AuthEvent, PdfName.EFOpen);
                    encryptionDictionary.Put(PdfName.EFF, PdfName.StdCF);
                    encryptionDictionary.Put(PdfName.StrF, PdfName.Identity);
                    encryptionDictionary.Put(PdfName.StmF, PdfName.Identity);
                }
                else {
                    stdcf.Put(PdfName.AuthEvent, PdfName.DocOpen);
                    encryptionDictionary.Put(PdfName.StrF, PdfName.StdCF);
                    encryptionDictionary.Put(PdfName.StmF, PdfName.StdCF);
                }
                stdcf.Put(PdfName.CFM, PdfName.V2);
                var cf = new PdfDictionary();
                cf.Put(PdfName.StdCF, stdcf);
                encryptionDictionary.Put(PdfName.CF, cf);
            }
        }

        protected internal override bool IsValidPassword(byte[] uValue, byte[] userKey) {
            return !EqualsArray(uValue, userKey, 16);
        }
    }
}
