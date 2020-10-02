#!/usr/bin/env python3

import os
import subprocess
import sys
from typing import List

REPOSITORY_DIR = os.path.dirname(__file__)
TEST_DIR = os.path.normpath(os.path.join(REPOSITORY_DIR, 'test'))
FRONTEND_BINARY = os.path.normpath(
    os.path.join(REPOSITORY_DIR, 'src', 'Frontend', 'bin', 'Debug', 'netcoreapp3.1', 'Frontend'))
CORELIB_DLL_SEARCH_PATH = os.path.normpath(
    os.path.join(REPOSITORY_DIR, 'src', 'CoreLib', 'bin', 'Debug', 'netstandard2.1'))

TERM_COLOR_RED = '\033[0;31m'
TERM_COLOR_GREEN = '\033[0;32m'
TERM_COLOR_CLEAR = '\033[0m'
TERM_ERASE_LINE = '\033[2K'

TERM_TEXT_FAIL = f'{TERM_COLOR_RED}FAIL{TERM_COLOR_CLEAR}'
TERM_TEXT_PASS = f'{TERM_COLOR_GREEN}PASS{TERM_COLOR_CLEAR}'

ANNOTATION_STARTING_TOKEN = '//@'
TEST_OUTPUT_PREFIX = 'monctest:'

if sys.platform == "win32":
    # Get ANSI codes working on newish windows consoles
    import ctypes

    kernel32 = ctypes.windll.kernel32
    kernel32.SetConsoleMode(kernel32.GetStdHandle(-11), 7)


    def is_crash(code):
        return code & 0x80000000
else:
    def is_crash(code):
        return code < 0 or code == 255


def main():
    showall = False
    if '--showall' in sys.argv:
        showall = True
        sys.argv.remove('--showall')

    test_files = []
    multi_files = {}
    for dirpath, dirnames, filenames in os.walk(TEST_DIR):
        if os.path.basename(dirpath) != 'multi_module':
            for filename in filenames:
                if os.path.splitext(filename)[1] == '.monc':
                    test_files.append(os.path.join(dirpath, filename))
        else:
            for filename in filenames:
                split_filename = os.path.splitext(filename)
                if split_filename[1] == '.monc':
                    split_basename = os.path.splitext(split_filename[0])
                    filepath = os.path.join(dirpath, filename)
                    if split_basename[1] in multi_files:
                        multi_files[split_basename[1]].append(filepath)
                    else:
                        multi_files[split_basename[1]] = [filepath]

    failed_files = []

    for path in test_files:
        if not test([path], showall):
            failed_files.append(path)

    for basename, file_list in multi_files.items():
        if not test(file_list, showall):
            failed_files.append(file_list)

    print('=' * 80)

    status = len(failed_files) == 0

    if status:
        print(f' ** {TERM_TEXT_PASS} **')
    else:
        print(f' ** {TERM_TEXT_FAIL} **')
        print('Failed tests:')
        for path in failed_files:
            print(path)

    print('=' * 80)

    if not status:
        sys.exit(1)


def test(paths: List[str], showall: bool) -> bool:
    sys.stdout.write(f'Testing {paths}...')
    sys.stdout.flush()

    result = None

    # TODO: Which file contains annotations for multi-file tests?
    annotations = parse_annotations(paths[0])

    args = [FRONTEND_BINARY]
    args.extend(paths)
    args.extend(('-L', CORELIB_DLL_SEARCH_PATH))
    args.extend(annotations.args)
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

    status = False

    if result:
        status = result.returncode == 0

        filename = os.path.basename(paths[0])
        if filename.startswith('fail_'):
            status = not status

        # Crashes always fail
        if is_crash(result.returncode):
            status = False
        elif annotations.expected_output and not check_output(result.stdout, annotations.expected_output):
            status = False

    sys.stdout.write('\r')
    sys.stdout.write(TERM_ERASE_LINE)

    if not status or showall:
        print('')
        print('-' * 80)

        if status:
            print(f'[{TERM_TEXT_PASS}] {paths}')
        else:
            print(f'[{TERM_TEXT_FAIL}] {paths}')

        print('-' * 80)

        if result:
            print(f'stdout: \n{result.stdout}')
            print(f'stderr: \n{result.stderr}')
            print(f'rv: {result.returncode}')
        else:
            print(f'Timed out (TODO: show output here for timed out process)')

        print('')

    return status


class Annotations(object):
    def __init__(self):
        self.args: List[str] = []
        self.expected_output = ''


def parse_annotations(filename: str) -> Annotations:
    with open(filename) as f:
        lines = f.read().split('\n')

    annotations = Annotations()

    for line in lines:
        line = line.lstrip()
        if not line.startswith(ANNOTATION_STARTING_TOKEN):
            continue
        key, value = line[len(ANNOTATION_STARTING_TOKEN):].split(' ', maxsplit=1)

        if key == 'args':
            annotations.args = [x for x in value.split(' ') if x]
        elif key == 'expected_output':
            annotations.expected_output = value.replace('\\n', '\n')

    return annotations


def check_output(full_output: str, expected: str) -> bool:
    full_lines = full_output.split('\n')
    new_lines: List[str] = []
    for line in full_lines:
        if line.startswith(TEST_OUTPUT_PREFIX):
            new_lines.append(line[len(TEST_OUTPUT_PREFIX):])
    return expected == '\n'.join(new_lines)


if __name__ == '__main__':
    main()
