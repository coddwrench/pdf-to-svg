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
using System.Globalization;
using System.IO;
using IText.IO.Util;
using IText.Logger;
using Versions.Attributes;

namespace IText.Kernel {
    /// <summary>This class contains version information about iText.</summary>
    /// <remarks>
    /// This class contains version information about iText.
    /// DO NOT CHANGE THE VERSION INFORMATION WITHOUT PERMISSION OF THE COPYRIGHT HOLDERS OF ITEXT.
    /// Changing the version makes it extremely difficult to debug an application.
    /// Also, the nature of open source software is that you honor the copyright of the original creators of the software.
    /// </remarks>
    public sealed class Version {
        /// <summary>Lock object used for synchronization</summary>
        private static readonly object staticLock = new object();

        /// <summary>String that will indicate if the AGPL version is used.</summary>
        private const string AGPL = " (AGPL-version)";

        /// <summary>The iText version instance.</summary>
        private static volatile Version version;

        /// <summary>This String contains the name of the product.</summary>
        /// <remarks>
        /// This String contains the name of the product.
        /// iText is a registered trademark by iText Group NV.
        /// Please don't change this constant.
        /// </remarks>
        private const string iTextProductName = "iText\u00ae";

        /// <summary>This String contains the version number of this iText release.</summary>
        /// <remarks>
        /// This String contains the version number of this iText release.
        /// For debugging purposes, we request you NOT to change this constant.
        /// </remarks>
        private const string release = "7.1.16-SNAPSHOT";

        /// <summary>This String contains the iText version as shown in the producer line.</summary>
        /// <remarks>
        /// This String contains the iText version as shown in the producer line.
        /// iText is a product developed by iText Group NV.
        /// iText Group requests that you retain the iText producer line
        /// in every PDF that is created or manipulated using iText.
        /// </remarks>
        private const string producerLine = iTextProductName + " " + release + " \u00a92000-2021 iText Group NV";

        /// <summary>The version info;</summary>
        private readonly VersionInfo info;

        private bool expired;

        [Obsolete(@"Use GetInstance() instead. Will be removed in next major release.")]
        public Version() {
            info = new VersionInfo(iTextProductName, release, producerLine, null);
        }

        internal Version(VersionInfo info, bool expired) {
            this.info = info;
            this.expired = expired;
        }

        /// <summary>Gets an instance of the iText version that is currently used.</summary>
        /// <remarks>
        /// Gets an instance of the iText version that is currently used.
        /// <para />
        /// Note that iText Group requests that you retain the iText producer line
        /// in every PDF that is created or manipulated using iText.
        /// </remarks>
        /// <returns>
        /// an instance of
        /// <see cref="Version"/>.
        /// </returns>
        public static Version GetInstance() {
            lock (staticLock) {
                if (version != null) {
                    try {
                        LicenseScheduledCheck();
                    }
                    catch (Exception e) {
                        // If any exception occurs during scheduled check of core license,
                        // then it means that license is not valid yet, so roll back to AGPL.
                        // The key value is null as it is similar to case
                        // when an exception has been thrown during initial license loading
                        AtomicSetVersion(InitAGPLVersion(e, null));
                    }
                    return version;
                }
            }
            Version localVersion;
            string key = null;
            try {
                var coreVersion = release;
                var info = GetLicenseeInfoFromLicenseKey(coreVersion);
                if (info != null) {
                    if (info[3] != null && info[3].Trim().Length > 0) {
                        key = info[3];
                    }
                    else {
                        key = "Trial version ";
                        if (info[5] == null) {
                            key += "unauthorised";
                        }
                        else {
                            key += info[5];
                        }
                    }
                    if (info.Length > 6) {
                        if (info[6] != null && info[6].Trim().Length > 0) {
                            //Compare versions with this release versions
                            CheckLicenseVersion(coreVersion, info[6]);
                        }
                    }
                    if (info[4] != null && info[4].Trim().Length > 0) {
                        localVersion = InitVersion(info[4], key, false);
                    }
                    else {
                        if (info[2] != null && info[2].Trim().Length > 0) {
                            localVersion = InitDefaultLicensedVersion(info[2], key);
                        }
                        else {
                            if (info[0] != null && info[0].Trim().Length > 0) {
                                // fall back to contact name, if company name is unavailable.
                                // we shouldn't have a licensed version without company name,
                                // but let's account for it anyway
                                localVersion = InitDefaultLicensedVersion(info[0], key);
                            }
                            else {
                                localVersion = InitAGPLVersion(null, key);
                            }
                        }
                    }
                }
                else {
                    localVersion = InitAGPLVersion(null, key);
                }
            }
            catch (LicenseVersionException lve) {
                //Catch the exception
                //Rethrow license version exceptions
                throw;
            }
            catch (TypeLoadException) {
                //License key library not on classpath, switch to AGPL
                localVersion = InitAGPLVersion(null, key);
            }
            catch (Exception e) {
                //Check if an iText5 license is loaded
                if (e.InnerException != null && e.InnerException.Message.Equals(LicenseVersionException.LICENSE_FILE_NOT_LOADED
                    )) {
                    if (IsiText5licenseLoaded()) {
                        throw new LicenseVersionException(LicenseVersionException.NO_I_TEXT7_LICENSE_IS_LOADED_BUT_AN_I_TEXT5_LICENSE_IS_LOADED
                            );
                    }
                }
                localVersion = InitAGPLVersion(e.InnerException, key);
            }
            return AtomicSetVersion(localVersion);
        }

