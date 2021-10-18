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
using IText.IO;
using IText.IO.Util;
using IText.Kernel.Pdf;
using IText.Logger;

namespace IText.Kernel.Crypto.Securityhandler
{
	public class StandardHandlerUsingAes256 : StandardSecurityHandler
	{
		private const int VALIDATION_SALT_OFFSET = 32;

		private const int KEY_SALT_OFFSET = 40;

		private const int SALT_LENGTH = 8;

		private bool isPdf2;

		protected internal bool encryptMetadata;

		public StandardHandlerUsingAes256(PdfDictionary encryptionDictionary, byte[] userPassword, byte[] ownerPassword
			, int permissions, bool encryptMetadata, bool embeddedFilesOnly, PdfVersion version)
		{
			isPdf2 = version != null && version.CompareTo(PdfVersion.PDF_2_0) >= 0;
			InitKeyAndFillDictionary(encryptionDictionary, userPassword, ownerPassword, permissions, encryptMetadata,
				embeddedFilesOnly);
		}

		public StandardHandlerUsingAes256(PdfDictionary encryptionDictionary, byte[] password)
		{
			InitKeyAndReadDictionary(encryptionDictionary, password);
		}

		public virtual bool IsEncryptMetadata()
		{
			return encryptMetadata;
		}

		public override void SetHashKeyForNextObject(int objNumber, int objGeneration)
		{
		}

		// in AES256 we don't recalculate nextObjectKey
		public override OutputStreamEncryption GetEncryptionStream(Stream os)
		{
			return new OutputStreamAesEncryption(os, nextObjectKey, 0, nextObjectKeySize);
		}

		public override IDecryptor GetDecryptor()
		{
			return new AesDecryptor(nextObjectKey, 0, nextObjectKeySize);
		}

		private void InitKeyAndFillDictionary(PdfDictionary encryptionDictionary, byte[] userPassword, byte[] ownerPassword
			, int permissions, bool encryptMetadata, bool embeddedFilesOnly)
		{
			ownerPassword = GenerateOwnerPasswordIfNullOrEmpty(ownerPassword);
			permissions |= PERMS_MASK_1_FOR_REVISION_3_OR_GREATER;
			permissions &= PERMS_MASK_2;
			try
			{
				byte[] userKey;
				byte[] ownerKey;
				byte[] ueKey;
				byte[] oeKey;
				byte[] aes256Perms;
				if (userPassword == null)
				{
					userPassword = new byte[0];
				}
				else
				{
					if (userPassword.Length > 127)
					{
						userPassword = JavaUtil.ArraysCopyOf(userPassword, 127);
					}
				}
				if (ownerPassword.Length > 127)
				{
					ownerPassword = JavaUtil.ArraysCopyOf(ownerPassword, 127);
				}
				// first 8 bytes are validation salt; second 8 bytes are key salt
				var userValAndKeySalt = IVGenerator.GetIV(16);
				var ownerValAndKeySalt = IVGenerator.GetIV(16);
				nextObjectKey = IVGenerator.GetIV(32);
				nextObjectKeySize = 32;
				byte[] hash;
				// Algorithm 8.1
				hash = ComputeHash(userPassword, userValAndKeySalt, 0, 8);
				userKey = JavaUtil.ArraysCopyOf(hash, 48);
				Array.Copy(userValAndKeySalt, 0, userKey, 32, 16);
				// Algorithm 8.2
				hash = ComputeHash(userPassword, userValAndKeySalt, 8, 8);
				var ac = new AESCipherCBCnoPad(true, hash);
				ueKey = ac.ProcessBlock(nextObjectKey, 0, nextObjectKey.Length);
				// Algorithm 9.1
				hash = ComputeHash(ownerPassword, ownerValAndKeySalt, 0, 8, userKey);
				ownerKey = JavaUtil.ArraysCopyOf(hash, 48);
				Array.Copy(ownerValAndKeySalt, 0, ownerKey, 32, 16);
				// Algorithm 9.2
				hash = ComputeHash(ownerPassword, ownerValAndKeySalt, 8, 8, userKey);
				ac = new AESCipherCBCnoPad(true, hash);
				oeKey = ac.ProcessBlock(nextObjectKey, 0, nextObjectKey.Length);
				// Algorithm 10
				var permsp = IVGenerator.GetIV(16);
				permsp[0] = (byte)permissions;
				permsp[1] = (byte)(permissions >> 8);
				permsp[2] = (byte)(permissions >> 16);
				permsp[3] = (byte)(permissions >> 24);
				permsp[4] = 255;
				permsp[5] = 255;
				permsp[6] = 255;
				permsp[7] = 255;
				permsp[8] = encryptMetadata ? (byte)'T' : (byte)'F';
				permsp[9] = (byte)'a';
				permsp[10] = (byte)'d';
				permsp[11] = (byte)'b';
				ac = new AESCipherCBCnoPad(true, nextObjectKey);
				aes256Perms = ac.ProcessBlock(permsp, 0, permsp.Length);
				this.permissions = permissions;
				this.encryptMetadata = encryptMetadata;
				SetStandardHandlerDicEntries(encryptionDictionary, userKey, ownerKey);
				SetAES256DicEntries(encryptionDictionary, oeKey, ueKey, aes256Perms, encryptMetadata, embeddedFilesOnly);
			}
			catch (Exception ex)
			{
				throw new PdfException(PdfException.PdfEncryption, ex);
			}
		}

