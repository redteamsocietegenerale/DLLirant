using PeNet;
using System.Collections.Generic;

namespace DLLirant.NET.Classes
{
    internal class PEAnalyzer
    {
        public string SelectedBinaryPath;

        private readonly PeFile peFile;

        public PEAnalyzer(string path)
        {
            SelectedBinaryPath = path;
            peFile = new PeFile(SelectedBinaryPath);
        }

        public List<string> GetPEInformations()
        {
            List<string> peInformations = new List<string>();

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

        public string CheckIfSigned()
        {
            if (peFile.IsSigned)
            {
                return "Is signed: Yes";
            }
            else
            {
                return "Is signed: No";
            }
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
                if (func.DLL == moduleName && func.Name != null && !func.Name.StartsWith("?"))
                {
                    importedFunctions.Add(func.Name);
                }
            }
            return importedFunctions;
        }

        public string GetMD5()
        {
            return peFile.Md5;
        }

        public string GetSHA1()
        {
            return peFile.Sha1;
        }

        public string GetSHA256()
        {
            return peFile.Sha256;
        }
    }
}
