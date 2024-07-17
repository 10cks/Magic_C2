import os
import re
import sys
import time
import shutil
import random
import subprocess

if __name__ == '__main__':
    profilePath = sys.argv[1]
    GeneratePath = sys.argv[2]
    shellPath = os.path.dirname(profilePath) + '\\..\\..\\'
    with open(profilePath, 'r', encoding='UTF-8') as file:
        profile = file.read().split('\n')

    random.seed(time.time())
    xor1 = random.randint(1, 255)
    xor2 = random.randint(1, 255)
    while xor2 == xor1:
        xor2 = random.randint(1, 255)

    # 解析 Profile
    processFilePath = None
    processFileCode = None
    processFilePathList = []
    for line in profile:
        # 定义变量
        if '=' in line:
            exec(line)

        # 替换代码字符串
        elif '->' in line:
            line = line.split('->')
            randMax = re.findall(r'rand\((\d+)\)', line[1])
            if randMax:
                line[1] = re.sub(r'rand\((\d+)\)', str(random.randint(1, int(randMax[0]))), line[1])
            if 'xor1' in line[1]:
                line[1] = line[1].replace('xor1', str(xor1))
            if 'xor2' in line[1]:
                line[1] = line[1].replace('xor2', str(xor2))
            processFileCode = processFileCode.replace(line[0], line[1])

        # 保存原代码 & 写入新代码
        elif line != 'End':
            if processFilePath:
                shutil.copy(processFilePath, processFilePath + '2')
                with open(processFilePath, 'w', encoding='UTF-8') as file:
                    file.write(processFileCode)

            processFilePath = shellPath + projectPath + '\\' + line
            processFilePathList += [processFilePath]
            with open(processFilePath, 'r', encoding='UTF-8', errors='ignore') as file:
                processFileCode = file.read()

        # 写入新代码
        elif line == 'End':
            if processFilePath:
                shutil.copy(processFilePath, processFilePath + '2')
                with open(processFilePath, 'w', encoding='UTF-8') as file:
                    file.write(processFileCode)
            break

    # 保存解释器 ShellCode 明文 & 加密解释器 ShellCode
    shellcodePath = shellPath + projectPath + '\\' + shellcodePath
    with open(shellcodePath, 'rb') as file:
        shellcode = file.read()
    cipherShellCode = b''
    for byte in shellcode:
        cipherShellCode += int.to_bytes(byte ^ xor1 ^ xor2)
    shutil.copy(shellcodePath, shellcodePath + '2')
    with open(shellcodePath, 'wb') as file:
        file.write(cipherShellCode)

    # 编译新代码
    subprocess.run('msbuild "' + shellPath + projectPath + '\\' + slnPath + '" /p:Configuration=Release /p:Platform=x64 /t:Clean')
    subprocess.run('msbuild "' + shellPath + projectPath + '\\' + slnPath + '" /p:Configuration=Release /p:Platform=x64 /t:Build')

    # 还原代码和解释器 ShellCode
    for processFilePath in processFilePathList:
        os.replace(processFilePath + '2', processFilePath)
    os.replace(shellcodePath + '2', shellcodePath)

    # 移动 Shell 至
    os.replace(shellPath + projectPath + '\\x64\\Release\\' + os.path.basename(slnPath).replace('.sln', '.exe'), GeneratePath)