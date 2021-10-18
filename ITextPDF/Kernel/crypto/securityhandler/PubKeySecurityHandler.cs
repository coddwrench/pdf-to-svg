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
using System.Security.Cryptography.X509Certificates;
using IText.IO;
using IText.IO.Util;
using IText.Kernel.Pdf;

namespace IText.Kernel.Crypto.Securityhandler
{
	/// <author>Aiken Sam (aikensam@ieee.org)</author>
	public abstract class PubKeySecurityHandler : SecurityHandler
	{
		private const int SEED_LENGTH = 20;

		private IList<PublicKeyRecipient> recipients;

		private byte[] seed;

		protected internal PubKeySecurityHandler()
		{
			seed = EncryptionUtils.GenerateSeed(SEED_LENGTH);
			recipients = new List<PublicKeyRecipient>();
		}

		protected internal virtual byte[] ComputeGlobalKey(string messageDigestAlgorithm, bool encryptMetadata)
		{
			throw new NotImplementedException();
			//IDigest md;
			//byte[] encodedRecipient;
			//try
			//{
			//	md = DigestUtilities.GetDigest(messageDigestAlgorithm);
			//	md.Update(GetSeed());
			//	for (var i = 0; i < GetRecipientsSize(); i++)
			//	{
			//		encodedRecipient = GetEncodedRecipient(i);
			//		md.Update(encodedRecipient);
			//	}
			//	if (!encryptMetadata)
			//	{
			//		md.Update(new[] { (byte)255, (byte)255, (byte)255, (byte)255 });
			//	}
			//}
			//catch (Exception e)
			//{
			//	throw new PdfException(PdfException.PdfEncryption, e);
			//}
			//return md.Digest();
		}

		protected internal static byte[] ComputeGlobalKeyOnReading(PdfDictionary encryptionDictionary, object
			 certificateKey, X509Certificate certificate, bool encryptMetadata, string digestAlgorithm)
		{

			throw new NotImplementedException();
			//var recipients = encryptionDictionary.GetAsArray(PdfName.Recipients);
			//if (recipients == null)
			//{
			//	recipients = encryptionDictionary.GetAsDictionary(PdfName.CF).GetAsDictionary(PdfName.DefaultCryptFilter).
			//		GetAsArray(PdfName.Recipients);
			//}
			//var envelopedData = EncryptionUtils.FetchEnvelopedData(certificateKey, certificate, recipients);
			//byte[] encryptionKey;
			//// IDigest md;
			//try
			//{
			//	md = DigestUtilities.GetDigest(digestAlgorithm);
			//	md.Update(envelopedData, 0, 20);
			//	for (var i = 0; i < recipients.Size(); i++)
			//	{
			//		var encodedRecipient = recipients.GetAsString(i).GetValueBytes();
			//		md.Update(encodedRecipient);
			//	}
			//	if (!encryptMetadata)
			//	{
			//		md.Update(new[] { (byte)255, (byte)255, (byte)255, (byte)255 });
			//	}
			//	encryptionKey = md.Digest();
			//}
			//catch (Exception f)
			//{
			//	throw new PdfException(PdfException.PdfDecryption, f);
			//}
			//return encryptionKey;
		}

		protected internal virtual void AddAllRecipients(X509Certificate[] certs, int[] permissions)
		{
			if (certs != null)
			{
				for (var i = 0; i < certs.Length; i++)
				{
					AddRecipient(certs[i], permissions[i]);
				}
			}
		}

		protected internal virtual PdfArray CreateRecipientsArray()
		{
			PdfArray recipients;
			try
			{
				recipients = GetEncodedRecipients();
			}
			catch (Exception e)
			{
				throw new PdfException(PdfException.PdfEncryption, e);
			}
			return recipients;
		}

		protected internal abstract void SetPubSecSpecificHandlerDicEntries(PdfDictionary encryptionDictionary, bool
			 encryptMetadata, bool embeddedFilesOnly);

		protected internal abstract string GetDigestAlgorithm();

		protected internal abstract void InitKey(byte[] globalKey, int keyLength);

		protected internal virtual void InitKeyAndFillDictionary(PdfDictionary encryptionDictionary, X509Certificate
			[] certs, int[] permissions, bool encryptMetadata, bool embeddedFilesOnly)
		{
			AddAllRecipients(certs, permissions);
			var keyLen = encryptionDictionary.GetAsInt(PdfName.Length);
			var keyLength = keyLen != null ? (int)keyLen : 40;
			var digestAlgorithm = GetDigestAlgorithm();
			var digest = ComputeGlobalKey(digestAlgorithm, encryptMetadata);
			InitKey(digest, keyLength);
			SetPubSecSpecificHandlerDicEntries(encryptionDictionary, encryptMetadata, embeddedFilesOnly);
		}

		protected internal virtual void InitKeyAndReadDictionary(PdfDictionary encryptionDictionary, object
			 certificateKey, X509Certificate certificate, bool encryptMetadata)
		{
			throw new NotImplementedException();

			//var digestAlgorithm = GetDigestAlgorithm();
			//var encryptionKey = ComputeGlobalKeyOnReading(encryptionDictionary, certificateKey,
			//	certificate, encryptMetadata, digestAlgorithm);
			//var keyLen = encryptionDictionary.GetAsInt(PdfName.Length);
			//var keyLength = keyLen != null ? (int)keyLen : 40;
			//InitKey(encryptionKey, keyLength);
		}

