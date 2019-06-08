# MonC
"MonC" is a toy programming language. It's heavily inspired by C, but so far it's closer to the B programming language. 
I'm working on a game behind the scenes that uses this language as part of it's gameplay. The game uses this library
to help implement this language as part of the game.

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
* Language Level Memory Addressing, Pointers Operations (Some memory operations might be implemented with poke/peek functions)

## Language 

## Why is it called MonC?
The original idea for this game I'm working on (in my spare time) was "RGP game but you battle by writting code instead of 
navigating through menu systems". "Mon" stands for Monster and "C" stands for C :)

## Why is this code public?
It's fun to show off code :)

## What's in this repo?
* Simple Frontend (For testing purposes)
* Lexer
* Parser
That's it for now

## What's not in this repo?
* Interpreter (Keeps changing rapidly so I'm not intending on ripping it out of the game's codebase until later.)
* VM/IL/CodeGen (As fun as this would be to implement, it's not super important to the game right now.)
