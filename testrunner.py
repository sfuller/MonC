#!/usr/bin/env python3

import argparse
import os
import subprocess
import sys
from typing import List, Dict, Set

REPOSITORY_DIR = os.path.dirname(__file__)
TEST_DIR = os.path.normpath(os.path.join(REPOSITORY_DIR, 'test'))
FRONTEND_BINARY = os.path.normpath(
    os.path.join(REPOSITORY_DIR, 'src', 'Frontend', 'bin', 'Debug', 'netcoreapp3.1', 'Frontend'))
DRIVER_BINARY = os.path.normpath(
    os.path.join(REPOSITORY_DIR, 'src', 'Driver', 'bin', 'Debug', 'netcoreapp3.1', 'Driver'))
CORELIB_DLL_SEARCH_PATH = os.path.normpath(
    os.path.join(REPOSITORY_DIR, 'src', 'CoreLib', 'bin', 'Debug', 'netstandard2.1'))

TERM_COLOR_RED = '\033[0;31m'
TERM_COLOR_GREEN = '\033[0;32m'
TERM_COLOR_YELLOW = '\033[0;33m'
TERM_COLOR_CLEAR = '\033[0m'
TERM_ERASE_LINE = '\033[2K'

TERM_TEXT_FAIL = f'{TERM_COLOR_RED}FAIL{TERM_COLOR_CLEAR}'
TERM_TEXT_PASS = f'{TERM_COLOR_GREEN}PASS{TERM_COLOR_CLEAR}'
TERM_TEXT_PARTIAL_PASS = f'{TERM_COLOR_YELLOW}PARTIAL PASS{TERM_COLOR_CLEAR}'

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


class Arguments(object):
    def __init__(self):
        self.show_all = False
        self.non_passing = False


argparser = argparse.ArgumentParser()
argparser.add_argument('--showall', dest='show_all', action='store_true')
argparser.add_argument('--nonpassing', dest='non_passing', action='store_true')


class Test(object):
    def __init__(self, name: str):
        self.name = name
        self.files: List[str] = []


def main():
    args = Arguments()
    # noinspection PyTypeChecker
    _, args_to_pass = argparser.parse_known_args(namespace=args)

    tests_by_name: Dict[str, Test] = {}

    for dirpath, dirnames, filenames in os.walk(TEST_DIR):
        for filename in filenames:
            parts = filename.split('.')

            if parts[-1] != 'monc':
                continue

            inner_name: str

            if len(parts) > 2:
                # Multi-module
                inner_name = parts[1]
            else:
                inner_name = parts[0]

            test_name = os.path.join(os.path.relpath(dirpath, TEST_DIR), inner_name)

            test = tests_by_name.get(test_name)
            if test is None:
                test = Test(test_name)
                tests_by_name[test_name] = test

            test.files.append(os.path.join(dirpath, filename))

    for tool_name, tool, extra_args in (("Frontend", FRONTEND_BINARY, ()), ("Driver", DRIVER_BINARY, ()),
                                        ("Driver LLVM", DRIVER_BINARY, ("-toolchain=llvm",))):
        print(f'Using {tool_name} to run tests')
        tool_args_to_pass = args_to_pass.copy()
        tool_args_to_pass.extend(extra_args)

        passing_tests_path = os.path.normpath(os.path.join(REPOSITORY_DIR, '.passing_tests'))
        passing_tests: Set[str]
        if os.path.isfile(passing_tests_path):
            with open(passing_tests_path) as f:
                passing_tests = set(path for path in f.read().splitlines() if path)
        else:
            passing_tests = set()

        # Remove tests that are passing if we've asked to test non-passing tests only.
        if args.non_passing:
            for passing_test in passing_tests:
                del tests_by_name[passing_test]

        failed_tests: List[str] = []

        for test_name, test in tests_by_name.items():
            if not run_test(test, tool, args, tool_args_to_pass):
                failed_tests.append(test_name)
            else:
                passing_tests.add(test_name)

        passing_tests.difference_update(failed_tests)

        with open(passing_tests_path, 'w') as f:
            for passing_test in passing_tests:
                f.write(passing_test)
                f.write('\n')

        print('=' * 80)

        status = len(failed_tests) == 0

        if status:
            message = TERM_TEXT_PARTIAL_PASS if args.non_passing else TERM_TEXT_PASS
            print(f' ** {message} **')
        else:
            print(f' ** {TERM_TEXT_FAIL} **')
            print('Failed tests:')
            for test_name in failed_tests:
                print(test_name)

        print('=' * 80)

    if not status:
        sys.exit(1)


def run_test(test: Test, tool: str, runner_args: Arguments, args_to_pass: List[str]) -> bool:
    sys.stdout.write(f'Testing {test.name}...')
    sys.stdout.flush()

    result = None

    # TODO: Which file contains annotations for multi-file tests?
    annotations = parse_annotations(test.files[0])

    args = [tool]
    args.extend(test.files)
    args.extend(('-L', CORELIB_DLL_SEARCH_PATH))
    args.extend(annotations.args)
    args.extend(args_to_pass)

    stdout = ''
    stderr = ''

    try:
        result = subprocess.run(
            args,
            encoding='utf-8',
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=60)
    except subprocess.TimeoutExpired as e:
        stdout = e.stdout.decode('utf-8', 'replace') if e.stdout else ''
        stderr = e.stderr.decode('utf-8', 'replace') if e.stderr else ''

    status = False

    expect_failure = False
    for file in test.files:
        if os.path.basename(file).startswith('fail_'):
            expect_failure = True
            break

    if result:
        status = result.returncode == 0
        stdout = result.stdout
        stderr = result.stderr

        if expect_failure:
            status = not status

        # Crashes always fail
        if is_crash(result.returncode):
            status = False
        elif annotations.expected_output and not check_output(stdout, annotations.expected_output):
            status = False

    sys.stdout.write('\r')
    sys.stdout.write(TERM_ERASE_LINE)

    if not status or runner_args.show_all:
        print('')
        print('-' * 80)

        if status:
            print(f'[{TERM_TEXT_PASS}] {test.name}')
        else:
            print(f'[{TERM_TEXT_FAIL}] {test.name}')

        print('-' * 80)

        print(f'invocation:')
        for arg in args:
            print('  ' + arg)
        print('')

        print(f'stdout: \n{stdout}')
        print(f'stderr: \n{stderr}')
        if result:
            print(f'rv: {result.returncode}')
        else:
            print(f'Timed out.')

        print('')

    return status


class Annotations(object):
    def __init__(self):
        self.args: List[str] = []
        self.expected_output = ''


def parse_annotations(filename: str) -> Annotations:
    with open(filename) as f:
        lines = f.read().splitlines()

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
    full_lines = full_output.splitlines()
    new_lines: List[str] = []
    for line in full_lines:
        if line.startswith(TEST_OUTPUT_PREFIX):
            new_lines.append(line[len(TEST_OUTPUT_PREFIX):])
    return expected == '\n'.join(new_lines)


if __name__ == '__main__':
    main()
