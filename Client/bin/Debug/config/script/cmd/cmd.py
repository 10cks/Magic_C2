import sys

sys.path.append('config\\script')
from Construct import GetParaHex, GetSelfAsmInfo

if __name__ == '__main__':
    selfAsmHex, selfAsmHash = GetSelfAsmInfo()

    cmd = 'cmd /c ' + ' '.join(sys.argv[1:])
    if cmd == 'cmd /c help':
        print({'display': 'cmd [CMD命令]'})
    else:
        paraHex = GetParaHex(cmd.encode('GBK') + b'\x00')
        print({'display': '', 'selfAsmHex': selfAsmHex, 'selfAsmHash': selfAsmHash, 'paraHex': paraHex})
    sys.stdout.flush()