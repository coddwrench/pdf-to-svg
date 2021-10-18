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
using IText.Kernel.Pdf;

namespace IText.Kernel.Crypto.Securityhandler {
    public class StandardHandlerUsingStandard40 : StandardSecurityHandler {
        protected internal static readonly byte[] pad = { 0x28, 0xBF, 0x4E, 0x5E
            , 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08, 0x2E, 0x2E, 0x00, 0xB6, 0xD0, 
            0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A };

        protected internal static readonly byte[] metadataPad = { 255, 255, 255, 255 };

        protected internal byte[] documentId;

        // stores key length of the main key
        protected internal int keyLength;

        protected internal ARCFOUREncryption arcfour = new ARCFOUREncryption();

        public StandardHandlerUsingStandard40(PdfDictionary encryptionDictionary, byte[] userPassword, byte[] ownerPassword
            , int permissions, bool encryptMetadata, bool embeddedFilesOnly, byte[] documentId) {
            InitKeyAndFillDictionary(encryptionDictionary, userPassword, ownerPassword, permissions, encryptMetadata, 
                embeddedFilesOnly, documentId);
        }

        public StandardHandlerUsingStandard40(PdfDictionary encryptionDictionary, byte[] password, byte[] documentId
            , bool encryptMetadata) {
            InitKeyAndReadDictionary(encryptionDictionary, password, documentId, encryptMetadata);
        }

        public override OutputStreamEncryption GetEncryptionStream(Stream os) {
            return new OutputStreamStandardEncryption(os, nextObjectKey, 0, nextObjectKeySize);
        }

        public override IDecryptor GetDecryptor() {
            return new StandardDecryptor(nextObjectKey, 0, nextObjectKeySize);
        }

