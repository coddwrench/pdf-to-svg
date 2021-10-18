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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IText.IO.Font.Cmap;
using IText.IO.Font.Constants;
using IText.IO.Util;

namespace IText.IO.Font
{
	public class FontCache
	{
		private static readonly IDictionary<string, IDictionary<string, object>> allCidFonts = new LinkedDictionary
			<string, IDictionary<string, object>>();

		private static readonly IDictionary<string, ICollection<string>> registryNames = new Dictionary<string, ICollection
			<string>>();

		private const string CJK_REGISTRY_FILENAME = "cjk_registry.properties";

		private const string FONTS_PROP = "fonts";

		private const string REGISTRY_PROP = "Registry";

		private const string W_PROP = "W";

		private const string W2_PROP = "W2";

		private static IDictionary<FontCacheKey, FontProgram> fontCache = new ConcurrentDictionary<FontCacheKey, FontProgram>();

		static FontCache()
		{
			try
			{
				LoadRegistry();
				if (registryNames.Any())
					foreach (var font in registryNames.Get(FONTS_PROP))
						allCidFonts.Put(font, ReadFontProperties(font));
			}
			catch (Exception) { }
		}

		/// <summary>
		/// Checks if the font with the given name and encoding is one
		/// of the predefined CID fonts.
		/// </summary>
		/// <param name="fontName">the font name.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if it is CJKFont.
		/// </returns>
		protected internal static bool IsPredefinedCidFont(string fontName)
		{
			if (!registryNames.ContainsKey(FONTS_PROP))
			{
				return false;
			}

			if (!registryNames.Get(FONTS_PROP).Contains(fontName))
			{
				return false;
			}
			return true;
		}

		/// <summary>Finds a CJK font family which is compatible to the given CMap.</summary>
		/// <param name="cmap">a name of the CMap for which compatible font is searched.</param>
		/// <returns>a CJK font name if there's known compatible font for the given cmap name, or null otherwise.</returns>
		public static string GetCompatibleCidFont(string cmap)
		{
			foreach (var e in registryNames)
			{
				if (e.Value.Contains(cmap))
				{
					var registry = e.Key;
					foreach (var e1 in allCidFonts)
					{
						if (registry.Equals(e1.Value.Get(REGISTRY_PROP)))
						{
							return e1.Key;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Finds all CMap names that belong to the same registry to which a given
		/// font belongs.
		/// </summary>
		/// <param name="fontName">a name of the font for which CMap's are searched.</param>
		/// <returns>a set of CMap names corresponding to the given font.</returns>
		public static ICollection<string> GetCompatibleCmaps(string fontName)
		{
			var cidFonts = GetAllPredefinedCidFonts().Get(fontName);
			if (cidFonts == null)
			{
				return null;
			}
			var registry = (string)cidFonts.Get(REGISTRY_PROP);
			return registryNames.Get(registry);
		}

		public static IDictionary<string, IDictionary<string, object>> GetAllPredefinedCidFonts()
		{
			return allCidFonts;
		}

		public static IDictionary<string, ICollection<string>> GetRegistryNames()
		{
			return registryNames;
		}

		/// <summary>Parses CMap with a given name producing it in a form of cid to unicode mapping.</summary>
		/// <param name="uniMap">a CMap name. It is expected that CMap identified by this name defines unicode to cid mapping.
		///     </param>
		/// <returns>an object for convenient mapping from cid to unicode. If no CMap was found for provided name an exception is thrown.
		///     </returns>
		public static CMapCidUni GetCid2UniCmap(string uniMap)
		{
			var cidUni = new CMapCidUni();
			return ParseCmap(uniMap, cidUni);
		}

		public static CMapUniCid GetUni2CidCmap(string uniMap)
		{
			var uniCid = new CMapUniCid();
			return ParseCmap(uniMap, uniCid);
		}

		public static CMapByteCid GetByte2CidCmap(string cmap)
		{
			var uniCid = new CMapByteCid();
			return ParseCmap(cmap, uniCid);
		}

		public static CMapCidByte GetCid2Byte(string cmap)
		{
			var cidByte = new CMapCidByte();
			return ParseCmap(cmap, cidByte);
		}

		/// <summary>
		/// Clears the cache by removing fonts that were added via
		/// <see cref="SaveFont(FontProgram, System.String)"/>.
		/// </summary>
		/// <remarks>
		/// Clears the cache by removing fonts that were added via
		/// <see cref="SaveFont(FontProgram, System.String)"/>.
		/// <para />
		/// Be aware that in multithreading environment this method call will affect the result of
		/// <see cref="GetFont(System.String)"/>.
		/// This in its turn affects creation of fonts via factories when
		/// <c>cached</c>
		/// argument is set to true (which is by default).
		/// </remarks>
		public static void ClearSavedFonts()
		{
			fontCache.Clear();
		}

		public static FontProgram GetFont(string fontName)
		{
			return fontCache.Get(FontCacheKey.Create(fontName));
		}

		internal static FontProgram GetFont(FontCacheKey key)
		{
			return fontCache.Get(key);
		}

		public static FontProgram SaveFont(FontProgram font, string fontName)
		{
			return SaveFont(font, FontCacheKey.Create(fontName));
		}

		internal static FontProgram SaveFont(FontProgram font, FontCacheKey key)
		{
			var fontFound = fontCache.Get(key);
			if (fontFound != null)
			{
				return fontFound;
			}
			fontCache.Put(key, font);
			return font;
		}

		private static void LoadRegistry()
		{
			var resource = ResourceUtil.GetResourceStream(FontResources.CMAPS + CJK_REGISTRY_FILENAME);
			try
			{
				var p = new Properties();
				p.Load(resource);
				foreach (var entry in p)
				{
					var value = (string)entry.Value;
					var splitValue = StringUtil.Split(value, " ");
					var set = new HashSet<string>();
					foreach (var s in splitValue)
						if (s.Length != 0)
							set.Add(s);

					registryNames.Put((string)entry.Key, set);
				}
			}
			finally
			{
				resource?.Dispose();
			}
		}

		private static IDictionary<string, object> ReadFontProperties(string name)
		{
			var resource = ResourceUtil.GetResourceStream(FontResources.CMAPS + name + ".properties");
			try
			{
				var p = new Properties();
				p.Load(resource);
				IDictionary<string, object> fontProperties = new Dictionary<string, object>();
				foreach (var entry in p)
				{
					fontProperties.Put((string)entry.Key, entry.Value);
				}
				fontProperties.Put(W_PROP, CreateMetric((string)fontProperties.Get(W_PROP)));
				fontProperties.Put(W2_PROP, CreateMetric((string)fontProperties.Get(W2_PROP)));
				return fontProperties;
			}
			finally
			{
				resource?.Dispose();
			}
		}

		private static IntHashtable CreateMetric(string s)
		{
			var h = new IntHashtable();
			var tk = new StringTokenizer(s);
			while (tk.HasMoreTokens())
			{
				var n1 = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
				h.Put(n1, Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture));
			}
			return h;
		}

		private static T ParseCmap<T>(string name, T cmap)
			where T : AbstractCMap
		{
			try
			{
				CMapParser.ParseCid(name, cmap, new CMapLocationResource());
			}
			catch (System.IO.IOException e)
			{
				throw new IOException(IOException.IoException, e);
			}
			return cmap;
		}
	}
}
