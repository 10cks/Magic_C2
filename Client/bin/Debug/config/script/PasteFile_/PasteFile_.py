import sys

sys.path.append('config\\script')
from Construct import GetParaHex, GetSelfAsmInfo

if __name__ == '__main__':
    selfAsmHex, selfAsmHash = GetSelfAsmInfo()

    cutOrCopy = '0' if sys.argv[1] == 'cut' else '1'
    oldFilePath = sys.argv[2]
    newFilePath = sys.argv[3]
    paraHex = GetParaHex(cutOrCopy.encode() + oldFilePath.encode('GBK') + b'\x00' + newFilePath.encode('GBK') + b'\x00')
    print({'display': '', 'selfAsmHex': selfAsmHex, 'selfAsmHash': selfAsmHash, 'paraHex': paraHex})
    sys.stdout.flush()