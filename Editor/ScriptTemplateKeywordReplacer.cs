using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnyVR.Editor
{
    /// <summary>
    ///     Decorates each new code file with a GPL license header containing basic
    ///     information about the project.
    /// </summary>
    public class ScriptTemplateKeywordReplacer : AssetModificationProcessor
    {
        private static readonly string s_licenseHeader =
            "// #APPNAME# is a multiuser, multiplatform XR framework.\r\n" +
            "// Copyright (C) #CURRENT-YEAR# #AUTHORS#.\r\n" +
            "// \r\n" +
            "// #APPNAME# is free software: you can redistribute it and/or modify\r\n" +
            "// it under the terms of the GNU General Public License as published\r\n" +
            "// by the Free Software Foundation, either version 3 of the License,\r\n" +
            "// or (at your option) any later version.\r\n" +
            "// \r\n" +
            "// #APPNAME# is distributed in the hope that it will be useful, but\r\n" +
            "// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-\r\n" +
            "// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.\r\n" +
            "// See the GNU General Public License for more details.\r\n" +
            "// \r\n" +
            "// You should have received a copy of the GNU General Public License\r\n" +
            "// along with #APPNAME#.\r\n" +
            "// If not, see <https://www.gnu.org/licenses/>.";

        public static void OnWillCreateAsset(string path)
        {
            path = path.Replace(".meta", "");
            int index = path.LastIndexOf(".");
            if (index < 0)
            {
                return;
            }

            string file = path.Substring(index);
            if (file != ".cs" && file != ".js" && file != ".boo")
            {
                return;
            }

            index = Application.dataPath.LastIndexOf("Assets");
            path = Application.dataPath.Substring(0, index) + path;
            if (!File.Exists(path))
            {
                return;
            }

            string fileContent = File.ReadAllText(path);
            fileContent = fileContent.Replace("#LICENSE-HEADER#", s_licenseHeader);
            fileContent = fileContent.Replace("#APPNAME#", Application.productName);
            fileContent = fileContent.Replace("#AUTHORS#", Application.companyName);
            fileContent = fileContent.Replace("#CURRENT-YEAR#", DateTime.Now.Year.ToString());

            File.WriteAllText(path, fileContent);
            AssetDatabase.Refresh();
        }
    }
}