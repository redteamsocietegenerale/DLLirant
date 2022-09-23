using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace DLLirant.NET.Classes
{
    internal class FileOperations
    {
        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                try
                {
                    Directory.Delete(path, true);
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { MessageBox.Show("ERROR: The output directory is used by another process"); }
        }

        public void CopyFile(string file)
        {
            if (!File.Exists($"output/{Path.GetFileName(file)}"))
                File.Copy(file, $"output/{Path.GetFileName(file)}");
        }

        public void RenameFile(string path, string newpath)
        {
            if (File.Exists(path) && !File.Exists(newpath))
                File.Move(path, newpath);
        }

        public void CopyFilesDirToDir(string dllname, string sourceDir, string targetDir)
        {
            if (Directory.Exists(sourceDir))
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    if (Path.GetFileName(file) != dllname)
                        File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
                }
        }

        public void RecreateOutputDirectories(List<string> directories)
        {
            foreach (string dir in directories)
            {
                DeleteDirectory(dir);
                CreateDirectory(dir);
            }
        }
    }
}
