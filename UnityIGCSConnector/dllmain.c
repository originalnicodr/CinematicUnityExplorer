// dllmain.cpp : Defines the entry point for the DLL application.
#include <Windows.h>
#include <stdint.h>
#include <stdio.h>

#define EXPOSE __declspec(dllexport)

// The function that will be the one called from the C# side, that will update the corresponding values.
typedef void (*MoveCameraCallback)(float, float, float, int);
MoveCameraCallback GlobalCallback = NULL;


EXPOSE int IGCS_StartScreenshotSession(uint8_t _ignore) {
  return 0;
}

EXPOSE void IGCS_EndScreenshotSession() {}

EXPOSE void U_IGCS_Initialize(MoveCameraCallback cb) {
  AllocConsole();
  printf("Initializing callback\n");
  GlobalCallback = cb;

  // Load IGCS
  HMODULE igcs = LoadLibraryA("IgcsConnector.addon64");

  if (!igcs) {
    return;
  }

  FARPROC cameraToolsFunction = GetProcAddress(igcs, "connectFromCameraTools");
  FARPROC getCameraData = GetProcAddress(igcs, "getDataFromCameraToolsBuffer");

  if (cameraToolsFunction == NULL) {
    return;
  }

  cameraToolsFunction();

  uint8_t* cameraData = getCameraData();

  cameraData[0] = 1;

  printf("Camera connected!\n");

  return;

}

EXPOSE void IGCS_MoveCameraPanorama() {}

EXPOSE void IGCS_MoveCameraMultishot(float step_left, float step_up, float fov, int from_start) {
  printf("%s WAS CALLED\n", __FUNCTION__);
  GlobalCallback(step_left, step_up, fov, from_start);

  return;
}



BOOL WINAPI DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}