def Run(scriptPara, scriptsPath):
    return {'display': '', 'paraHex': (scriptPara['filePath'].encode('GBK') + b'\x00').hex()}