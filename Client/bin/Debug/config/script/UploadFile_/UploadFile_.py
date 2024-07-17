import sys

sys.path.append('config\\script')
from Construct import GetParaHex, GetSelfAsmInfo

if __name__ == '__main__':
    selfAsmHex, selfAsmHash = GetSelfAsmInfo()

    localFilePath = sys.argv[1]
    targetFilePath = sys.argv[2]
    readIndex = sys.argv[3]
    eachUploadSize = sys.argv[4]

    with open(localFilePath, 'rb') as file:
        file.seek(int(readIndex))
        fileData = file.read(int(eachUploadSize))

    paraHex = GetParaHex(('0' if readIndex == '0' else '1').encode('GBK') + targetFilePath.encode('GBK') + b'\x00' + fileData)
    print({'display': '', 'selfAsmHex': selfAsmHex, 'selfAsmHash': selfAsmHash, 'paraHex': paraHex, 'scriptInfo': str(len(fileData))})
    sys.stdout.flush()