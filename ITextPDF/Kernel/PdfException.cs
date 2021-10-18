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

namespace IText.Kernel {
    /// <summary>Exception class for exceptions in kernel module.</summary>
    public class PdfException : Exception {
        public const string _1IsAnUnknownGraphicsStateDictionary = "{0} is an unknown graphics state dictionary.";

        public const string _1IsNotAnAcceptableValueForTheField2 = "{0} is not an acceptable value for the field {1}.";

        public const string _1IsNotAValidPlaceableWindowsMetafile = "{0} is not a valid placeable windows metafile.";

        public const string AnnotationShallHaveReferenceToPage = "Annotation shall have reference to page.";

        public const string AppendModeRequiresADocumentWithoutErrorsEvenIfRecoveryWasPossible = "Append mode requires a document without errors, even if recovery is possible.";

        public const string AuthenticatedAttributeIsMissingTheDigest = "Authenticated attribute is missing the digest.";

        public const string AvailableSpaceIsNotEnoughForSignature = "Available space is not enough for signature.";

        public const string BadCertificateAndKey = "Bad public key certificate and/or private key.";

        public const string BadUserPassword = "Bad user password. Password is not provided or wrong password provided. Correct password should be passed to PdfReader constructor with properties. See ReaderProperties#setPassword() method.";

        public const string CannotAddCellToCompletedLargeTable = "The large table was completed. It's prohibited to use it anymore. Created different Table instance instead.";

        public const string CannotAddKidToTheFlushedElement = "Cannot add kid to the flushed element.";

        [Obsolete]
        public const string CannotAddNonDictionaryExtGStateToResources1 = "Cannot add graphic state to resources. The PdfObject type is {0}, but should be PdfDictionary.";

        public const string CannotAddNonDictionaryPatternToResources1 = "Cannot add pattern to resources. The PdfObject type is {0}, but should be PdfDictionary or PdfStream.";

        public const string CannotAddNonDictionaryPropertiesToResources1 = "Cannot add properties to resources. The PdfObject type is {0}, but should be PdfDictionary.";

        public const string CannotAddNonDictionaryShadingToResources1 = "Cannot add shading to resources. The PdfObject type is {0}, but should be PdfDictionary or PdfStream.";

        public const string CannotAddNonStreamFormToResources1 = "Cannot add form to resources. The PdfObject type is {0}, but should be PdfStream.";

        public const string CannotAddNonStreamImageToResources1 = "Cannot add image to resources. The PdfObject type is {0}, but should be PdfStream.";

        public const string CannotBeEmbeddedDueToLicensingRestrictions = "{0} cannot be embedded due to licensing restrictions.";

        public const string CannotCloseDocument = "Cannot close document.";

        public const string CannotCloseDocumentWithAlreadyFlushedPdfCatalog = "Cannot close document with already flushed PDF Catalog.";

        public const string CannotConvertPdfArrayToBooleanArray = "Cannot convert PdfArray to an array of booleans";

        public const string CannotConvertPdfArrayToDoubleArray = "Cannot convert PdfArray to an array of doubles.";

        public const string CannotConvertPdfArrayToIntArray = "Cannot convert PdfArray to an array of integers.";

        public const string CannotConvertPdfArrayToFloatArray = "Cannot convert PdfArray to an array of floats.";

        public const string CannotConvertPdfArrayToLongArray = "Cannot convert PdfArray to an array of longs.";

        public const string CannotConvertPdfArrayToRectanle = "Cannot convert PdfArray to Rectangle.";

        public const string CannotCopyFlushedObject = "Cannot copy flushed object.";

        public const string CannotCopyFlushedTag = "Cannot copy flushed tag.";

        public const string CannotCopyObjectContent = "Cannot copy object content.";

        public const string CannotCopyIndirectObjectFromTheDocumentThatIsBeingWritten = "Cannot copy indirect object from the document that is being written.";

        public const string CannotCopyToDocumentOpenedInReadingMode = "Cannot copy to document opened in reading mode.";

        public const string CannotCreateFontFromNullFontDictionary = "Cannot create font from null pdf dictionary.";

        public const string CannotCreateLayoutImageByWmfImage = "Cannot create layout image by WmfImage instance. First convert the image into FormXObject and then use the corresponding layout image constructor.";

        public const string CannotCreatePdfImageXObjectByWmfImage = "Cannot create PdfImageXObject instance by WmfImage. Use PdfFormXObject constructor instead.";

