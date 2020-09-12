#!/usr/bin/env python3
import os
import sys
import subprocess
import difflib


def main():
    other_working_dir = sys.argv[1]

    args = ['./monc']
    args.extend(sys.argv[2:])

    left_result = subprocess.run(args, cwd=other_working_dir, stdout=subprocess.PIPE, encoding='utf-8')

    # Current 'cwd' assumes this script is one directory below monc executable.
    current_cwd = os.path.normpath(os.path.join(__file__, '..', '..'))
    right_result = subprocess.run(args, cwd=current_cwd, stdout=subprocess.PIPE, encoding='utf-8')

    diff = difflib.unified_diff(left_result.stdout.split('\n'), right_result.stdout.split('\n'), lineterm='')
    sys.stdout.write('\n'.join(diff))
    sys.stdout.write('\n')


if __name__ == '__main__':
    main()

