namespace FileVarsEditor.ImporterExporter
{
    interface IImporterExporter
    {
        bool export(string dbPath, string file, ImporterExporter.OnProgress onProgress);
        bool import(string file, string dbPath, ImporterExporter.OnProgress onProgress);
    }
}