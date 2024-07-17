#pragma once

#include <map>

using namespace std;

void EncryptData(char*, int);
void DecryptData(char*, int);

char* GetSelfAsm(char*, int, map<int, char*>&, char*);