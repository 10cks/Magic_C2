import sys

sys.path.append('config\\script')
from Construct import GetParaHex, GetSelfAsmInfo

if __name__ == '__main__':
    selfAsmHex, selfAsmHash = GetSelfAsmInfo()

    filePath = sys.argv[1]
    paraHex = GetParaHex(filePath.encode('GBK') + b'\x00')
    print({'display': '', 'selfAsmHex': selfAsmHex, 'selfAsmHash': selfAsmHash, 'paraHex': paraHex})
    sys.stdout.flush()