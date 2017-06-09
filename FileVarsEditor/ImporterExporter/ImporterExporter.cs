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
    class ImporterExporter
    {
        private int remainThreads = 0;
        int totalFilesWorked = 0;

        public delegate void OnProgress(int max, int att);
    

        public bool export(string dbPath, string file, OnProgress onProgress)
        {
            
            dynamic export = new ExpandoObject();

            string[] files = Directory.GetFiles(dbPath);
            List<KeyValuePair<string, string>> filesData = new List<KeyValuePair<string, string>>();

            int perThread = (int)(Math.Round((double)files.Length / 4.0));
            if (perThread == 0)
                perThread = 1;

            remainThreads = 4;
            totalFilesWorked = 0;


            dynamic t1D = new ExpandoObject();
            t1D.dic = filesData;
            t1D.files = files;
            t1D.start = perThread * 0;
            t1D.end = t1D.start + perThread;
            Thread t1 = new Thread(importFilesThread);
            t1.Start(t1D);

            dynamic t2D = new ExpandoObject();
            t2D.dic = filesData;
            t2D.files = files;
            t2D.start = perThread * 1;
            t2D.end = t2D.start + perThread;
            Thread t2 = new Thread(importFilesThread);
            t2.Start(t2D);

            dynamic t3D = new ExpandoObject();
            t3D.dic = filesData;
            t3D.files = files;
            t3D.start = perThread * 2;
            t3D.end = t3D.start + perThread;
            Thread t3 = new Thread(importFilesThread);
            t3.Start(t3D);

            dynamic t4D = new ExpandoObject();
            t4D.dic = filesData;
            t4D.files = files;
            t4D.start = perThread * 3;
            t4D.end = t4D.start + perThread;
            Thread t4 = new Thread(importFilesThread);
            t4.Start(t4D);

            while (remainThreads > 0)
            {
                System.Threading.Thread.Sleep(10);
                onProgress(files.Length, totalFilesWorked);
            }
            onProgress(files.Length, files.Length);

            export.files = filesData;

            try
            {
                File.WriteAllText(file, JsonConvert.SerializeObject(export));
                return true;
            }
            catch { return false; }
            
        }


        public bool import(string file, string dbPath, OnProgress onProgress)
        {
            dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(file));

            List< Object> files = data.files;

           // try
            {
                for (int cont = 0; cont < files.Count; cont++)
                {
                    dynamic att = files[cont];

                    this.hexToFile(dbPath + "\\" + att.Key.Replace("_sep_", "."), att.Value);
                    if (cont % 10 == 0)
                        onProgress(files.Count, cont);
                }
                onProgress(files.Count, files.Count);

                return true;
            }
            //catch { return false; }

        }
        private void importFilesThread(object parameters)
        {
            dynamic lol = parameters;
            KeyValuePair<string, string> temp;
            for (int cont = lol.start; (cont < lol.end) && (cont < lol.files.Length); cont++)
            {
                temp = new KeyValuePair<string, string>(Path.GetFileName(lol.files[cont]).Replace(".", "_sep_"), this.fileToHex(lol.files[cont]));

                lol.dic.Add(temp);
            }

            remainThreads--;
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
