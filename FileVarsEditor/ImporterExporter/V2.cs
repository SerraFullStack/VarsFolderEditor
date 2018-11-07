using JsonMaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileVarsEditor.ImporterExporter
{
    class V2: IImporterExporter
    {
        private int remainThreads = 0;
        int totalFilesWorked = 0;

        JSON jm = new JSON();
        string workingPath;
        public bool export(string dbPath, string file, ImporterExporter.OnProgress onProgress)
        {
            workingPath = dbPath.Replace("/", "\\");
            if (workingPath[workingPath.Length - 1] == '\\')
                workingPath = workingPath.Substring(0, workingPath.Length - 1);

            importFolder(dbPath);

            if (!file.ToLower().EndsWith(".json"))
                file += ".json";

            File.WriteAllText(file, jm.ToJson());

            return true;
        }

        private void importFolder(string dbPath)
        {
            //get files from path
            var files = Directory.GetFiles(dbPath);


            //get the name of dbPath
            string currPath = dbPath.Substring(workingPath.Length);
            if ((currPath != "") && (currPath[0] == '\\'))
            {
                currPath = currPath.Substring(1);

            }
            foreach (var c in files)
            {
                //removes the workingpath and the '/' from the current file
                string curr = c.Substring(workingPath.Length+1).Replace("\\", ".").Replace("/", ".");

                //calculate the json name of the currFile
                string jsoName = curr.Replace("\\", ".");
                    
                //read the file
                string fileData = fileToHex(c);

                //save the data to json
                jm.setString(curr + ".originalPath", currPath);
                jm.setString(curr + ".data", fileData);
            }

            //add folders inside
            var folders = Directory.GetDirectories(dbPath);
            foreach (var c in folders)
                importFolder(c);
        }


        public bool import(string file, string dbPath, ImporterExporter.OnProgress onProgress)
        {
            jm.clear();
            jm.parseJson(File.ReadAllText(file));
            return importJson("", dbPath);
            

        }

        private bool importJson(string parentName, string dbPath)
        {
            if (jm.contains(parentName + ".originalPath") && (jm.contains(parentName + ".data")))
            {
                string originalPath = jm.getString(parentName + ".originalPath");
                string data = jm.getString(parentName + ".data");

                string fileName = dbPath.Replace("\\", "/") + "/" + parentName.Replace(".", "/");

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                hexToFile(fileName, data);
            }
            else
            {
                var childs = jm.getChildsNames(parentName);

                string tempParent = parentName != "" ? parentName + "." : "";
                foreach (var c in childs)
                {
                    importJson(tempParent + c, dbPath);
                }
            }

            return true;
        }
        

        private string fileToHex(string filename)
        {
            if (File.Exists(filename))
            {
                byte[] bytes = File.ReadAllBytes(filename);
                StringBuilder outS = new StringBuilder();
                foreach (var att in bytes)
                    outS.Append(att.ToString("X2"));

                return outS.ToString();
            }
            else
                return "";
        }

        private void hexToFile(string file, string hex)
        {
            byte[] buffer = new byte[(int)(hex.Length / 2)];
            int bufferAtt = 0;
            for (int cont = 0; cont < hex.Length; cont = cont + 2)
            {
                buffer[bufferAtt++] = byte.Parse(hex.Substring(cont, 2), System.Globalization.NumberStyles.HexNumber);
            }
            File.WriteAllBytes(file, buffer);
        }
    }
}