		private void AddRecipient(X509Certificate cert, int permission)
		{
			recipients.Add(new PublicKeyRecipient(cert, permission));
		}

		private byte[] GetSeed()
		{
			var clonedSeed = new byte[seed.Length];
			Array.Copy(seed, 0, clonedSeed, 0, seed.Length);
			return clonedSeed;
		}

		private int GetRecipientsSize()
		{
			return recipients.Count;
		}

		private byte[] GetEncodedRecipient(int index)
		{
			throw new NotImplementedException();

			////Certificate certificate = recipient.getX509();
			//var recipient = recipients[index];
			//var cms = recipient.GetCms();
			//if (cms != null)
			//{
			//	return cms;
			//}
			//var certificate = recipient.GetCertificate();
			////constants permissions: PdfWriter.AllowCopy | PdfWriter.AllowPrinting | PdfWriter.AllowScreenReaders | PdfWriter.AllowAssembly;
			//var permission = recipient.GetPermission();
			//// NOTE! Added while porting to itext7
			//// Previous strange code was:
			//// int revision = 3;
			//// permission |= revision == 3 ? 0xfffff0c0 : 0xffffffc0;
			//// revision value never changed, so code have been replaced to this:
			//permission |= unchecked((int)(0xfffff0c0));
			//permission &= unchecked((int)(0xfffffffc));
			//permission += 1;
			//var pkcs7input = new byte[24];
			//var one = (byte)permission;
			//var two = (byte)(permission >> 8);
			//var three = (byte)(permission >> 16);
			//var four = (byte)(permission >> 24);
			//// put this seed in the pkcs7 input
			//Array.Copy(seed, 0, pkcs7input, 0, 20);
			//pkcs7input[20] = four;
			//pkcs7input[21] = three;
			//pkcs7input[22] = two;
			//pkcs7input[23] = one;
			//var baos = new MemoryStream();
			//var k = new Asn1OutputStream(baos);
			//var obj = CreateDERForRecipient(pkcs7input, certificate);
			//k.WriteObject(obj);
			//cms = baos.ToArray();
			//recipient.SetCms(cms);
			//return cms;
		}

		private PdfArray GetEncodedRecipients()
		{
			var EncodedRecipients = new PdfArray();
			byte[] cms;
			for (var i = 0; i < recipients.Count; i++)
			{
				try
				{
					cms = GetEncodedRecipient(i);
					EncodedRecipients.Add(new PdfLiteral(StreamUtil.CreateEscapedString(cms)));
				}
				//catch (GeneralSecurityException) {
				//    EncodedRecipients = null;
				//    // break was added while porting to itext7
				//    break;
				//}
				catch (IOException)
				{
					EncodedRecipients = null;
					// break was added while porting to itext7
					break;
				}
			}
			return EncodedRecipients;
		}

		//Asn1Object
		private object CreateDERForRecipient(byte[] @in, X509Certificate cert)
		{
			//var parameters = EncryptionUtils.CalculateDERForRecipientParams(@in);
			//var keytransrecipientinfo = ComputeRecipientInfo(cert, parameters.abyte0);
			//var deroctetstring = new DerOctetString(parameters.abyte1);
			//var derset = new DerSet(new RecipientInfo(keytransrecipientinfo));
			//var encryptedcontentinfo = new EncryptedContentInfo(PkcsObjectIdentifiers.Data
			//    , parameters.algorithmIdentifier, deroctetstring);
			//var env = new EnvelopedData(null, derset, encryptedcontentinfo, (Asn1Set)null);
			//var contentinfo = new ContentInfo(PkcsObjectIdentifiers.EnvelopedData, 
			//    env);
			//return contentinfo.ToAsn1Object();
			throw new NotImplementedException();
		}

		//private KeyTransRecipientInfo ComputeRecipientInfo(X509Certificate x509certificate, byte[] abyte0) {
		private object ComputeRecipientInfo(X509Certificate x509certificate, byte[] abyte0)
		{
			throw new NotImplementedException();

			//var asn1inputstream = new Asn1InputStream(new MemoryStream(x509certificate.GetTbsCertificate()
			//    ));
			//var tbscertificatestructure = TbsCertificateStructure.GetInstance(asn1inputstream.ReadObject
			//    ());
			//Debug.Assert(tbscertificatestructure != null);
			//var algorithmidentifier = tbscertificatestructure.SubjectPublicKeyInfo.AlgorithmID;
			//var issuerandserialnumber = new IssuerAndSerialNumber(tbscertificatestructure.Issuer, tbscertificatestructure
			//    .SerialNumber.Value);
			//var cipheredBytes = EncryptionUtils.CipherBytes(x509certificate, abyte0, algorithmidentifier);
			//var deroctetstring = new DerOctetString(cipheredBytes);
			//var recipId = new RecipientIdentifier(issuerandserialnumber);
			//return new KeyTransRecipientInfo(recipId, algorithmidentifier, deroctetstring);
		}
	}
}