        public const string CannotCreatePdfStreamByInputStreamWithoutPdfDocument = "Cannot create pdfstream by InputStream without PdfDocument.";

        public const string CannotCreateType0FontWithTrueTypeFontProgramWithoutEmbedding = "Cannot create Type0 font with true type font program without embedding it.";

        public const string CannotDrawElementsOnAlreadyFlushedPages = "Cannot draw elements on already flushed pages.";

        public const string CannotEmbedStandardFont = "Standard fonts cannot be embedded.";

        public const string CannotEmbedType0FontWithCidFontProgram = "Cannot embed Type0 font with CID font program based on non-generic predefined CMap.";

        public const string CannotGetContentBytes = "Cannot get content bytes.";

        public const string CannotGetPdfStreamBytes = "Cannot get PdfStream bytes.";

        public const string CannotOperateWithFlushedPdfStream = "Cannot operate with the flushed PdfStream.";

        public const string CannotRetrieveMediaBoxAttribute = "Invalid PDF. There is no media box attribute for page or its parents.";

        public const string CannotFindImageDataOrEI = "Cannot find image data or EI.";

        public const string CannotFlushDocumentRootTagBeforeDocumentIsClosed = "Cannot flush document root tag before document is closed.";

        public const string CannotFlushObject = "Cannot flush object.";

        public const string CannotMoveFlushedTag = "Cannot move flushed tag";

        public const string CannotMoveToFlushedKid = "Cannot move to flushed kid.";

        public const string CannotMoveToMarkedContentReference = "Cannot move to marked content reference.";

        public const string CannotMoveToParentCurrentElementIsRoot = "Cannot move to parent current element is root.";

        public const string CannotMovePagesInPartlyFlushedDocument = "Cannot move pages in partly flushed document. Page number {0} is already flushed.";

        public const string CannotOpenDocument = "Cannot open document.";

        public const string CannotParseContentStream = "Cannot parse content stream.";

        public const string CannotReadAStreamInOrderToAppendNewBytes = "Cannot read a stream in order to append new bytes.";

        public const string CannotReadPdfObject = "Cannot read PdfObject.";

        public const string CannotRecogniseDocumentFontWithEncoding = "Cannot recognise document font {0} with {1} encoding";

        public const string CannotRelocateRootTag = "Cannot relocate root tag.";

        public const string CannotRelocateTagWhichIsAlreadyFlushed = "Cannot relocate tag which is already flushed.";

        public const string CannotRelocateTagWhichParentIsAlreadyFlushed = "Cannot relocate tag which parent is already flushed.";

        public const string CannotRemoveDocumentRootTag = "Cannot remove document root tag.";

        public const string CannotRemoveMarkedContentReferenceBecauseItsPageWasAlreadyFlushed = "Cannot remove marked content reference, because its page has been already flushed.";

        public const string CannotRemoveTagBecauseItsParentIsFlushed = "Cannot remove tag, because its parent is flushed.";

        [Obsolete]
        public const string CannotSetDataToPdfstreamWhichWasCreatedByInputStream = "Cannot set data to PdfStream which was created by InputStream.";

        public const string CannotSetDataToPdfStreamWhichWasCreatedByInputStream = "Cannot set data to PdfStream which was created by InputStream.";

        public const string CannotSetEncryptedPayloadToDocumentOpenedInReadingMode = "Cannot set encrypted payload to a document opened in read only mode.";

        public const string CannotSetEncryptedPayloadToEncryptedDocument = "Cannot set encrypted payload to an encrypted document.";

        public const string CannotSplitDocumentThatIsBeingWritten = "Cannot split document that is being written.";

        public const string CannotWriteToPdfStream = "Cannot write to PdfStream.";

        public const string CannotWriteObjectAfterItWasReleased = "Cannot write object after it was released. In normal situation the object must be read once again before being written.";

        public const string CannotDecodePkcs7SigneddataObject = "Cannot decode PKCS#7 SignedData object.";

        public const string CannotFindSigningCertificateWithSerial1 = "Cannot find signing certificate with serial {0}.";

        public const string CertificateIsNotProvidedDocumentIsEncryptedWithPublicKeyCertificate = "Certificate is not provided. Document is encrypted with public key certificate, it should be passed to PdfReader constructor with properties. See ReaderProperties#setPublicKeySecurityParams() method.";