		private void SetAES256DicEntries(PdfDictionary encryptionDictionary, byte[] oeKey, byte[] ueKey, byte[] aes256Perms
			, bool encryptMetadata, bool embeddedFilesOnly)
		{
			var vAes256 = 5;
			var rAes256 = 5;
			var rAes256Pdf2 = 6;
			encryptionDictionary.Put(PdfName.OE, new PdfLiteral(StreamUtil.CreateEscapedString(oeKey)));
			encryptionDictionary.Put(PdfName.UE, new PdfLiteral(StreamUtil.CreateEscapedString(ueKey)));
			encryptionDictionary.Put(PdfName.Perms, new PdfLiteral(StreamUtil.CreateEscapedString(aes256Perms)));
			encryptionDictionary.Put(PdfName.R, new PdfNumber(isPdf2 ? rAes256Pdf2 : rAes256));
			encryptionDictionary.Put(PdfName.V, new PdfNumber(vAes256));
			var stdcf = new PdfDictionary();
			stdcf.Put(PdfName.Length, new PdfNumber(32));
			if (!encryptMetadata)
			{
				encryptionDictionary.Put(PdfName.EncryptMetadata, PdfBoolean.FALSE);
			}
			if (embeddedFilesOnly)
			{
				stdcf.Put(PdfName.AuthEvent, PdfName.EFOpen);
				encryptionDictionary.Put(PdfName.EFF, PdfName.StdCF);
				encryptionDictionary.Put(PdfName.StrF, PdfName.Identity);
				encryptionDictionary.Put(PdfName.StmF, PdfName.Identity);
			}
			else
			{
				stdcf.Put(PdfName.AuthEvent, PdfName.DocOpen);
				encryptionDictionary.Put(PdfName.StrF, PdfName.StdCF);
				encryptionDictionary.Put(PdfName.StmF, PdfName.StdCF);
			}
			stdcf.Put(PdfName.CFM, PdfName.AESV3);
			var cf = new PdfDictionary();
			cf.Put(PdfName.StdCF, stdcf);
			encryptionDictionary.Put(PdfName.CF, cf);
		}

