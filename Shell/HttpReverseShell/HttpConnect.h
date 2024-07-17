#pragma once

#include <queue>

using namespace std;

struct ConnectData {
	char* data;
	int dataLength;
};

void HttpRequest(char*, int, char*, char*, int, char*, queue<ConnectData>&, queue<ConnectData>&);