        public const string CertificationSignatureCreationFailedDocShallNotContainSigs = "Certification signature creation failed. Document shall not contain any certification or approval signatures before signing with certification signature.";

        public const string CfNotFoundEncryption = "/CF not found (encryption)";

        public const string CodabarMustHaveAtLeastStartAndStopCharacter = "Codabar must have at least start and stop character.";

        public const string CodabarMustHaveOneAbcdAsStartStopCharacter = "Codabar must have one of 'ABCD' as start/stop character.";

        public const string ColorSpaceNotFound = "ColorSpace not found.";

        public const string ContentStreamMustNotInvokeOperatorsThatSpecifyColorsOrOtherColorRelatedParameters = "Content stream must not invoke operators that specify colors or other color related parameters in the graphics state.";

        public const string DataHandlerCounterHasBeenDisabled = "Data handler counter has been disabled";

        public const string DecodeParameterType1IsNotSupported = "Decode parameter type {0} is not supported.";

        public const string DefaultAppearanceNotFound = "DefaultAppearance is required but not found";

        public const string DefaultcryptfilterNotFoundEncryption = "/DefaultCryptFilter not found (encryption).";

        public const string DictionaryKey1IsNotAName = "Dictionary key {0} is not a name.";

        /// <summary>Exception message in case of dictionary does not have specified font data.</summary>
        [Obsolete(@"Will be removed in nex major release as there are no usages left.")]
        public const string DictionaryDoesntHave1FontData = "Dictionary doesn't have {0} font data.";

        public const string DictionaryDoesntHaveSupportedFontData = "Dictionary doesn't have supported font data.";

        public const string DocumentAlreadyPreClosed = "Document has been already pre closed.";

        public const string DocumentClosedItIsImpossibleToExecuteAction = "Document was closed. It is impossible to execute action.";

        public const string DocumentDoesntContainStructTreeRoot = "Document doesn't contain StructTreeRoot.";

        public const string DocumentHasNoPages = "Document has no pages.";

        public const string DocumentHasNoPdfCatalogObject = "Document has no PDF Catalog object.";

        public const string DocumentHasNotBeenReadYet = "The PDF document has not been read yet. Document reading occurs in PdfDocument class constructor";

        public const string DocumentMustBePreClosed = "Document must be preClosed.";

        public const string DocumentForCopyToCannotBeNull = "Document for copyTo cannot be null.";

        public const string DuringDecompressionMultipleStreamsInSumOccupiedMoreMemoryThanAllowed = "During decompression multiple streams in sum occupied more memory than allowed. Please either check your pdf or increase the allowed single decompressed pdf stream maximum size value by setting the appropriate parameter of ReaderProperties's MemoryLimitsAwareHandler.";

        public const string DuringDecompressionSingleStreamOccupiedMoreMemoryThanAllowed = "During decompression a single stream occupied more memory than allowed. Please either check your pdf or increase the allowed multiple decompressed pdf streams maximum size value by setting the appropriate parameter of ReaderProperties's MemoryLimitsAwareHandler.";

        public const string DuringDecompressionSingleStreamOccupiedMoreThanMaxIntegerValue = "During decompression a single stream occupied more than a maximum integer value. Please check your pdf.";

        public const string EndOfContentStreamReachedBeforeEndOfImageData = "End of content stream reached before end of image data.";

        public const string ErrorWhileReadingObjectStream = "Error while reading Object Stream.";

        public const string EncryptedPayloadFileSpecDoesntHaveEncryptedPayloadDictionary = "Encrypted payload file spec shall have encrypted payload dictionary.";

        public const string EncryptedPayloadFileSpecShallBeIndirect = "Encrypted payload file spec shall be indirect.";

        public const string EncryptedPayloadFileSpecShallHaveEFDictionary = "Encrypted payload file spec shall have 'EF' key. The value of such key shall be a dictionary that contains embedded file stream.";

        public const string EncryptedPayloadFileSpecShallHaveTypeEqualToFilespec = "Encrypted payload file spec shall have 'Type' key. The value of such key shall be 'Filespec'.";

        public const string EncryptedPayloadShallHaveTypeEqualsToEncryptedPayloadIfPresent = "Encrypted payload dictionary shall have field 'Type' equal to 'EncryptedPayload' if present";

        public const string EncryptedPayloadShallHaveSubtype = "Encrypted payload shall have 'Subtype' field specifying crypto filter";