		private void InitKeyAndReadDictionary(PdfDictionary encryptionDictionary, byte[] password)
		{
			try
			{
				if (password == null)
				{
					password = new byte[0];
				}
				else
				{
					if (password.Length > 127)
					{
						password = JavaUtil.ArraysCopyOf(password, 127);
					}
				}
				isPdf2 = encryptionDictionary.GetAsNumber(PdfName.R).GetValue() == 6;
				var oValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.O));
				var uValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.U));
				var oeValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.OE));
				var ueValue = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.UE));
				var perms = GetIsoBytes(encryptionDictionary.GetAsString(PdfName.Perms));
				var pValue = (PdfNumber)encryptionDictionary.Get(PdfName.P);
				permissions = pValue.LongValue();
				byte[] hash;
				hash = ComputeHash(password, oValue, VALIDATION_SALT_OFFSET, SALT_LENGTH, uValue);
				usedOwnerPassword = CompareArray(hash, oValue, 32);
				if (usedOwnerPassword)
				{
					hash = ComputeHash(password, oValue, KEY_SALT_OFFSET, SALT_LENGTH, uValue);
					var ac = new AESCipherCBCnoPad(false, hash);
					nextObjectKey = ac.ProcessBlock(oeValue, 0, oeValue.Length);
				}
				else
				{
					hash = ComputeHash(password, uValue, VALIDATION_SALT_OFFSET, SALT_LENGTH);
					if (!CompareArray(hash, uValue, 32))
					{
						throw new BadPasswordException(PdfException.BadUserPassword);
					}
					hash = ComputeHash(password, uValue, KEY_SALT_OFFSET, SALT_LENGTH);
					var ac = new AESCipherCBCnoPad(false, hash);
					nextObjectKey = ac.ProcessBlock(ueValue, 0, ueValue.Length);
				}
				nextObjectKeySize = 32;
				var ac_1 = new AESCipherCBCnoPad(false, nextObjectKey);
				var decPerms = ac_1.ProcessBlock(perms, 0, perms.Length);
				if (decPerms[9] != (byte)'a' || decPerms[10] != (byte)'d' || decPerms[11] != (byte)'b')
				{
					throw new BadPasswordException(PdfException.BadUserPassword);
				}
				var permissionsDecoded = (decPerms[0] & 0xff) | ((decPerms[1] & 0xff) << 8) | ((decPerms[2] & 0xff) << 16)
										 | ((decPerms[3] & 0xff) << 24);
				var encryptMetadata = decPerms[8] == (byte)'T';
				var encryptMetadataEntry = encryptionDictionary.GetAsBool(PdfName.EncryptMetadata);
				if (permissionsDecoded != permissions || encryptMetadataEntry != null && encryptMetadata != encryptMetadataEntry
					)
				{
					var logger = LogManager.GetLogger(typeof(StandardHandlerUsingAes256));
					logger.Error(LogMessageConstant.ENCRYPTION_ENTRIES_P_AND_ENCRYPT_METADATA_NOT_CORRESPOND_PERMS_ENTRY
						);
				}
				permissions = permissionsDecoded;
				this.encryptMetadata = encryptMetadata;
			}
			catch (BadPasswordException ex)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new PdfException(PdfException.PdfEncryption, ex);
			}
		}

		private byte[] ComputeHash(byte[] password, byte[] salt, int saltOffset, int saltLen)
		{
			return ComputeHash(password, salt, saltOffset, saltLen, null);
		}

		private byte[] ComputeHash(byte[] password, byte[] salt, int saltOffset, int saltLen, byte[] userKey)
		{
			throw new NotImplementedException();

			/*var mdSha256 = DigestUtilities.GetDigest("SHA-256");
            mdSha256.Update(password);
            mdSha256.Update(salt, saltOffset, saltLen);
            if (userKey != null) {
                mdSha256.Update(userKey);
            }
            var k = mdSha256.Digest();
            if (isPdf2) {
                // See 7.6.4.3.3 "Algorithm 2.B"
                var mdSha384 = DigestUtilities.GetDigest("SHA-384");
                var mdSha512 = DigestUtilities.GetDigest("SHA-512");
                var userKeyLen = userKey != null ? userKey.Length : 0;
                var passAndUserKeyLen = password.Length + userKeyLen;
                // k1 repetition length
                int k1RepLen;
                var roundNum = 0;
                while (true) {
                    // a)
                    k1RepLen = passAndUserKeyLen + k.Length;
                    var k1 = new byte[k1RepLen * 64];
                    Array.Copy(password, 0, k1, 0, password.Length);
                    Array.Copy(k, 0, k1, password.Length, k.Length);
                    if (userKey != null) {
                        Array.Copy(userKey, 0, k1, password.Length + k.Length, userKeyLen);
                    }
                    for (var i = 1; i < 64; ++i) {
                        Array.Copy(k1, 0, k1, k1RepLen * i, k1RepLen);
                    }
                    // b)
                    var cipher = new AESCipherCBCnoPad(true, JavaUtil.ArraysCopyOf(k, 16), JavaUtil.ArraysCopyOfRange
                        (k, 16, 32));
                    var e = cipher.ProcessBlock(k1, 0, k1.Length);
                    // c)
                    IDigest md = null;
                    var i_1 = new BigInteger(1, JavaUtil.ArraysCopyOf(e, 16));
                    var remainder = i_1.Remainder(BigInteger.ValueOf(3)).IntValue;
                    switch (remainder) {
                        case 0: {
                            md = mdSha256;
                            break;
                        }

                        case 1: {
                            md = mdSha384;
                            break;
                        }

                        case 2: {
                            md = mdSha512;
                            break;
                        }
                    }
                    // d)
                    k = md.Digest(e);
                    ++roundNum;
                    if (roundNum > 63) {
                        // e)
                        // interpreting last byte as unsigned integer
                        var condVal = e[e.Length - 1] & 0xFF;
                        if (condVal <= roundNum - 32) {
                            break;
                        }
                    }
                }
                k = k.Length == 32 ? k : JavaUtil.ArraysCopyOf(k, 32);
            }
            return k;*/
		}

		private static bool CompareArray(byte[] a, byte[] b, int len)
		{
			for (var k = 0; k < len; ++k)
			{
				if (a[k] != b[k])
				{
					return false;
				}
			}
			return true;
		}
	}
}
