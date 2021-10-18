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
using System.Globalization;
using IText.IO.Font.Constants;
using IText.IO.Util;

namespace  IText.IO.Font {
    public class CidFontProperties {
        private static readonly IDictionary<string, IDictionary<string, object>> allFonts = new Dictionary<string, 
            IDictionary<string, object>>();

        private static readonly IDictionary<string, ICollection<string>> registryNames = new Dictionary<string, ICollection
            <string>>();

        static CidFontProperties() {
            try {
                LoadRegistry();
                foreach (var font in registryNames.Get("fonts")) {
                    allFonts.Put(font, ReadFontProperties(font));
                }
            }
            catch (Exception) {
            }
        }

        /// <summary>Checks if its a valid CJKFont font.</summary>
        /// <param name="fontName">the font name.</param>
        /// <param name="enc">the encoding.</param>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if it is CJKFont.
        /// </returns>
        public static bool IsCidFont(string fontName, string enc) {
            if (!registryNames.ContainsKey("fonts")) {
                return false;
            }
            if (!registryNames.Get("fonts").Contains(fontName)) {
                return false;
            }
            if (enc.Equals(PdfEncodings.IDENTITY_H) || enc.Equals(PdfEncodings.IDENTITY_V)) {
                return true;
            }
            var registry = (string)allFonts.Get(fontName).Get("Registry");
            var encodings = registryNames.Get(registry);
            return encodings != null && encodings.Contains(enc);
        }

        public static string GetCompatibleFont(string enc) {
            foreach (var e in registryNames) {
                if (e.Value.Contains(enc)) {
                    var registry = e.Key;
                    foreach (var e1 in allFonts) {
                        if (registry.Equals(e1.Value.Get("Registry"))) {
                            return e1.Key;
                        }
                    }
                }
            }
            return null;
        }

        public static IDictionary<string, IDictionary<string, object>> GetAllFonts() {
            return allFonts;
        }

        public static IDictionary<string, ICollection<string>> GetRegistryNames() {
            return registryNames;
        }

        private static void LoadRegistry() {
            var resource = ResourceUtil.GetResourceStream(FontResources.CMAPS + "cjk_registry.properties");
            var p = new Properties();
            p.Load(resource);
            resource.Dispose();
            foreach (var key in p.Keys) {
                var value = p.GetProperty((string)key);
                var sp = StringUtil.Split(value, " ");
                ICollection<string> hs = new HashSet<string>();
                foreach (var s in sp) {
                    if (s.Length > 0) {
                        hs.Add(s);
                    }
                }
                registryNames.Put((string)key, hs);
            }
        }

        private static IDictionary<string, object> ReadFontProperties(string name) {
            name += ".properties";
            var resource = ResourceUtil.GetResourceStream(FontResources.CMAPS + name);
            var p = new Properties();
            p.Load(resource);
            resource.Dispose();
            var W = CreateMetric(p.GetProperty("W"));
            p.Remove("W");
            var W2 = CreateMetric(p.GetProperty("W2"));
            p.Remove("W2");
            IDictionary<string, object> map = new Dictionary<string, object>();
            foreach (var obj in p.Keys) {
                map.Put((string)obj, p.GetProperty((string)obj));
            }
            map.Put("W", W);
            map.Put("W2", W2);
            return map;
        }

        private static IntHashtable CreateMetric(string s) {
            var h = new IntHashtable();
            var tk = new StringTokenizer(s);
            while (tk.HasMoreTokens()) {
                var n1 = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
                h.Put(n1, Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture));
            }
            return h;
        }
    }
}