        public const string ExternalEntityElementFoundInXml = "External entity element found in XML. This entity will not be parsed to prevent XML attacks.";

        public const string FailedToGetTsaResponseFrom1 = "Failed to get TSA response from {0}.";

        public const string FieldFlatteningIsNotSupportedInAppendMode = "Field flattening is not supported in append mode.";

        public const string FieldAlreadySigned = "Field has been already signed.";

        public const string FieldNamesCannotContainADot = "Field names cannot contain a dot.";

        public const string FieldTypeIsNotASignatureFieldType = "Field type is not a signature field type.";

        public const string Filter1IsNotSupported = "Filter {0} is not supported.";

        public const string FilePosition1CrossReferenceEntryInThisXrefSubsection = "file position {0} cross reference entry in this xref subsection.";

        public const string FilterCcittfaxdecodeIsOnlySupportedForImages = "Filter CCITTFaxDecode is only supported for images";

        public const string FilterIsNotANameOrArray = "filter is not a name or array.";

        public const string FlushedPageCannotBeAddedOrInserted = "Flushed page cannot be added or inserted.";

        public const string FLUSHED_PAGE_CANNOT_BE_REMOVED = "Flushed page cannot be removed from a document which is tagged or has an AcroForm";

        public const string FlushingHelperFLushingModeIsNotForDocReadingMode = "Flushing writes the object to the output stream and releases it from memory. It is only possible for documents that have a PdfWriter associated with them. Use PageFlushingHelper#releaseDeep method instead.";

        public const string FontAndSizeMustBeSetBeforeWritingAnyText = "Font and size must be set before writing any text.";

        public const string FontEmbeddingIssue = "Font embedding issue.";

        public const string FontProviderNotSetFontFamilyNotResolved = "FontProvider and FontSet are empty. Cannot resolve font family name (see ElementPropertyContainer#setFontFamily) without initialized FontProvider (see RootElement#setFontProvider).";

        [Obsolete]
        public const string FontSizeIsTooSmall = "Font size is too small.";

        public const string FormXObjectMustHaveBbox = "Form XObject must have BBox.";

        public const string FunctionIsNotCompatibleWitColorSpace = "Function is not compatible with ColorSpace.";

        [Obsolete]
        public const string GivenAccessibleElementIsNotConnectedToAnyTag = "Given accessible element is not connected to any tag.";

        public const string IllegalCharacterInAsciihexdecode = "illegal character in ASCIIHexDecode.";

        public const string IllegalCharacterInAscii85decode = "Illegal character in ASCII85Decode.";

        public const string IllegalCharacterInCodabarBarcode = "Illegal character in Codabar Barcode.";

        public const string IllegalLengthValue = "Illegal length value.";

        public const string IllegalRValue = "Illegal R value.";

        public const string IllegalVValue = "Illegal V value.";

        public const string InAPageLabelThePageNumbersMustBeGreaterOrEqualTo1 = "In a page label the page numbers must be greater or equal to 1.";

        public const string InCodabarStartStopCharactersAreOnlyAllowedAtTheExtremes = "In Codabar, start/stop characters are only allowed at the extremes.";

        public const string InvalidHttpResponse1 = "Invalid http response {0}.";

        public const string InvalidTsa1ResponseCode2 = "Invalid TSA {0} response code {1}.";

        public const string IncorrectNumberOfComponents = "Incorrect number of components.";

        public const string InvalidCodewordSize = "Invalid codeword size.";

        public const string InvalidCrossReferenceEntryInThisXrefSubsection = "Invalid cross reference entry in this xref subsection.";

        public const string InvalidIndirectReference1 = "Invalid indirect reference {0}.";

        public const string InvalidMediaBoxValue = "Tne media box object has incorrect values.";

        public const string InvalidPageStructure1 = "Invalid page structure {0}.";

        public const string InvalidPageStructurePagesPagesMustBePdfDictionary = "Invalid page structure. /Pages must be PdfDictionary.";

        public const string InvalidRangeArray = "Invalid range array.";

        public const string InvalidOffsetForObject1 = "Invalid offset for object {0}.";

        public const string InvalidXrefStream = "Invalid xref stream.";

        public const string InvalidXrefTable = "Invalid xref table.";

        public const string IoException = "I/O exception.";

        public const string IoExceptionWhileCreatingFont = "I/O exception while creating Font";