        public virtual byte[] ComputeUserPassword(byte[] ownerPassword, PdfDictionary encryptionDictionary) {
            var ownerKey = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.O));
            var userPad = ComputeOwnerKey(ownerKey, PadPassword(ownerPassword));
            for (var i = 0; i < userPad.Length; i++) {
                var match = true;
                for (var j = 0; j < userPad.Length - i; j++) {
                    if (userPad[i + j] != pad[j]) {
                        match = false;
                        break;
                    }
                }
                if (!match) {
                    continue;
                }
                var userPassword = new byte[i];
                Array.Copy(userPad, 0, userPassword, 0, i);
                return userPassword;
            }
            return userPad;
        }

        protected internal virtual void CalculatePermissions(int permissions) {
            permissions |= PERMS_MASK_1_FOR_REVISION_2;
            permissions &= PERMS_MASK_2;
            this.permissions = permissions;
        }

        protected internal virtual byte[] ComputeOwnerKey(byte[] userPad, byte[] ownerPad) {
			throw new NotImplementedException();
            //var ownerKey = new byte[32];
            //var digest = md5.Digest(ownerPad);
            //arcfour.PrepareARCFOURKey(digest, 0, 5);
            //arcfour.EncryptARCFOUR(userPad, ownerKey);
            //return ownerKey;
        }

        protected internal virtual void ComputeGlobalEncryptionKey(byte[] userPad, byte[] ownerKey, bool encryptMetadata
            ) {
            mkey = new byte[keyLength / 8];
            throw new NotImplementedException();

			// fixed by ujihara in order to follow PDF reference
			//md5.Reset();
			//md5.Update(userPad);
			//md5.Update(ownerKey);
			//var ext = new byte[4];
			//ext[0] = (byte)permissions;
			//ext[1] = (byte)(permissions >> 8);
			//ext[2] = (byte)(permissions >> 16);
			//ext[3] = (byte)(permissions >> 24);
			//md5.Update(ext, 0, 4);
			//if (documentId != null) {
			//    md5.Update(documentId);
			//}
			//if (!encryptMetadata) {
			//    md5.Update(metadataPad);
			//}
			//var digest = new byte[mkey.Length];
			//Array.Copy(md5.Digest(), 0, digest, 0, mkey.Length);
			//Array.Copy(digest, 0, mkey, 0, mkey.Length);
		}

		protected internal virtual byte[] ComputeUserKey() {
            var userKey = new byte[32];
            arcfour.PrepareARCFOURKey(mkey);
            arcfour.EncryptARCFOUR(pad, userKey);
            return userKey;
        }

        protected internal virtual void SetSpecificHandlerDicEntries(PdfDictionary encryptionDictionary, bool encryptMetadata
            , bool embeddedFilesOnly) {
            encryptionDictionary.Put(PdfName.R, new PdfNumber(2));
            encryptionDictionary.Put(PdfName.V, new PdfNumber(1));
        }

        protected internal virtual bool IsValidPassword(byte[] uValue, byte[] userKey) {
            return !EqualsArray(uValue, userKey, 32);
        }

        private void InitKeyAndFillDictionary(PdfDictionary encryptionDictionary, byte[] userPassword, byte[] ownerPassword
            , int permissions, bool encryptMetadata, bool embeddedFilesOnly, byte[] documentId) {
            ownerPassword = GenerateOwnerPasswordIfNullOrEmpty(ownerPassword);
            CalculatePermissions(permissions);
            this.documentId = documentId;
            keyLength = GetKeyLength(encryptionDictionary);
            // PDF reference 3.5.2 Standard Security Handler, Algorithm 3.3-1
            // If there is no owner password, use the user password instead.
            var userPad = PadPassword(userPassword);
            var ownerPad = PadPassword(ownerPassword);
            var ownerKey = ComputeOwnerKey(userPad, ownerPad);
            ComputeGlobalEncryptionKey(userPad, ownerKey, encryptMetadata);
            var userKey = ComputeUserKey();
            SetStandardHandlerDicEntries(encryptionDictionary, userKey, ownerKey);
            SetSpecificHandlerDicEntries(encryptionDictionary, encryptMetadata, embeddedFilesOnly);
        }

        private void InitKeyAndReadDictionary(PdfDictionary encryptionDictionary, byte[] password, byte[] documentId
            , bool encryptMetadata) {
            var uValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.U));
            var oValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.O));
            var pValue = (PdfNumber)encryptionDictionary.Get(PdfName.P);
            permissions = pValue.LongValue();
            this.documentId = documentId;
            keyLength = GetKeyLength(encryptionDictionary);
            var paddedPassword = PadPassword(password);
            CheckPassword(encryptMetadata, uValue, oValue, paddedPassword);
        }

        private void CheckPassword(bool encryptMetadata, byte[] uValue, byte[] oValue, byte[] paddedPassword) {
            // assume password - is owner password
            byte[] userKey;
            var userPad = ComputeOwnerKey(oValue, paddedPassword);
            ComputeGlobalEncryptionKey(userPad, oValue, encryptMetadata);
            userKey = ComputeUserKey();
            // computed user key should be equal to uValue
            if (IsValidPassword(uValue, userKey)) {
                // assume password - is user password
                ComputeGlobalEncryptionKey(paddedPassword, oValue, encryptMetadata);
                userKey = ComputeUserKey();
                // computed user key should be equal to uValue
                if (IsValidPassword(uValue, userKey)) {
                    throw new BadPasswordException(PdfException.BadUserPassword);
                }
                usedOwnerPassword = false;
            }
        }

        private byte[] PadPassword(byte[] password) {
            var userPad = new byte[32];
            if (password == null) {
                Array.Copy(pad, 0, userPad, 0, 32);
            }
            else {
                Array.Copy(password, 0, userPad, 0, Math.Min(password.Length, 32));
                if (password.Length < 32) {
                    Array.Copy(pad, 0, userPad, password.Length, 32 - password.Length);
                }
            }
            return userPad;
        }

        private int GetKeyLength(PdfDictionary encryptionDict) {
            var keyLength = encryptionDict.GetAsInt(PdfName.Length);
            return keyLength != null ? (int)keyLength : 40;
        }
    }
}
