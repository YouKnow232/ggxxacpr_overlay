// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
using namespace std;

typedef int (CORECLR_DELEGATE_CALLTYPE* component_entry_point_fn)(void*, int);

namespace {
    HMODULE thisModule;
    component_entry_point_fn overlay_unload_function_pointer;
}

// TODO: Need to package hostfxr.dll, hostpolicy.dll, and coreclr.dll with release.

static const char *_debug_prefix = "[Bootstrapper] ";
static void DebugLog(const char* fmt, ...)
{
    char buffer[1024];
    va_list args;
    va_start(args, fmt);
    vsnprintf(buffer, sizeof(buffer), fmt, args);
    va_end(args);
    OutputDebugStringA(buffer);
    string s = buffer;
    cout << _debug_prefix << buffer << endl;
}

extern "C" __declspec(dllexport)
DWORD WINAPI Eject(LPVOID lparam) {
    if (thisModule == nullptr || overlay_unload_function_pointer == nullptr) {
        DebugLog("Eject called when not properly initialized: module: 0x%1p, unloadPtr: 0x%2p", thisModule, overlay_unload_function_pointer);
        return 1;
    }

    // Detach overlay
    DebugLog("Ejecting overlay...");
    int retCode = overlay_unload_function_pointer(nullptr, 0);
    DebugLog("Overlay detached %1", retCode);

    // Detach self
    DebugLog("Ejecting self...");
    FreeLibraryAndExitThread(thisModule, 0);
}

