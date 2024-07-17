import sys

sys.path.append('config\\script')
from Construct import GetParaHex, GetSelfAsmInfo

if __name__ == '__main__':
    selfAsmHex, selfAsmHash = GetSelfAsmInfo()

    eachDownloadSize = sys.argv[1]
    filePath = sys.argv[2]
    readIndex = sys.argv[3]
    paraHex = GetParaHex(eachDownloadSize.encode() + b'\x00' + filePath.encode('GBK') + b'\x00' + readIndex.encode() + b'\x00')
    print({'display': '', 'selfAsmHex': selfAsmHex, 'selfAsmHash': selfAsmHash, 'paraHex': paraHex})
    sys.stdout.flush()