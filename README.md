# MonC [![Build Status](https://travis-ci.com/sfuller/MonC.svg?branch=master)](https://travis-ci.com/github/sfuller/MonC)
"MonC" is a toy programming language. It's heavily inspired by C, but so far it's closer to the B programming language. 

## Design Goals
* Easy to learn for those not versed in programming.
* Keep it super simple.

## Language Features (So far)
* Common Flow Control Functionality (if/else, for, while, continue, break, return)
* Arithmetic Operators
* Comparison Operators
* Function Definitions and Calls

## Whats missing? (Right now)
* Type system (everything is a 32 bit integer right now)
* Language Level Memory Addressing, Pointer Operations (Some memory operations might be implemented with poke/peek functions)
* Indirect Function Calls

## Why is it called MonC?
I don't know.

## Why is this code public?
It's fun to show off code :)

## What's in this repo?
* Simple Frontend (For testing purposes)
* Lexer
* Parser
* VM + IL Code Generator
* Debugger

## What's not in this repo?
* Executable Generation
* AOT/JIT (Hooking this up to LLVM would be pretty neat!)