        /// <summary>Checks if the AGPL version is used.</summary>
        /// <returns>returns true if the AGPL version is used.</returns>
        public static bool IsAGPLVersion() {
            return GetInstance().IsAGPL();
        }

        /// <summary>Is the license expired?</summary>
        /// <returns>true if expired</returns>
        public static bool IsExpired() {
            return GetInstance().expired;
        }

        /// <summary>Gets the product name.</summary>
        /// <remarks>
        /// Gets the product name.
        /// iText Group NV requests that you retain the iText producer line
        /// in every PDF that is created or manipulated using iText.
        /// </remarks>
        /// <returns>the product name</returns>
        public string GetProduct() {
            return info.GetProduct();
        }

        /// <summary>Gets the release number.</summary>
        /// <remarks>
        /// Gets the release number.
        /// iText Group NV requests that you retain the iText producer line
        /// in every PDF that is created or manipulated using iText.
        /// </remarks>
        /// <returns>the release number</returns>
        public string GetRelease() {
            return info.GetRelease();
        }

        /// <summary>Returns the iText version as shown in the producer line.</summary>
        /// <remarks>
        /// Returns the iText version as shown in the producer line.
        /// iText is a product developed by iText Group NV.
        /// iText Group requests that you retain the iText producer line
        /// in every PDF that is created or manipulated using iText.
        /// </remarks>
        /// <returns>iText version</returns>
        public string GetVersion() {
            return info.GetVersion();
        }

        /// <summary>Returns a license key if one was provided, or null if not.</summary>
        /// <returns>a license key.</returns>
        public string GetKey() {
            return info.GetKey();
        }

        /// <summary>Returns a version info in one class</summary>
        /// <returns>a version info.</returns>
        public VersionInfo GetInfo() {
            return info;
        }

        internal static string[] ParseVersionString(string version) {
            var splitRegex = "\\.";
            var split = StringUtil.Split(version, splitRegex);
            //Guard for empty versions and throw exceptions
            if (split.Length == 0) {
                throw new LicenseVersionException(LicenseVersionException.VERSION_STRING_IS_EMPTY_AND_CANNOT_BE_PARSED);
            }
            //Desired Format: X.Y.Z-....
            //Also catch X, X.Y-...
            var major = split[0];
            //If no minor version is present, default to 0
            var minor = "0";
            if (split.Length > 1) {
                minor = split[1].Substring(0);
            }
            //Check if both values are numbers
            if (!IsVersionNumeric(major)) {
                throw new LicenseVersionException(LicenseVersionException.MAJOR_VERSION_IS_NOT_NUMERIC);
            }
            if (!IsVersionNumeric(minor)) {
                throw new LicenseVersionException(LicenseVersionException.MINOR_VERSION_IS_NOT_NUMERIC);
            }
            return new[] { major, minor };
        }

        internal static bool IsVersionNumeric(string version) {
            try {
                var value = Convert.ToInt32(version, CultureInfo.InvariantCulture);
                // parseInt accepts numbers which start with a plus sign, but for a version it's unacceptable
                return value >= 0 && !version.Contains("+");
            }
            catch (FormatException) {
                return false;
            }
        }

        /// <summary>Checks if the current object has been initialized with AGPL license.</summary>
        /// <returns>returns true if the current object has been initialized with AGPL license.</returns>
        internal bool IsAGPL() {
            return GetVersion().IndexOf(AGPL, StringComparison.Ordinal) > 0;
        }

        private static Version InitDefaultLicensedVersion(string ownerName, string key) {
            var producer = producerLine + " (" + ownerName;
            if (!key.ToLowerInvariant().StartsWith("trial")) {
                producer += "; licensed version)";
            }
            else {
                producer += "; " + key + ")";
            }
            return InitVersion(producer, key, false);
        }

        private static Version InitAGPLVersion(Exception cause, string key) {
            var producer = producerLine + AGPL;
            var expired = cause != null && cause.Message != null && cause.Message.Contains("expired");
            return InitVersion(producer, key, expired);
        }

        private static Version InitVersion(string producer, string key, bool expired) {
            return new Version(new VersionInfo(iTextProductName, release, producer, key), expired);
        }

