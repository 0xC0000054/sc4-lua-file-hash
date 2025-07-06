# SC4LuaFileHash

A C# console application that recreates the Lua file name hashing used by SimCity 4.

The program can be downloaded from the releases tab: https://github.com/0xC0000054/sc4-lua-file-hash/releases

## Usage

The program has two modes, _extract_ and _hash_.

The _extract_ command takes two parameters, the path to the DBPF file and the path to the output directory.
The _hash_ command hashes the input file name and writes the result to the console.

When extracting Lua files from _SimCity\_1.dat_, the program will rename them to the original name (if known) or use
a guess based on the file contents.

The two files with guessed names are _\_adv\_startup.lua_ and _\_scripting\_conventions.lua_.
Trying to discover the original file names through brute forcing the hash is probably not
feasible, there are simply too many potential file names.

## License

This project is licensed under the terms of the GNU Lesser General Public License version 3.0.   
See [License.txt](License.txt) for more information.

### Third-Party Libraries

[SC4Parser](https://github.com/Killeroo/SC4Parser) - MIT License