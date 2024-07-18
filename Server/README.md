安装库:
go mod init Server
go get github.com/gin-gonic/gin
go get github.com/mattn/go-sqlite3
go-sqlite3 需要 GCC 的解决办法:
1.下载 https://github.com/mstorsjo/llvm-mingw/releases 中的 llvm-mingw-2024xxxx-ucrt-x86_64
2.将 bin 添加至环境变量
3.设置 CGO_ENABLED=1
4.重启编译器

编译:
go build -ldflags "-s -w" -o Server.exe
