def Run(scriptPara, scriptsPath):
    return {'display': '', 'paraHex': (scriptPara['eachDownloadSize'].encode() + b'\x00' + scriptPara['targetFilePath'].encode('GBK') + b'\x00' + scriptPara['downloadSize'].encode() + b'\x00').hex()}