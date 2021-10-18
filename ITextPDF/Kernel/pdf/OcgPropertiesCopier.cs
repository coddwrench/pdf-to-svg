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
using IText.IO;
using IText.IO.Font;
using IText.IO.Util;
using IText.Logger;

namespace IText.Kernel.Pdf
{
	internal sealed class OcgPropertiesCopier
	{
		private static readonly ILog LOGGER = LogManager.GetLogger(typeof(OcgPropertiesCopier));

		private OcgPropertiesCopier()
		{
		}

		// Empty constructor
		public static void CopyOCGProperties(PdfDocument fromDocument, PdfDocument toDocument, IDictionary<PdfPage
			, PdfPage> page2page)
		{
			try
			{
				// Configs are not copied
				var toOcProperties = toDocument.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.OCProperties
					);
				var fromOcgsToCopy = GetAllUsedNonFlushedOCGs
					(page2page, toOcProperties);
				if (fromOcgsToCopy.IsEmpty())
				{
					return;
				}
				// Reset ocProperties field in order to create it a new at the
				// method end using the new (merged) OCProperties dictionary
				toOcProperties = toDocument.GetCatalog().FillAndGetOcPropertiesDictionary();
				var fromOcProperties = fromDocument.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.OCProperties
					);
				CopyOCGs(fromOcgsToCopy, toOcProperties, toDocument);
				CopyDDictionary(fromOcgsToCopy, fromOcProperties.GetAsDictionary(PdfName
					.D), toOcProperties, toDocument);
			}
			catch (Exception ex)
			{
				LOGGER.Error(MessageFormatUtil.Format(LogMessageConstant.OCG_COPYING_ERROR, ex.ToString()));
			}
		}

		private static ICollection<PdfIndirectReference> GetAllUsedNonFlushedOCGs(IDictionary<PdfPage, PdfPage> page2page
			, PdfDictionary toOcProperties)
		{
			// NOTE: the PDF is considered to be valid and therefore the presence of OСG in OCProperties.OCGs is not checked
			ICollection<PdfIndirectReference> fromUsedOcgs = new LinkedHashSet<PdfIndirectReference>();
			// Visit the pages in parallel to find non-flush OSGs
			var fromPages = page2page.Keys.ToArray(new PdfPage[0]);
			var toPages = page2page.Values.ToArray(new PdfPage[0]);
			for (var i = 0; i < toPages.Length; i++)
			{
				var fromPage = fromPages[i];
				var toPage = toPages[i];
				// Copy OCGs from annotations
				var toAnnotations = toPage.GetAnnotations();
				var fromAnnotations = fromPage.GetAnnotations();
				for (var j = 0; j < toAnnotations.Count; j++)
				{
					if (!toAnnotations[j].IsFlushed())
					{
						var toAnnotDict = toAnnotations[j].GetPdfObject();
						var fromAnnotDict = fromAnnotations[j].GetPdfObject();
						var toAnnot = toAnnotations[j];
						var fromAnnot = fromAnnotations[j];
						if (!toAnnotDict.IsFlushed())
						{
							GetUsedNonFlushedOCGsFromOcDict(toAnnotDict.GetAsDictionary(PdfName.OC
								), fromAnnotDict.GetAsDictionary(PdfName.OC), fromUsedOcgs, toOcProperties);
							GetUsedNonFlushedOCGsFromXObject(toAnnot.GetNormalAppearanceObject(),
								fromAnnot.GetNormalAppearanceObject(), fromUsedOcgs, toOcProperties);
							GetUsedNonFlushedOCGsFromXObject(toAnnot.GetRolloverAppearanceObject(
								), fromAnnot.GetRolloverAppearanceObject(), fromUsedOcgs, toOcProperties);
							GetUsedNonFlushedOCGsFromXObject(toAnnot.GetDownAppearanceObject(), fromAnnot
								.GetDownAppearanceObject(), fromUsedOcgs, toOcProperties);
						}
					}
				}
				var toResources = toPage.GetPdfObject().GetAsDictionary(PdfName.Resources);
				var fromResources = fromPage.GetPdfObject().GetAsDictionary(PdfName.Resources);
				GetUsedNonFlushedOCGsFromResources(toResources, fromResources, fromUsedOcgs
					, toOcProperties);
			}
			return fromUsedOcgs;
		}

		private static void GetUsedNonFlushedOCGsFromResources(PdfDictionary toResources, PdfDictionary fromResources
			, ICollection<PdfIndirectReference> fromUsedOcgs, PdfDictionary toOcProperties)
		{
			if (toResources != null && !toResources.IsFlushed())
			{
				// Copy OCGs from properties
				var toProperties = toResources.GetAsDictionary(PdfName.Properties);
				var fromProperties = fromResources.GetAsDictionary(PdfName.Properties);
				if (toProperties != null && !toProperties.IsFlushed())
				{
					foreach (var name in toProperties.KeySet())
					{
						var toCurrObj = toProperties.Get(name);
						var fromCurrObj = fromProperties.Get(name);
						GetUsedNonFlushedOCGsFromOcDict(toCurrObj, fromCurrObj, fromUsedOcgs,
							toOcProperties);
					}
				}
				// Copy OCGs from xObject
				var toXObject = toResources.GetAsDictionary(PdfName.XObject);
				var fromXObject = fromResources.GetAsDictionary(PdfName.XObject);
				GetUsedNonFlushedOCGsFromXObject(toXObject, fromXObject, fromUsedOcgs
					, toOcProperties);
			}
		}

		private static void GetUsedNonFlushedOCGsFromXObject(PdfDictionary toXObject, PdfDictionary fromXObject, ICollection
			<PdfIndirectReference> fromUsedOcgs, PdfDictionary toOcProperties)
		{
			if (toXObject != null && !toXObject.IsFlushed())
			{
				if (toXObject.IsStream() && !toXObject.IsFlushed())
				{
					var toStream = (PdfStream)toXObject;
					var fromStream = (PdfStream)fromXObject;
					GetUsedNonFlushedOCGsFromOcDict(toStream.GetAsDictionary(PdfName.OC), fromStream.GetAsDictionary(PdfName.OC), fromUsedOcgs, toOcProperties);
					GetUsedNonFlushedOCGsFromResources(toStream.GetAsDictionary(PdfName.Resources), fromStream.GetAsDictionary(PdfName.Resources), fromUsedOcgs, toOcProperties);
				}
				else
				{
					foreach (var name in toXObject.KeySet())
					{
						var toCurrObj = toXObject.Get(name);
						var fromCurrObj = fromXObject.Get(name);
						if (toCurrObj.IsStream() && !toCurrObj.IsFlushed())
						{
							var toStream = (PdfStream)toCurrObj;
							var fromStream = (PdfStream)fromCurrObj;
							GetUsedNonFlushedOCGsFromXObject(toStream, fromStream, fromUsedOcgs, toOcProperties);
						}
					}
				}
			}
		}

		private static void GetUsedNonFlushedOCGsFromOcDict(PdfObject toObj, PdfObject fromObj, ICollection<PdfIndirectReference
			> fromUsedOcgs, PdfDictionary toOcProperties)
		{
			if (toObj != null && toObj.IsDictionary() && !toObj.IsFlushed())
			{
				var toCurrDict = (PdfDictionary)toObj;
				var fromCurrDict = (PdfDictionary)fromObj;
				var typeName = toCurrDict.GetAsName(PdfName.Type);
				if (PdfName.OCG.Equals(typeName) && !OcgAlreadyInOCGs(toCurrDict.GetIndirectReference
					(), toOcProperties))
				{
					fromUsedOcgs.Add(fromCurrDict.GetIndirectReference());
				}
				else
				{
					if (PdfName.OCMD.Equals(typeName))
					{
						PdfArray toOcgs = null;
						PdfArray fromOcgs = null;
						if (toCurrDict.GetAsDictionary(PdfName.OCGs) != null)
						{
							toOcgs = new PdfArray();
							toOcgs.Add(toCurrDict.GetAsDictionary(PdfName.OCGs));
							fromOcgs = new PdfArray();
							fromOcgs.Add(fromCurrDict.GetAsDictionary(PdfName.OCGs));
						}
						else
						{
							if (toCurrDict.GetAsArray(PdfName.OCGs) != null)
							{
								toOcgs = toCurrDict.GetAsArray(PdfName.OCGs);
								fromOcgs = fromCurrDict.GetAsArray(PdfName.OCGs);
							}
						}
						if (toOcgs != null && !toOcgs.IsFlushed())
						{
							for (var i = 0; i < toOcgs.Size(); i++)
							{
								GetUsedNonFlushedOCGsFromOcDict(toOcgs.Get(i), fromOcgs.Get(i), fromUsedOcgs
									, toOcProperties);
							}
						}
					}
				}
			}
		}

		private static void CopyOCGs(ICollection<PdfIndirectReference> fromOcgsToCopy, PdfDictionary toOcProperties
			, PdfDocument toDocument)
		{
			ICollection<string> layerNames = new HashSet<string>();
			if (toOcProperties.GetAsArray(PdfName.OCGs) != null)
			{
				var toOcgs = toOcProperties.GetAsArray(PdfName.OCGs);
				foreach (var toOcgObj in toOcgs)
				{
					if (toOcgObj.IsDictionary())
					{
						layerNames.Add(((PdfDictionary)toOcgObj).GetAsString(PdfName.Name).ToUnicodeString());
					}
				}
			}
			var hasConflictingNames = false;
			foreach (var fromOcgRef in fromOcgsToCopy)
			{
				var toOcg = (PdfDictionary)fromOcgRef.GetRefersTo().CopyTo(toDocument, false);
				var currentLayerName = toOcg.GetAsString(PdfName.Name).ToUnicodeString();
				if (layerNames.Contains(currentLayerName))
				{
					hasConflictingNames = true;
					var i = 0;
					while (layerNames.Contains(currentLayerName + "_" + i))
					{
						i++;
					}
					currentLayerName += "_" + i;
					toOcg.Put(PdfName.Name, new PdfString(currentLayerName, PdfEncodings.UNICODE_BIG));
				}
				if (toOcProperties.GetAsArray(PdfName.OCGs) == null)
				{
					toOcProperties.Put(PdfName.OCGs, new PdfArray());
				}
				toOcProperties.GetAsArray(PdfName.OCGs).Add(toOcg);
			}
			if (hasConflictingNames)
			{
				LOGGER.Warn(LogMessageConstant.DOCUMENT_HAS_CONFLICTING_OCG_NAMES);
			}
		}

		private static bool OcgAlreadyInOCGs(PdfIndirectReference toOcgRef, PdfDictionary toOcProperties)
		{
			if (toOcProperties == null)
			{
				return false;
			}
			var toOcgs = toOcProperties.GetAsArray(PdfName.OCGs);
			if (toOcgs != null)
			{
				foreach (var toOcg in toOcgs)
				{
					if (toOcgRef.Equals(toOcg.GetIndirectReference()))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static void CopyDDictionary(ICollection<PdfIndirectReference> fromOcgsToCopy, PdfDictionary fromDDict
			, PdfDictionary toOcProperties, PdfDocument toDocument)
		{
			if (toOcProperties.GetAsDictionary(PdfName.D) == null)
			{
				toOcProperties.Put(PdfName.D, new PdfDictionary());
			}
			var toDDict = toOcProperties.GetAsDictionary(PdfName.D);
			// The Name field is not copied because it will be given when flushing the PdfOCProperties
			// Delete the Creator field because the D dictionary are changing
			toDDict.Remove(PdfName.Creator);
			// The BaseState field is not copied because for dictionary D BaseState should have the value ON, which is the default
			CopyDArrayField(PdfName.ON, fromOcgsToCopy, fromDDict, toDDict, toDocument
				);
			CopyDArrayField(PdfName.OFF, fromOcgsToCopy, fromDDict, toDDict, toDocument
				);
			// The Intent field is not copied because for dictionary D Intent should have the value View, which is the default
			// The AS field is not copied because it will be given when flushing the PdfOCProperties
			CopyDArrayField(PdfName.Order, fromOcgsToCopy, fromDDict, toDDict, toDocument
				);
			// The ListModel field is not copied because it only affects the visual presentation of the layers
			CopyDArrayField(PdfName.RBGroups, fromOcgsToCopy, fromDDict, toDDict,
				toDocument);
			CopyDArrayField(PdfName.Locked, fromOcgsToCopy, fromDDict, toDDict, toDocument
				);
		}

		private static void AttemptToAddObjectToArray(ICollection<PdfIndirectReference> fromOcgsToCopy, PdfObject
			fromObj, PdfArray toArray, PdfDocument toDocument)
		{
			var fromObjRef = fromObj.GetIndirectReference();
			if (fromObjRef != null && fromOcgsToCopy.Contains(fromObjRef))
			{
				toArray.Add(fromObj.CopyTo(toDocument, false));
			}
		}

		private static void CopyDArrayField(PdfName fieldToCopy, ICollection<PdfIndirectReference> fromOcgsToCopy,
			PdfDictionary fromDict, PdfDictionary toDict, PdfDocument toDocument)
		{
			if (fromDict.GetAsArray(fieldToCopy) == null)
			{
				return;
			}
			var fromArray = fromDict.GetAsArray(fieldToCopy);
			if (toDict.GetAsArray(fieldToCopy) == null)
			{
				toDict.Put(fieldToCopy, new PdfArray());
			}
			var toArray = toDict.GetAsArray(fieldToCopy);
			ICollection<PdfIndirectReference> toOcgsToCopy = new HashSet<PdfIndirectReference>();
			foreach (var fromRef in fromOcgsToCopy)
			{
				toOcgsToCopy.Add(fromRef.GetRefersTo().CopyTo(toDocument, false).GetIndirectReference());
			}
			if (PdfName.Order.Equals(fieldToCopy))
			{
				// Stage 1: delete all Order the entire branches from the output document in which the copied OCGs were
				IList<int> removeIndex = new List<int>();
				for (var i = 0; i < toArray.Size(); i++)
				{
					var toOrderItem = toArray.Get(i);
					if (OrderBranchContainsSetElements(toOrderItem, toArray, i, toOcgsToCopy
						, null, null))
					{
						removeIndex.Add(i);
					}
				}
				for (var i = removeIndex.Count - 1; i > -1; i--)
				{
					toArray.Remove(removeIndex[i]);
				}
				var toOcgs = toDocument.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.OCProperties).GetAsArray(
					PdfName.OCGs);
				// Stage 2: copy all the Order the entire branches in which the copied OСGs were
				for (var i = 0; i < fromArray.Size(); i++)
				{
					var fromOrderItem = fromArray.Get(i);
					if (OrderBranchContainsSetElements(fromOrderItem, fromArray, i, fromOcgsToCopy
						, toOcgs, toDocument))
					{
						toArray.Add(fromOrderItem.CopyTo(toDocument, false));
					}
				}
			}
			else
			{
				// Stage 3: remove from Order OCGs not presented in the output document. When forming
				// the Order dictionary in the PdfOcProperties constructor, only those OCGs that are
				// in the OCProperties/OCGs array will be taken into account
				if (PdfName.RBGroups.Equals(fieldToCopy))
				{
					// Stage 1: delete all RBGroups from the output document in which the copied OCGs were
					for (var i = toArray.Size() - 1; i > -1; i--)
					{
						var toRbGroup = (PdfArray)toArray.Get(i);
						foreach (var toRbGroupItemObj in toRbGroup)
						{
							if (toOcgsToCopy.Contains(toRbGroupItemObj.GetIndirectReference()))
							{
								toArray.Remove(i);
								break;
							}
						}
					}
					// Stage 2: copy all the RBGroups in which the copied OCGs were
					foreach (var fromRbGroupObj in fromArray)
					{
						var fromRbGroup = (PdfArray)fromRbGroupObj;
						foreach (var fromRbGroupItemObj in fromRbGroup)
						{
							if (fromOcgsToCopy.Contains(fromRbGroupItemObj.GetIndirectReference()))
							{
								toArray.Add(fromRbGroup.CopyTo(toDocument, false));
								break;
							}
						}
					}
				}
				else
				{
					// Stage 3: remove from RBGroups OCGs not presented in the output
					// document (is in the PdfOcProperties#fillDictionary method)
					foreach (var fromObj in fromArray)
					{
						AttemptToAddObjectToArray(fromOcgsToCopy, fromObj, toArray, toDocument
							);
					}
				}
			}
			if (toArray.IsEmpty())
			{
				toDict.Remove(fieldToCopy);
			}
		}

		private static bool OrderBranchContainsSetElements(PdfObject arrayObj, PdfArray array, int currentIndex, ICollection
			<PdfIndirectReference> ocgs, PdfArray toOcgs, PdfDocument toDocument)
		{
			if (arrayObj.IsDictionary())
			{
				if (ocgs.Contains(arrayObj.GetIndirectReference()))
				{
					return true;
				}

				if (currentIndex < (array.Size() - 1) && array.Get(currentIndex + 1).IsArray())
				{
					var nextArray = array.GetAsArray(currentIndex + 1);
					if (!nextArray.Get(0).IsString())
					{
						var result = OrderBranchContainsSetElements(nextArray, array, currentIndex
							+ 1, ocgs, toOcgs, toDocument);
						if (result && toOcgs != null && !ocgs.Contains(arrayObj.GetIndirectReference()))
						{
							// Add the OCG to the OCGs array to register the OCG in document, since it is not used
							// directly in the document, but is used as a parent for the order group. If it is not added
							// to the OCGs array, then the OCG will be deleted at the 3rd stage of the /Order entry coping.
							toOcgs.Add(arrayObj.CopyTo(toDocument, false));
						}
						return result;
					}
				}
			}
			else
			{
				if (arrayObj.IsArray())
				{
					var arrayItem = (PdfArray)arrayObj;
					for (var i = 0; i < arrayItem.Size(); i++)
					{
						var obj = arrayItem.Get(i);
						if (OrderBranchContainsSetElements(obj, arrayItem, i, ocgs, toOcgs, toDocument
							))
						{
							return true;
						}
					}
					if (!arrayItem.IsEmpty() && !arrayItem.Get(0).IsString())
					{
						if (currentIndex > 0 && array.Get(currentIndex - 1).IsDictionary())
						{
							var previousDict = (PdfDictionary)array.Get(currentIndex - 1);
							return ocgs.Contains(previousDict.GetIndirectReference());
						}
					}
				}
			}
			return false;
		}
	}
}