        public const string LzwDecoderException = "LZW decoder exception.";

        public const string LzwFlavourNotSupported = "LZW flavour not supported.";

        public const string MacroSegmentIdMustBeGtOrEqZero = "macroSegmentId must be >= 0";

        public const string MacroSegmentIdMustBeGtZero = "macroSegmentId must be > 0";

        public const string MacroSegmentIdMustBeLtMacroSegmentCount = "macroSegmentId must be < macroSemgentCount";

        public const string MissingRequiredFieldInFontDictionary = "Missing required field {0} in font dictionary.";

        public const string MustBeATaggedDocument = "Must be a tagged document.";

        public const string NumberOfEntriesInThisXrefSubsectionNotFound = "Number of entries in this xref subsection not found.";

        public const string NoCompatibleEncryptionFound = "No compatible encryption found.";

        public const string NoCryptoDictionaryDefined = "No crypto dictionary defined.";

        public const string NoGlyphsDefinedForType3Font = "No glyphs defined for type3 font.";

        public const string NoKidWithSuchRole = "No kid with such role.";

        [Obsolete(@"Now we log a warning rather than throw an exception.")]
        public const string NoMaxLenPresent = "No /MaxLen has been set even though the Comb flag has been set.";

        public const string NoninvertibleMatrixCannotBeProcessed = "A noninvertible matrix has been parsed. The behaviour is unpredictable.";

        public const string NotAPlaceableWindowsMetafile = "Not a placeable windows metafile.";

        public const string NotAValidPkcs7ObjectNotASequence = "Not a valid PKCS#7 object - not a sequence";

        public const string NotAValidPkcs7ObjectNotSignedData = "Not a valid PKCS#7 object - not signed data.";

        public const string NotAWmfImage = "Not a WMF image.";

        public const string NoValidEncryptionMode = "No valid encryption mode.";

        public const string NumberOfBooleansInTheArrayDoesntCorrespondWithTheNumberOfFields = "The number of booleans in the array doesn't correspond with the number of fields.";

        public const string ObjectMustBeIndirectToWorkWithThisWrapper = "Object must be indirect to work with this wrapper.";

        public const string ObjectNumberOfTheFirstObjectInThisXrefSubsectionNotFound = "Object number of the first object in this xref subsection not found.";

        public const string OnlyIdentityCMapsSupportsWithTrueType = "Only Identity CMaps supports with truetype";

        public const string OnlyBmpCanBeWrappedInWmf = "Only BMP can be wrapped in WMF.";

        public const string OperatorEINotFoundAfterEndOfImageData = "Operator EI not found after the end of image data.";

        public const string Page1CannotBeAddedToDocument2BecauseItBelongsToDocument3 = "Page {0} cannot be added to document {1}, because it belongs to document {2}.";

        public const string PageIsNotSetForThePdfTagStructure = "Page is not set for the pdf tag structure.";

        public const string PageAlreadyFlushed = "The page has been already flushed.";

        public const string PageAlreadyFlushedUseAddFieldAppearanceToPageMethodBeforePageFlushing = "The page has been already flushed. Use PdfAcroForm#addFieldAppearanceToPage() method before page flushing.";

        public const string PdfEncodings = "PdfEncodings exception.";

        public const string PdfEncryption = "PdfEncryption exception.";

        public const string PdfDecryption = "Exception occurred with PDF document decryption. One of the possible reasons is wrong password or wrong public key certificate and private key.";

        public const string PdfDocumentMustBeOpenedInStampingMode = "PdfDocument must be opened in stamping mode.";

        public const string PdfFormXobjectHasInvalidBbox = "PdfFormXObject has invalid BBox.";

        public const string PdfObjectStreamReachMaxSize = "PdfObjectStream reach max size.";

        public const string PdfPagesTreeCouldBeGeneratedOnlyOnce = "PdfPages tree could be generated only once.";

        public const string PdfReaderHasBeenAlreadyUtilized = "Given PdfReader instance has already been utilized. The PdfReader cannot be reused, please create a new instance.";

        public const string PdfStartxrefIsNotFollowedByANumber = "PDF startxref is not followed by a number.";

        public const string PdfStartxrefNotFound = "PDF startxref not found.";

        public const string PdfIndirectObjectBelongsToOtherPdfDocument = "Pdf indirect object belongs to other PDF document. Copy object to current pdf document.";

        public const string PdfVersionNotValid = "PDF version is not valid.";

