using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace DLLirant.Classes
{
    internal class CodeGenerator
    {
        public enum TypeDLLHijacking
        {
            DLLSearchOrderHijacking,
            OrdinalBased
        }

        public static string GenerateDLL(string dllmain, List<string> functions = null, TypeDLLHijacking typeDLLHijacking = TypeDLLHijacking.DLLSearchOrderHijacking)
        {
            string CppCode = string.Empty;
            switch (typeDLLHijacking)
            {
                case TypeDLLHijacking.DLLSearchOrderHijacking:
                    CppCode =
                        "#include <windows.h>\r\n" +
                        "#include <stdio.h>\r\n\r\n" +

                        "#pragma comment (lib, \"User32.lib\")\r\n\r\n" +
                        "int Main() {\r\n" +
                            "\tFILE* fptr;\r\n" +
                            "\tfopen_s(&fptr, \"C:\\\\DLLirant\\\\output.txt\", \"w\");\r\n" +
                            "\tfprintf(fptr, \"%s\", \"It works !\");\r\n" +
                            "\tfclose(fptr);\r\n" +
                            "\treturn 1;\r\n" +
                        "}\r\n\r\n" +
                        "BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)\r\n" +
                        "{\r\n" +
                            "\tswitch (ul_reason_for_call) {\r\n" +
                                "\t\tcase DLL_PROCESS_ATTACH:\r\n" +
                                    "\t\t\t" + dllmain + "\r\n" +
                                    "\t\t\tbreak;\r\n" +
                                "\t\tcase DLL_THREAD_ATTACH:\r\n" +
                                "\t\tcase DLL_THREAD_DETACH:\r\n" +
                                "\t\tcase DLL_PROCESS_DETACH:\r\n" +
                                    "\t\t\tbreak;\r\n" +
                            "\t}\r\n" +
                            "\treturn TRUE;\r\n" +
                            "}\r\n\r\n";
                    break;
                case TypeDLLHijacking.OrdinalBased:
                    CppCode =
                       "#include <windows.h>\r\n" +
                       "#include <string>\r\n" +

                       "#pragma comment (lib, \"User32.lib\")\r\n\r\n" +
                       "int Main(int nb) {\r\n" +
                           "\tstd::wstring message = std::to_wstring(nb);\r\n" +
                           "\tMessageBoxW(0, message.data(), L\"DLL Hijack\", 0);\r\n" +
                       "\treturn 1;\r\n" +
                       "}\r\n\r\n" +
                       "BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)\r\n" +
                       "{\r\n" +
                           "\tswitch (ul_reason_for_call) {\r\n" +
                               "\t\tcase DLL_PROCESS_ATTACH:\r\n" +
                                   "\t\t\t" + dllmain + "\r\n" +
                                   "\t\t\tbreak;\r\n" +
                               "\t\tcase DLL_THREAD_ATTACH:\r\n" +
                               "\t\tcase DLL_THREAD_DETACH:\r\n" +
                               "\t\tcase DLL_PROCESS_DETACH:\r\n" +
                                   "\t\t\tbreak;\r\n" +
                           "\t}\r\n" +
                           "\treturn TRUE;\r\n" +
                           "}\r\n\r\n";
                    break;
                default:
                    break;
            }

            if (functions != null) { CppCode += string.Join("\n", functions.ToArray()); };

            using (StreamWriter writer = new StreamWriter("output/dllmain.cpp"))
            {
                writer.WriteLine(CppCode);
            }

            return CppCode;
        }
    }
}
