#include <windows.h>
#include <stdio.h>

#pragma comment (lib, "User32.lib")

int Main() {
    FILE* fptr;
    fopen_s(&fptr, "C:\\DLLirant\\output.txt", "w");
    fprintf(fptr, "%s", "It works !\n");
    fclose(fptr);
    MessageBoxW(0, L"DLL Hijack found!", L"DLL Hijack", 0);
    return 1;
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        ##DLL_MAIN##
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

##EXPORTED_FUNCTIONS##
