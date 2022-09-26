﻿using PeNet;
using System.Collections.Generic;

namespace DLLirant.NET.Classes
{
    internal class PEAnalyser
    {
        public string SelectedBinaryPath;

        private PeFile peFile;

        public PEAnalyser(string path)
        {
            SelectedBinaryPath = path;
        }

        public List<string> GetPEInformations()
        {
            peFile = new PeFile(SelectedBinaryPath);
            List<string> peInformations = new List<string>();
            if (peFile.IsSigned)
            {
                peInformations.Add("Is signed: Yes");
            }
            else
            {
                peInformations.Add("Is signed: No");
            }

            if (peFile.HasValidSignature)
            {
                peInformations.Add("Is signature valid: Yes");
            }
            else
            {
                peInformations.Add("Is signature valid: No");
            }

            if (peFile.Is64Bit)
            {
                peInformations.Add("Architecture: x64");
            }
            else if (peFile.Is32Bit)
            {
                peInformations.Add("Architecture: x86");
            }
            else
            {
                peInformations.Add("Architecture: Unknown");
            }

            peInformations.Add($"MD5: {peFile.Md5}");
            peInformations.Add($"SHA1: {peFile.Sha1}");
            peInformations.Add($"SHA256: {peFile.Sha256}");
            return peInformations;
        }

        public List<string> GetModules(List<string> excludesList)
        {
            List<string> modules = new List<string>();
            foreach (PeNet.Header.Pe.ImportFunction func in peFile.ImportedFunctions)
            {
                bool isExcluded = false;
                foreach (string exclude in excludesList)
                {
                    if (func.DLL.ToLower().Contains(exclude.ToLower()))
                    {
                        isExcluded = true;
                        break;
                    }
                }
                if (!modules.Contains(func.DLL) && !isExcluded)
                {
                    modules.Add(func.DLL);
                }
            }
            return modules;
        }

        public List<string> GetImportedFunctions(string moduleName)
        {
            List<string> importedFunctions = new List<string>();
            foreach (PeNet.Header.Pe.ImportFunction func in peFile.ImportedFunctions)
            {
                if(func.DLL == moduleName && func.Name != null)
                {
                    importedFunctions.Add(func.Name);
                }
            }
            return importedFunctions;
        }
    }
}