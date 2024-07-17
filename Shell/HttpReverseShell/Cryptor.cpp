#include <map>
#include <iostream>

using namespace std;

// 加密数据
void EncryptData(char* data, int dataLength) {
	for (int i = 0; i < dataLength - 1; i++) {
		*(data + i) ^= *(data + dataLength - 1);
	}
}

// 解密数据
void DecryptData(char* data, int dataLength) {
	for (int i = 0; i < dataLength - 1; i++) {
		*(data + i) ^= *(data + dataLength - 1);
	}
}

// 获取自定义汇编哈希值
int GetSelfAsmHash(char* selfAsm) {
	int hash = 0;
	do {
		hash += *selfAsm;
		hash = (hash << 8) - hash;
	} while (*selfAsm++ != '!');
	return hash;
}

// 获取自定义汇编
char* GetSelfAsm(char* selfAsmOrHash, int selfAsmOrHashLength, map<int, char*>& selfAsmHashMap, char* selfAsmHashInfo) {
	// 首次使用
	if (selfAsmOrHashLength > 20) {
		if (strlen(selfAsmHashInfo) < 1000) {
			// 自定义汇编哈希: 自定义汇编 -> 自定义汇编哈希字典
			int selfAsmHash = GetSelfAsmHash(selfAsmOrHash);
			char* selfAsm = (char*)malloc(selfAsmOrHashLength);
			memcpy(selfAsm, selfAsmOrHash, selfAsmOrHashLength);
			selfAsmHashMap[selfAsmHash] = selfAsm;
			// 自定义汇编哈希 -> 自定义汇编哈希信息
			sprintf_s(selfAsmHashInfo, 1024, "%s,%d", selfAsmHashInfo, selfAsmHash);
		}
		return selfAsmOrHash;
	}
	// 通过哈希获取
	return selfAsmHashMap[atoi(selfAsmOrHash)];
}