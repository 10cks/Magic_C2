import os
import sys
import time
import random

from Imul import ProcessImul
from GenerateSelfAsm import FormatAsm
from Disassembly import ParseShellCode

def GetSelfAsmHash(selfAsm):
    hash = 0
    for byte in selfAsm:
        hash += ord(byte)
        hash = (hash << 8) - hash
    hash &= 0xffffffff  # 保留 32 位
    if hash & 0x80000000:  # 负数
        hash = -((~hash + 1) & 0xffffffff)
    return str(hash)

def AsmConverter(shellcode):
    asm = ParseShellCode(shellcode)
    asm = ProcessImul(asm)
    return FormatAsm(asm)

def GetParaHex(para):
    random.seed(time.time())
    return (str(len(para)).encode() + b'\x00' + para + bytes([random.randint(0x01, 0xFF)])).hex()

def GetSelfAsmInfo():
    with open(os.path.dirname(sys.argv[0]) + '\\ShellCode.txt', 'r', encoding='UTF-8') as file:
        shellcode = file.read().replace(' ', '').replace('\n', '')
    selfAsm = AsmConverter(shellcode)
    selfAsmHex = selfAsm.encode().hex()
    selfAsmHash = GetSelfAsmHash(selfAsm)
    return selfAsmHex, selfAsmHash