static DWORD WINAPI HostDotnetRuntime(LPVOID lpParam)
{
    DebugLog("StartCoreCLR thread start.\n");

    // [DEBUG] Hostfxr debug logging
    //SetEnvironmentVariable(L"DOTNET_STARTUP_HOOKS", L"");
    //SetEnvironmentVariable(L"DOTNET_EnableDiagnostics", L"1");
    //SetEnvironmentVariable(L"COREHOST_TRACE", L"1");
    //SetEnvironmentVariable(L"COREHOST_TRACE_VERBOSITY", L"4");

    // TODO: package these somehow
    //const char* basePath = "C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\Release\\net9.0-windows\\win-x86\\publish\\framework-dependent\\";
    //const char_t* runtimeConfigPath = L"C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\Release\\net9.0-windows\\win-x86\\publish\\framework-dependent\\GGXXACPROverlay.runtimeconfig.json";
    //const char_t* overlayPath = L"C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\Release\\net9.0-windows\\win-x86\\publish\\framework-dependent\\GGXXACPROverlay.dll";
    //const char_t* runtimeConfigPath = L"C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\x86\\Release\\net9.0-windows\\win-x86\\GGXXACPROverlay.runtimeconfig.json";
    //const char_t* overlayPath = L"C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\x86\\Release\\net9.0-windows\\win-x86\\GGXXACPROverlay.dll";
    const char_t* runtimeConfigPath = L".\\GGXXACPROverlay\\GGXXACPROverlay.runtimeconfig.json";
    const char_t* overlayPath = L".\\GGXXACPROverlay\\GGXXACPROverlay.dll";

    // auto hostfxrInstallPath = "C:\\Program Files (x86)\\dotnet\\host\\fxr\\9.0.1\\hostfxr.dll";

    char_t hostfxrPath[512];
    size_t hostfxrBufferSize = sizeof(hostfxrPath) / sizeof(char_t);

    int rc = get_hostfxr_path(hostfxrPath, &hostfxrBufferSize, nullptr);
    if (rc == 0) {
        DebugLog("hostfxr.dll path: %1", hostfxrPath);
    }
    else {
        DebugLog("Failed to get hostfxr path");
        return 1;
    }

    HMODULE hHostfxr = LoadLibraryW(hostfxrPath);
    if (!hHostfxr) {
        DWORD err = GetLastError();
        DebugLog("ERROR: LoadLibraryA failed. GLE=%1lu\n", err);
        MessageBoxA(NULL, "Failed to load hostfxr.dll!", "GGXXACPROverlay", MB_OK | MB_ICONERROR);
        return 1;
    }

    // Define hostfxr function pointers
    auto hostfxr_initialize_for_runtime_config =
        (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(hHostfxr, "hostfxr_initialize_for_runtime_config");
    if (hostfxr_initialize_for_runtime_config == nullptr) {
        DebugLog("Failed to load hostfxr_initialize_for_runtime_config");
        return 2;
    }
    auto hostfxr_get_runtime_delegate =
        (hostfxr_get_runtime_delegate_fn)GetProcAddress(hHostfxr, "hostfxr_get_runtime_delegate");
    if (hostfxr_get_runtime_delegate == nullptr) {
        DebugLog("Failed to load hostfxr_get_runtime_delegate");
        return 3;
    }
    auto hostfxr_close =
        (hostfxr_close_fn)GetProcAddress(hHostfxr, "hostfxr_close");
    if (hostfxr_close == nullptr) {
        DebugLog("Failed to load hostfxr_close");
        return 4;
    }


    // Initialize dotnet runtime
    hostfxr_handle handle = nullptr;
    int retCode = hostfxr_initialize_for_runtime_config(runtimeConfigPath, nullptr, &handle);
    //int retCode = hostfxr_initialize_for_runtime_config(testConfigPath, nullptr, &handle);
    if (retCode != 0 || handle == nullptr) {
        DebugLog("Failed to initialize runtime: %1", retCode);
        return 5;
    }

    // Get load assembly delegate function
    load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
    retCode = hostfxr_get_runtime_delegate(handle, hdt_load_assembly_and_get_function_pointer, (void**)&load_assembly_and_get_function_pointer);
    if (retCode != 0 || load_assembly_and_get_function_pointer == nullptr) {
        DebugLog("Failed to get delegate 0x%p", retCode);
        hostfxr_close(handle);
        return 6;
    }

    DebugLog("Calling load_assembly_and_get_function_pointer ...", retCode);

    // Defining Entry point
    component_entry_point_fn Main = nullptr;

    //load_assembly_and_get_function_pointer_fn load_fn = (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    retCode = load_assembly_and_get_function_pointer(
            overlayPath,                // Path to managed assembly
            L"GGXXACPROverlay.Program, GGXXACPROverlay",// "[Namespace].[Type], [Assembly]"
            L"Main",                    // Method name
            UNMANAGEDCALLERSONLY_METHOD,// Entry point type
            nullptr,                    // Reserved
            (void**)&Main);             // Out Delegate pointer

    DebugLog("load_fn retCode 0x%p", retCode);

    if (retCode != 0 || Main == nullptr)
    {
        DebugLog("Failed to get method pointer: 0x%1p | 0x%2p", retCode, Main);
        hostfxr_close(handle);
        return 7;
    }

    DebugLog("Obtained managed delgate %p", (void*)Main);

    retCode = load_assembly_and_get_function_pointer(
        overlayPath,                // Path to managed assembly
        L"GGXXACPROverlay.Program, GGXXACPROverlay",    //[Namespace].[Type], [Assembly]
        L"DetachAndUnload",         // Method Name
        UNMANAGEDCALLERSONLY_METHOD,// Entry point type
        nullptr,                    // Reserved
        (void**)&overlay_unload_function_pointer);  // Out Delegate pointer

    if (retCode != 0 || overlay_unload_function_pointer == nullptr)
    {
        DebugLog("Failed to get DetachAndUnload method pointer: 0x%1p | 0x%2p", retCode, overlay_unload_function_pointer);
        hostfxr_close(handle);
        return 9;
    }


    // Call the managed assembly's entry point
    DebugLog("Calling managed delegate...");
    retCode = Main(nullptr, 0);
    DebugLog("Managed Assembly returned: %1d", retCode);

    // Close Thread returning eject function address to injector
    hostfxr_close(handle);
    ExitThread(0);
}

static void OpenConsoleWindow() {
    AllocConsole();

    FILE* fpOut;
    freopen_s(&fpOut, "CONOUT$", "w", stdout);
    freopen_s(&fpOut, "CONOUT$", "w", stderr);

    FILE* fpIn;
    freopen_s(&fpIn, "CONIN$", "r", stdin);

    ios::sync_with_stdio(true);
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved)
{
    thisModule = hModule;

    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hModule);
        OpenConsoleWindow();
        DebugLog("Attached");
        CreateThread(nullptr, 0, HostDotnetRuntime, nullptr, 0, nullptr);
    }

    return TRUE;
}
 