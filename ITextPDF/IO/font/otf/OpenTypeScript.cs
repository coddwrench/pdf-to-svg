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

using System.Collections.Generic;

namespace  IText.IO.Font.Otf {
    public class OpenTypeScript {
        public readonly string DEFAULT_SCRIPT = "DFLT";

        private OpenTypeFontTableReader openTypeReader;

        private IList<ScriptRecord> records;

        public OpenTypeScript(OpenTypeFontTableReader openTypeReader, int locationScriptTable) {
            this.openTypeReader = openTypeReader;
            records = new List<ScriptRecord>();
            openTypeReader.rf.Seek(locationScriptTable);
            var tagsLocs = openTypeReader.ReadTagAndLocations(locationScriptTable);
            foreach (var tagLoc in tagsLocs) {
                ReadScriptRecord(tagLoc);
            }
        }

        public virtual IList<ScriptRecord> GetScriptRecords() {
            return records;
        }

        public virtual LanguageRecord GetLanguageRecord(string[] scripts, string language) {
            ScriptRecord scriptFound = null;
            ScriptRecord scriptDefault = null;
            foreach (var sr in records) {
                if (DEFAULT_SCRIPT.Equals(sr.tag)) {
                    scriptDefault = sr;
                    break;
                }
            }
            foreach (var script in scripts) {
                foreach (var sr in records) {
                    if (sr.tag.Equals(script)) {
                        scriptFound = sr;
                        break;
                    }
                    if (DEFAULT_SCRIPT.Equals(script)) {
                        scriptDefault = sr;
                    }
                }
                if (scriptFound != null) {
                    break;
                }
            }
            if (scriptFound == null) {
                scriptFound = scriptDefault;
            }
            if (scriptFound == null) {
                return null;
            }
            LanguageRecord lang = null;
            foreach (var lr in scriptFound.languages) {
                if (lr.tag.Equals(language)) {
                    lang = lr;
                    break;
                }
            }
            if (lang == null) {
                lang = scriptFound.defaultLanguage;
            }
            return lang;
        }

        private void ReadScriptRecord(TagAndLocation tagLoc) {
            openTypeReader.rf.Seek(tagLoc.location);
            var locationDefaultLanguage = openTypeReader.rf.ReadUnsignedShort();
            if (locationDefaultLanguage > 0) {
                locationDefaultLanguage += tagLoc.location;
            }
            var tagsLocs = openTypeReader.ReadTagAndLocations(tagLoc.location);
            var srec = new ScriptRecord();
            srec.tag = tagLoc.tag;
            srec.languages = new LanguageRecord[tagsLocs.Length];
            for (var k = 0; k < tagsLocs.Length; ++k) {
                srec.languages[k] = ReadLanguageRecord(tagsLocs[k]);
            }
            if (locationDefaultLanguage > 0) {
                var t = new TagAndLocation();
                t.tag = "";
                t.location = locationDefaultLanguage;
                srec.defaultLanguage = ReadLanguageRecord(t);
            }
            records.Add(srec);
        }

        private LanguageRecord ReadLanguageRecord(TagAndLocation tagLoc) {
            var rec = new LanguageRecord();
            //skip lookup order
            openTypeReader.rf.Seek(tagLoc.location + 2);
            rec.featureRequired = openTypeReader.rf.ReadUnsignedShort();
            var count = openTypeReader.rf.ReadUnsignedShort();
            rec.features = openTypeReader.ReadUShortArray(count);
            rec.tag = tagLoc.tag;
            return rec;
        }
    }
}