        public const string RefArrayItemsInStructureElementDictionaryShallBeIndirectObjects = "Ref array items in structure element dictionary shall be indirect objects.";

        public const string RequestedPageNumberIsOutOfBounds = "Requested page number {0} is out of bounds.";

        public const string PngFilterUnknown = "PNG filter unknown.";

        public const string PrintScalingEnforceEntryInvalid = "/PrintScaling shall may appear in the Enforce array only if the corresponding entry in the viewer preferences dictionary specifies a valid value other than AppDefault";

        public const string ResourcesCannotBeNull = "Resources cannot be null.";

        public const string ResourcesDoNotContainExtgstateEntryUnableToProcessOperator1 = "Resources do not contain ExtGState entry. Unable to process operator {0}.";

        public const string RoleIsNotMappedToAnyStandardRole = "Role \"{0}\" is not mapped to any standard role.";

        public const string RoleInNamespaceIsNotMappedToAnyStandardRole = "Role \"{0}\" in namespace {1} is not mapped to any standard role.";

        public const string ShadingTypeNotFound = "Shading type not found.";

        public const string SignatureWithName1IsNotTheLastItDoesntCoverWholeDocument = "Signature with name {0} is not the last. It doesn't cover the whole document.";

        public const string StdcfNotFoundEncryption = "/StdCF not found (encryption)";

        public const string StructParentIndexNotFoundInTaggedObject = "StructParent index not found in tagged object.";

        public const string StructureElementInStructureDestinationShallBeAnIndirectObject = "Structure element referenced by a structure destination shall be an indirect object.";

        public const string StructureElementShallContainParentObject = "StructureElement shall contain parent object.";

        public const string StructureElementDictionaryShallBeAnIndirectObjectInOrderToHaveChildren = "Structure element dictionary shall be an indirect object in order to have children.";

        public const string TagCannotBeMovedToTheAnotherDocumentsTagStructure = "Tag cannot be moved to the another document's tag structure.";

        public const string TagFromTheExistingTagStructureIsFlushedCannotAddCopiedPageTags = "Tag from the existing tag structure is flushed. Cannot add copied page tags.";

        public const string TagStructureCopyingFailedItMightBeCorruptedInOneOfTheDocuments = "Tag structure copying failed: it might be corrupted in one of the documents.";

        public const string TagStructureFlushingFailedItMightBeCorrupted = "Tag structure flushing failed: it might be corrupted.";

        public const string TagTreePointerIsInInvalidStateItPointsAtFlushedElementUseMoveToRoot = "TagTreePointer is in invalid state: it points at flushed element. Use TagTreePointer#moveToRoot.";

        public const string TagTreePointerIsInInvalidStateItPointsAtRemovedElementUseMoveToRoot = "TagTreePointer is in invalid state: it points at removed element use TagTreePointer#moveToRoot.";

        public const string TextCannotBeNull = "Text cannot be null.";

        public const string TextIsTooBig = "Text is too big.";

        public const string TextMustBeEven = "The text length must be even.";

        public const string TwoBarcodeMustBeExternally = "The two barcodes must be composed externally.";

        public const string ThereAreIllegalCharactersForBarcode128In1 = "There are illegal characters for barcode 128 in {0}.";

        public const string ThereIsNoAssociatePdfWriterForMakingIndirects = "There is no associate PdfWriter for making indirects.";

        public const string ThereIsNoFieldInTheDocumentWithSuchName1 = "There is no field in the document with such name: {0}.";

        public const string ThisPkcs7ObjectHasMultipleSignerinfosOnlyOneIsSupportedAtThisTime = "This PKCS#7 object has multiple SignerInfos. Only one is supported at this time.";

        public const string ThisInstanceOfPdfSignerAlreadyClosed = "This instance of PdfSigner has been already closed.";

        public const string ToFlushThisWrapperUnderlyingObjectMustBeAddedToDocument = "To manually flush this wrapper, you have to ensure that the object behind this wrapper is added to the document, i.e. it has an indirect reference.";

        public const string Tsa1FailedToReturnTimeStampToken2 = "TSA {0} failed to return time stamp token: {1}.";

        public const string TrailerNotFound = "Trailer not found.";

        public const string TrailerPrevEntryPointsToItsOwnCrossReferenceSection = "Trailer prev entry points to its own cross reference section.";

