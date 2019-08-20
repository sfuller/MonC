#!/usr/bin/env python3

import os
import subprocess
import sys

TEST_DIR = os.path.normpath(os.path.join(__file__, '..', 'test'))
FRONTEND_BINARY = os.path.normpath(os.path.join(__file__, '..', 'bin/Debug/Frontend.exe'))

def main():
    test_files = [os.path.join(TEST_DIR, p) for p in  os.listdir(TEST_DIR)]

    failed_files = []

    for path in test_files:
        if not test(path):
            failed_files.append(path)

    if len(failed_files) > 0:
        print('==========')
        print('Failed Files:')
        print('')
        for path in failed_files:
            print(path)
        sys.exit(1)


def test(path):
    print('-----')
    print(f'Testing {path}')
    with open(path) as f:
        args = ['mono', FRONTEND_BINARY]
        args.extend(sys.argv[1:])
        result = subprocess.run(args, stdin=f)
    

    return result.returncode is 0
        

if __name__ == '__main__':
    main()

