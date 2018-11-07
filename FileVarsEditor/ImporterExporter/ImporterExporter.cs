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
    class ImporterExporter : IImporterExporter
    {
        private int remainThreads = 0;
        int totalFilesWorked = 0;

        public delegate void OnProgress(int max, int att);
    

        public bool export(string dbPath, string file, OnProgress onProgress)
        {
            IImporterExporter exporter = new V3();

            return exporter.export(dbPath, file, onProgress);
        }


        public bool import(string file, string dbPath, OnProgress onProgress)
        {
            var f = new System.IO.StreamReader(file);
            string firstLine = f.ReadLine();
            f.Close();

            IImporterExporter importer;
            //if (firstLine.Contains("V2"))
                importer = new V3();
            //else
            //    importer = new V1();

            return importer.import(file, dbPath, onProgress);
        }
    }
}