        public const string UnbalancedBeginEndMarkedContentOperators = "Unbalanced begin/end marked content operators.";

        public const string UnbalancedLayerOperators = "Unbalanced layer operators.";

        public const string UnbalancedSaveRestoreStateOperators = "Unbalanced save restore state operators.";

        public const string UnexpectedCharacter1FoundAfterIDInInlineImage = "Unexpected character {0} found after ID in inline image.";

        public const string UnexpectedCloseBracket = "Unexpected close bracket.";

        public const string UnexpectedColorSpace1 = "Unexpected ColorSpace: {0}.";

        public const string UnexpectedEndOfFile = "Unexpected end of file.";

        public const string UnexpectedGtGt = "unexpected >>.";

        public const string UnexpectedShadingType = "Unexpected shading type.";

        public const string UnknownEncryptionTypeREq1 = "Unknown encryption type R == {0}.";

        public const string UnknownEncryptionTypeVEq1 = "Unknown encryption type V == {0}.";

        public const string UnknownPdfException = "Unknown PdfException.";

        public const string UnknownHashAlgorithm1 = "Unknown hash algorithm: {0}.";

        public const string UnknownKeyAlgorithm1 = "Unknown key algorithm: {0}.";

        [Obsolete]
        public const string UnsupportedDefaultColorSpaceName1 = "Unsupported default color space name. Was {0}, but should be DefaultCMYK, DefaultGray or DefaultRGB";

        public const string UnsupportedFontEmbeddingStrategy = "Unsupported font embedding strategy.";

        public const string UnsupportedXObjectType = "Unsupported XObject type.";

        public const string VerificationAlreadyOutput = "Verification already output.";

        public const string WhenAddingObjectReferenceToTheTagTreeItMustBeConnectedToNotFlushedObject = "When adding object reference to the tag tree, it must be connected to not flushed object.";

        public const string WhitePointIsIncorrectlySpecified = "White point is incorrectly specified.";

        public const string WmfImageException = "WMF image exception.";

        public const string WrongFormFieldAddAnnotationToTheField = "Wrong form field. Add annotation to the field.";

        [Obsolete(@"in favour of more informative named constant")]
        public const string WrongMediaBoxSize1 = "Wrong media box size: {0}.";

        public const string WRONGMEDIABOXSIZETOOFEWARGUMENTS = "Wrong media box size: {0}. Need at least 4 arguments";

        public const string XrefSubsectionNotFound = "xref subsection not found.";

        public const string YouHaveToDefineABooleanArrayForThisCollectionSortDictionary = "You have to define a boolean array for this collection sort dictionary.";

        public const string YouMustSetAValueBeforeAddingAPrefix = "You must set a value before adding a prefix.";

        public const string YouNeedASingleBooleanForThisCollectionSortDictionary = "You need a single boolean for this collection sort dictionary.";

        public const string QuadPointArrayLengthIsNotAMultipleOfEight = "The QuadPoint Array length is not a multiple of 8.";

        /// <summary>Object for more details</summary>
        protected internal object @object;

        private IList<object> messageParams;

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        public PdfException(string message)
            : base(message) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        public PdfException(Exception cause)
            : this(UnknownPdfException, cause) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="obj">an object for more details.</param>
        public PdfException(string message, object obj)
            : this(message) {
            @object = obj;
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        public PdfException(string message, Exception cause)
            : base(message, cause) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        /// <param name="obj">an object for more details.</param>
        public PdfException(string message, Exception cause, object obj)
            : this(message, cause) {
            @object = obj;
        }

        public override string Message {
            get
            {
	            if (messageParams == null || messageParams.Count == 0) {
                    return base.Message;
                }

	            return MessageFormatUtil.Format(base.Message, GetMessageParams());
            }
        }

        /// <summary>Sets additional params for Exception message.</summary>
        /// <param name="messageParams">additional params.</param>
        /// <returns>object itself.</returns>
        public virtual PdfException SetMessageParams(params object[] messageParams) {
            this.messageParams = new List<object>();
            this.messageParams.AddAll(messageParams);
            return this;
        }

        /// <summary>Gets additional params for Exception message.</summary>
        /// <returns>array of additional params</returns>
        protected internal virtual object[] GetMessageParams() {
            var parameters = new object[messageParams.Count];
            for (var i = 0; i < messageParams.Count; i++) {
                parameters[i] = messageParams[i];
            }
            return parameters;
        }
    }
}
