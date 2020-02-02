#!/usr/bin/env python3

import os
import subprocess
import sys

TEST_DIR = os.path.normpath(os.path.join(__file__, '..', 'test'))
FRONTEND_BINARY = os.path.normpath(os.path.join(__file__, '..', 'src', 'Frontend', 'bin', 'Debug', 'netcoreapp3.1', 'Frontend'))


TERM_COLOR_RED =   '\033[0;31m'
TERM_COLOR_GREEN = '\033[0;32m'
TERM_COLOR_CLEAR = '\033[0m'
TERM_ERASE_LINE =  '\033[2K'


TERM_TEXT_FAIL = f'{TERM_COLOR_RED}FAIL{TERM_COLOR_CLEAR}'
TERM_TEXT_PASS = f'{TERM_COLOR_GREEN}PASS{TERM_COLOR_CLEAR}'


def main():

    showall = False
    if '--showall' in sys.argv:
        showall = True
        sys.argv.remove('--showall')

    test_files = [os.path.join(TEST_DIR, p) for p in os.listdir(TEST_DIR) if os.path.splitext(p)[1] == '.monc']

    failed_files = []

    for path in test_files:
        if not test(path, showall):
            failed_files.append(path)

    print('=' * 80)
    
    status = len(failed_files) is 0

    if status:
        print(f' ** {TERM_TEXT_PASS} **')
    else:
        print(f' ** {TERM_TEXT_FAIL} **')
        print ('Failed tests:')
        for path in failed_files:
            print(path)
   
    print('=' * 80)

    if not status:
        sys.exit(1)


def test(path, showall: bool) -> bool:
    sys.stdout.write(f'Testing {path}...')
    sys.stdout.flush()
    
    result = None

    with open(path) as f:
        args = [FRONTEND_BINARY, path]
        args.extend(sys.argv[1:])
        try:
            result = subprocess.run(
                    args,
                    encoding='utf-8',
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    timeout=60)
        except subprocess.TimeoutExpired:
            pass
    
    filename = os.path.basename(path)
   
    status = False

    if result:
        status = result.returncode is 0

        if filename.startswith('fail_'):
            status = not status

        # Crashes always fail
        if result.returncode < 0 or result.returncode is 255:
            status = False

    sys.stdout.write('\r')
    sys.stdout.write(TERM_ERASE_LINE)

    if not status or showall:
        print('')
        print('-' * 80)

        if status:
            print(f'[{TERM_TEXT_PASS}] {path}')
        else:
            print(f'[{TERM_TEXT_FAIL}] {path}')
        
        print('-' * 80)
        
        if result:
            print(f'stdout: \n{result.stdout}')
            print(f'stderr: \n{result.stderr}')
            print(f'rv: {result.returncode}')
        else:
            print(f'Timed out (TODO: show output here for timed out process)')

        print('')

    return status


if __name__ == '__main__':
    main()

