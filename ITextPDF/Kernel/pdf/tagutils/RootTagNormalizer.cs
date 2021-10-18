/*
This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: iText Software.

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

using System.Collections.Generic;
using IText.IO;
using IText.Kernel.Pdf.Tagging;
using IText.Logger;

namespace IText.Kernel.Pdf.Tagutils {
    internal class RootTagNormalizer {
        private TagStructureContext context;

        private PdfStructElem rootTagElement;

        private PdfDocument document;

        internal RootTagNormalizer(TagStructureContext context, PdfStructElem rootTagElement, PdfDocument document
            ) {
            this.context = context;
            this.rootTagElement = rootTagElement;
            this.document = document;
        }

        internal virtual PdfStructElem MakeSingleStandardRootTag(IList<IStructureNode> rootKids) {
            document.GetStructTreeRoot().MakeIndirect(document);
            if (rootTagElement == null) {
                CreateNewRootTag();
            }
            else {
                rootTagElement.MakeIndirect(document);
                document.GetStructTreeRoot().AddKid(rootTagElement);
                EnsureExistingRootTagIsDocument();
            }
            AddStructTreeRootKidsToTheRootTag(rootKids);
            return rootTagElement;
        }

        private void CreateNewRootTag() {
            IRoleMappingResolver mapping;
            var docDefaultNs = context.GetDocumentDefaultNamespace();
            mapping = context.ResolveMappingToStandardOrDomainSpecificRole(StandardRoles.DOCUMENT, docDefaultNs);
            if (mapping == null || mapping.CurrentRoleIsStandard() && !StandardRoles.DOCUMENT.Equals(mapping.GetRole()
                )) {
                LogCreatedRootTagHasMappingIssue(docDefaultNs, mapping);
            }
            rootTagElement = document.GetStructTreeRoot().AddKid(new PdfStructElem(document, PdfName.Document));
            if (context.TargetTagStructureVersionIs2()) {
                rootTagElement.SetNamespace(docDefaultNs);
                context.EnsureNamespaceRegistered(docDefaultNs);
            }
        }

        private void EnsureExistingRootTagIsDocument() {
            IRoleMappingResolver mapping;
            mapping = context.GetRoleMappingResolver(rootTagElement.GetRole().GetValue(), rootTagElement.GetNamespace(
                ));
            var isDocBeforeResolving = mapping.CurrentRoleIsStandard() && StandardRoles.DOCUMENT.Equals(mapping.GetRole
                ());
            mapping = context.ResolveMappingToStandardOrDomainSpecificRole(rootTagElement.GetRole().GetValue(), rootTagElement
                .GetNamespace());
            var isDocAfterResolving = mapping != null && mapping.CurrentRoleIsStandard() && StandardRoles.DOCUMENT.Equals
                (mapping.GetRole());
            if (isDocBeforeResolving && !isDocAfterResolving) {
                LogCreatedRootTagHasMappingIssue(rootTagElement.GetNamespace(), mapping);
            }
            else {
                if (!isDocAfterResolving) {
                    WrapAllKidsInTag(rootTagElement, rootTagElement.GetRole(), rootTagElement.GetNamespace());
                    rootTagElement.SetRole(PdfName.Document);
                    if (context.TargetTagStructureVersionIs2()) {
                        rootTagElement.SetNamespace(context.GetDocumentDefaultNamespace());
                        context.EnsureNamespaceRegistered(context.GetDocumentDefaultNamespace());
                    }
                }
            }
        }

        private void AddStructTreeRootKidsToTheRootTag(IList<IStructureNode> rootKids) {
            var originalRootKidsIndex = 0;
            var isBeforeOriginalRoot = true;
            foreach (var elem in rootKids) {
                // StructTreeRoot kids are always PdfStructElement, so we are save here to cast it
                var kid = (PdfStructElem)elem;
                if (kid.GetPdfObject() == rootTagElement.GetPdfObject()) {
                    isBeforeOriginalRoot = false;
                    continue;
                }
                // This boolean is used to "flatten" possible deep "stacking" of the tag structure in case of the multiple pages copying operations.
                // This could happen due to the wrapping of all the kids in the createNewRootTag or ensureExistingRootTagIsDocument methods.
                // And therefore, we don't need here to resolve mappings, because we exactly know which role we set.
                var kidIsDocument = PdfName.Document.Equals(kid.GetRole());
                if (kidIsDocument && kid.GetNamespace() != null && context.TargetTagStructureVersionIs2()) {
                    // we flatten only tags of document role in standard structure namespace
                    var kidNamespaceName = kid.GetNamespace().GetNamespaceName();
                    kidIsDocument = StandardNamespaces.PDF_1_7.Equals(kidNamespaceName) || StandardNamespaces.PDF_2_0.Equals(kidNamespaceName
                        );
                }
                if (isBeforeOriginalRoot) {
                    rootTagElement.AddKid(originalRootKidsIndex, kid);
                    originalRootKidsIndex += kidIsDocument ? kid.GetKids().Count : 1;
                }
                else {
                    rootTagElement.AddKid(kid);
                }
                if (kidIsDocument) {
                    RemoveOldRoot(kid);
                }
            }
        }

        private void WrapAllKidsInTag(PdfStructElem parent, PdfName wrapTagRole, PdfNamespace wrapTagNs) {
            var kidsNum = parent.GetKids().Count;
            var tagPointer = new TagTreePointer(parent, document);
            tagPointer.AddTag(0, wrapTagRole.GetValue());
            if (context.TargetTagStructureVersionIs2()) {
                tagPointer.GetProperties().SetNamespace(wrapTagNs);
            }
            var newParentOfKids = new TagTreePointer(tagPointer);
            tagPointer.MoveToParent();
            for (var i = 0; i < kidsNum; ++i) {
                tagPointer.RelocateKid(1, newParentOfKids);
            }
        }

        private void RemoveOldRoot(PdfStructElem oldRoot) {
            var tagPointer = new TagTreePointer(document);
            tagPointer.SetCurrentStructElem(oldRoot).RemoveTag();
        }

        private void LogCreatedRootTagHasMappingIssue(PdfNamespace rootTagOriginalNs, IRoleMappingResolver mapping
            ) {
            var origRootTagNs = "";
            if (rootTagOriginalNs != null && rootTagOriginalNs.GetNamespaceName() != null) {
                origRootTagNs = " in \"" + rootTagOriginalNs.GetNamespaceName() + "\" namespace";
            }
            var mappingRole = " to ";
            if (mapping != null) {
                mappingRole += "\"" + mapping.GetRole() + "\"";
                if (mapping.GetNamespace() != null && !StandardNamespaces.PDF_1_7.Equals(mapping.GetNamespace().GetNamespaceName
                    ())) {
                    mappingRole += " in \"" + mapping.GetNamespace().GetNamespaceName() + "\" namespace";
                }
            }
            else {
                mappingRole += "not standard role";
            }
            var logger = LogManager.GetLogger(typeof(RootTagNormalizer));
            logger.Warn(string.Format(LogMessageConstant.CREATED_ROOT_TAG_HAS_MAPPING, origRootTagNs, mappingRole
                ));
        }
    }
}
