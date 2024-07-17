#include <map>
#include <iostream>

using namespace std;

// ��������
void EncryptData(char* data, int dataLength) {
	for (int i = 0; i < dataLength - 1; i++) {
		*(data + i) ^= *(data + dataLength - 1);
	}
}

// ��������
void DecryptData(char* data, int dataLength) {
	for (int i = 0; i < dataLength - 1; i++) {
		*(data + i) ^= *(data + dataLength - 1);
	}
}

// ��ȡ�Զ������ϣֵ
int GetSelfAsmHash(char* selfAsm) {
	int hash = 0;
	do {
		hash += *selfAsm;
		hash = (hash << 8) - hash;
	} while (*selfAsm++ != '!');
	return hash;
}

// ��ȡ�Զ�����
char* GetSelfAsm(char* selfAsmOrHash, int selfAsmOrHashLength, map<int, char*>& selfAsmHashMap, char* selfAsmHashInfo) {
	// �״�ʹ��
	if (selfAsmOrHashLength > 20) {
		if (strlen(selfAsmHashInfo) < 1000) {
			// �Զ������ϣ: �Զ����� -> �Զ������ϣ�ֵ�
			int selfAsmHash = GetSelfAsmHash(selfAsmOrHash);
			char* selfAsm = (char*)malloc(selfAsmOrHashLength);
			memcpy(selfAsm, selfAsmOrHash, selfAsmOrHashLength);
			selfAsmHashMap[selfAsmHash] = selfAsm;
			// �Զ������ϣ -> �Զ������ϣ��Ϣ
			sprintf_s(selfAsmHashInfo, 1024, "%s,%d", selfAsmHashInfo, selfAsmHash);
		}
		return selfAsmOrHash;
	}
	// ͨ����ϣ��ȡ
	return selfAsmHashMap[atoi(selfAsmOrHash)];
}