import os
import sys

if __name__ == '__main__':
    help = '全部命令:\n'
    for root, folders, files in os.walk('config\\script'):
        for folder in folders:
            if '.' not in folder and '_' not in folder and folder != 'help':
                help += folder + '\n'
    print({'display': help + '[命令名称] help\nclose 关闭当前窗口'})
    sys.stdout.flush()