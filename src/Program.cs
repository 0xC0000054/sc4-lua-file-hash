/*
 * This file is part of SC4LuaFileHash, a utility that
 * that recreates the Lua file name hashing used by SimCity 4.
 *
 * Copyright (C) 2025 Nicholas Hayes
 *
 * SC4LuaFileHash is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 *
 * SC4LuaFileHash is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with SC4LuaFileHash.
 * If not, see <http://www.gnu.org/licenses/>.
 */

namespace SC4LuaFileHash
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            try
            {
                string command = args[0];

                if (command.Equals("extract", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length != 3)
                    {
                        PrintUsage();
                        return;
                    }

                    string datFilePath = args[1];
                    string directoryPath = args[2];

                    (int luaFilesWritten, int luaFilesRenamed) = LuaScriptWriter.WriteScriptsToDirectory(datFilePath, directoryPath);

                    Console.WriteLine("Extracted {0} Lua files from {1}", luaFilesWritten, datFilePath);

                    if (luaFilesRenamed > 0)
                    {
                        Console.WriteLine("Renamed {0} Maxis Lua files.", luaFilesRenamed);
                    }
                }
                else if (command.Equals("hash", StringComparison.OrdinalIgnoreCase))
                {
                    string input = args[1];

                    uint hashedValue = LuaScriptWriter.GetFileNameHash(input);
                    Console.WriteLine($"The hash of '{input}' is: 0x{hashedValue:X8}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("SC4LuaFileHash <command> <command arguments>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  extract    Extracts the Lua files from a DAT and writes them to the specified output folder.");
            Console.WriteLine("       arguments:");
            Console.WriteLine("                   The first argument is the path to the DBPF file.");
            Console.WriteLine("                   The second argument is the path to the output directory.");
            Console.WriteLine("  hash    Writes the hash of the input file name to the console.");
            Console.WriteLine("       arguments:");
            Console.WriteLine("                   The file name value to hash.");
        }
    }
}
