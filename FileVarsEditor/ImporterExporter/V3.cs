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
    class V3 : IImporterExporter
    {
        private int remainThreads = 0;
        int totalFilesWorked = 0;

        JSON jm = new JSON();
        string workingPath;

        int exportMax = 0;
        int exportCount = 0;
        public bool export(string dbPath, string file, ImporterExporter.OnProgress onProgress)
        {
            workingPath = dbPath.Replace("/", "\\");
            if (workingPath[workingPath.Length - 1] == '\\')
                workingPath = workingPath.Substring(0, workingPath.Length - 1);

            exportMax = calculateFilesToImport(dbPath);
            exportCount = 0;
            importFolder(dbPath, onProgress);

            if (!file.ToLower().EndsWith(".json"))
                file += ".json";

            File.WriteAllText(file, jm.ToJson());

            return true;
        }


        private int calculateFilesToImport(string dbPath)
        {
            //get files from path
            var files = Directory.GetFiles(dbPath);
            int currCount = files.Length;

            //add folders inside
            var folders = Directory.GetDirectories(dbPath);
            foreach (var c in folders)
                currCount += calculateFilesToImport(c);

            return currCount;
        }

        private void importFolder(string dbPath, ImporterExporter.OnProgress onProgress)
        {
            //get files from path
            var files = Directory.GetFiles(dbPath);

            //add folders inside
            var folders = Directory.GetDirectories(dbPath);
            foreach (var c in folders)
                importFolder(c, onProgress);


            //get the name of dbPath
            string currPath = dbPath.Substring(workingPath.Length);
            if ((currPath != "") && (currPath[0] == '\\'))
            {
                currPath = currPath.Substring(1);
            }


            foreach (var c in files)
            {
                //removes the workingpath and the '/' from the current file
                string curr = c.Substring(workingPath.Length + 1).Replace("\\", ".").Replace("/", ".");

                curr = prepareJsonName(curr);
                //read the file
                string fileData = fileToHex(c);

                //save the data to json
                //jm.setString(curr + ".originalPath", currPath);
                //jm.setString(curr + ".data", fileData);
                
                



                jm.setString(curr, fileData);

                exportCount++;
                onProgress(exportMax, exportCount);
            }

            

            
        }

        private string prepareJsonName(string originalName)
        {
            var t = originalName.Split('.');
            string result = "";

            for (int c = 0; c < t.Length; c++) 
            {
                var curr = t[c];
                var onlyNumbers = new String(curr.Where(currChar => "0123456789".Contains(currChar)).ToArray());

                if (onlyNumbers.Length > 0)
                    curr = curr.Replace(onlyNumbers, "_nmbr_" + onlyNumbers);

                result += curr;
                if (c < t.Length - 1)
                    result += '.';
            }

            return result;
        }


        public bool import(string file, string dbPath, ImporterExporter.OnProgress onProgress)
        {
            jm.clear();
            jm.parseJson(File.ReadAllText(file));


            var allKeys = jm.getObjectsNames("");
            int max = allKeys.Count;
            int count = 0;
            foreach (var curr in allKeys)
            {
                //checks if curr contains childs or no
                if (jm.getChildsNames(curr).Count == 0)
                {
                    string fileName = dbPath.Replace('\\', '/') + '/' + curr.Replace('.', '/');
                    fileName = fileName.Replace("_nmbr_", "");
                    string data = jm.getString(curr);
                    string directory = Path.GetDirectoryName(fileName);

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(fileName, data);
                }

                count++;
                onProgress(max, count);
            }

            return true;


        }


        private string fileToHex(string filename)
        {
            return File.ReadAllText(filename);
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
            File.WriteAllText(file, hex);
            return;
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