        private static Type GetLicenseKeyClass() {
            var licenseKeyClassFullName = "iText.License.LicenseKey, itext.licensekey";
            return GetClassFromLicenseKey(licenseKeyClassFullName);
        }

        private static Type GetClassFromLicenseKey(string classPartialName) {
            string classFullName = null;

            var kernelAssembly = typeof(Version).GetAssembly();
            var keyVersionAttr = kernelAssembly.GetCustomAttribute(typeof(KeyVersionAttribute));
            if (keyVersionAttr is KeyVersionAttribute) {
                var keyVersion = ((KeyVersionAttribute)keyVersionAttr).KeyVersion;
                var format = "{0}, Version={1}, Culture=neutral, PublicKeyToken=8354ae6d2174ddca";
                classFullName = string.Format(format, classPartialName, keyVersion);
            }

            Type type = null;
            if (classFullName != null) {
                string fileLoadExceptionMessage = null;
                try {
                    type = Type.GetType(classFullName);
                } catch (FileLoadException fileLoadException) {
                    fileLoadExceptionMessage = fileLoadException.Message;
                }

                if (type == null) {
                    var logger = LogManager.GetLogger(typeof(Version));
                    try {
                        type = Type.GetType(classPartialName);
                    } catch {
                        // ignore
                    }
                    if (type == null && fileLoadExceptionMessage != null) {
                        logger.Error(fileLoadExceptionMessage);
                    }
                }
            }
            return type;
        }

        private static void CheckLicenseVersion(string coreVersionString, string licenseVersionString) {
            var coreVersions = ParseVersionString(coreVersionString);
            var licenseVersions = ParseVersionString(licenseVersionString);
            var coreMajor = Convert.ToInt32(coreVersions[0], CultureInfo.InvariantCulture);
            var coreMinor = Convert.ToInt32(coreVersions[1], CultureInfo.InvariantCulture);
            var licenseMajor = Convert.ToInt32(licenseVersions[0], CultureInfo.InvariantCulture);
            var licenseMinor = Convert.ToInt32(licenseVersions[1], CultureInfo.InvariantCulture);
            //Major version check
            if (licenseMajor < coreMajor) {
                throw new LicenseVersionException(LicenseVersionException.THE_MAJOR_VERSION_OF_THE_LICENSE_0_IS_LOWER_THAN_THE_MAJOR_VERSION_1_OF_THE_CORE_LIBRARY
                    ).SetMessageParams(licenseMajor, coreMajor);
            }
            if (licenseMajor > coreMajor) {
                throw new LicenseVersionException(LicenseVersionException.THE_MAJOR_VERSION_OF_THE_LICENSE_0_IS_HIGHER_THAN_THE_MAJOR_VERSION_1_OF_THE_CORE_LIBRARY
                    ).SetMessageParams(licenseMajor, coreMajor);
            }
            //Minor version check
            if (licenseMinor < coreMinor) {
                throw new LicenseVersionException(LicenseVersionException.THE_MINOR_VERSION_OF_THE_LICENSE_0_IS_LOWER_THAN_THE_MINOR_VERSION_1_OF_THE_CORE_LIBRARY
                    ).SetMessageParams(licenseMinor, coreMinor);
            }
        }

        private static string[] GetLicenseeInfoFromLicenseKey(string validatorKey) {
            var licenseeInfoMethodName = "GetLicenseeInfoForVersion";
            var klass = GetLicenseKeyClass();
            if (klass != null) {
                Type[] cArg = { typeof(string) };
                var m = klass.GetMethod(licenseeInfoMethodName, cArg);
                object[] args = { validatorKey };
                var info = (string[])m.Invoke(Activator.CreateInstance(klass), args);
                return info;
            }
            return null;
        }

        private static bool IsiText5licenseLoaded() {
            var validatorKey5 = "5";
            var result = false;
            try {
                var info = GetLicenseeInfoFromLicenseKey(validatorKey5);
                result = true;
            }
            catch (Exception) {
            }
            return result;
        }

        private static Version AtomicSetVersion(Version newVersion) {
            lock (staticLock) {
                version = newVersion;
                return version;
            }
        }

        private static void LicenseScheduledCheck() {
            if (version.IsAGPL()) {
                return;
            }
            var licenseKeyProductFullName = "iText.License.LicenseKeyProduct, itext.licensekey";
            var checkLicenseKeyMethodName = "ScheduledCheck";
            try {
                var licenseKeyClass = GetLicenseKeyClass();
                var licenseKeyProductClass = GetClassFromLicenseKey(licenseKeyProductFullName);
                Type[] cArg = { licenseKeyProductClass };
                var method = licenseKeyClass.GetMethod(checkLicenseKeyMethodName, cArg);
                method.Invoke(null, new object[] { null });
            }
            catch (Exception e) {
                throw new Exception(e.Message, e);
            }
        }
    }
}
