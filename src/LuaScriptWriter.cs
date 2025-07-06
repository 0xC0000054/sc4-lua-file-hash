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

using SC4Parser.Files;

namespace SC4LuaFileHash
{
    internal static class LuaScriptWriter
    {
        private const uint LuaScriptTypeID = 0xCA63E2A3;
        private const uint AdvisorLuaGroupID = 0x4A5E8EF6;
        private const uint AutomataLuaGroupID = 0x4A5E8F3F;

        private static readonly char[] LuaStringQuotationChars = ['"', '\''];

        public static uint GetFileNameHash(ReadOnlySpan<char> inputFileName)
        {
            // The Maxis algorithm for computing the instance id from the Lua script
            // file name consists of the following steps:
            //
            // 1. Get file name without the path and extension.
            // 2. Hash that value with a 24-bit CRC.
            // 3. Logically or the 24-bit hash with 0xFF000000.

            ReadOnlySpan<char> hashInput = Path.GetFileNameWithoutExtension(inputFileName);
            uint fileNameCRC = RZHash.Crc24(hashInput, false);

            return 0xFF000000 | fileNameCRC;
        }

        public static (int luaFilesWritten, int luaFilesRenamed) WriteScriptsToDirectory(string datPath, string directoryPath)
        {
            int luaFileCount = ExtractLuaScriptsFromDatFile(datPath, directoryPath);
            int renamedFileCount = 0;

            if (datPath.EndsWith("SimCity_1.dat", StringComparison.OrdinalIgnoreCase))
            {
                renamedFileCount = RenameHashedMaxisScripts(directoryPath);
            }

            return (luaFileCount, renamedFileCount);
        }

        private static int ExtractLuaScriptsFromDatFile(string datFileName, string extractedScriptRoot)
        {
            int luaFilesWritten = 0;

            using (var datFile = new DatabasePackedFile(datFileName))
            {
                var entries = datFile.IndexEntries;

                string advisorScriptDir = Path.Combine(extractedScriptRoot, "advisor");
                Directory.CreateDirectory(advisorScriptDir);

                string automataScriptDir = Path.Combine(extractedScriptRoot, "automata");
                Directory.CreateDirectory(automataScriptDir);

                foreach (var indexEntry in entries)
                {
                    var tgi = indexEntry.TGI;

                    if (tgi.Type == LuaScriptTypeID)
                    {
                        var data = datFile.LoadIndexEntry(indexEntry);

                        string scriptDir = extractedScriptRoot;

                        switch (tgi.Group)
                        {
                            case AdvisorLuaGroupID:
                                scriptDir = advisorScriptDir;
                                break;
                            case AutomataLuaGroupID:
                                scriptDir = automataScriptDir;
                                break;
                        }

                        string filePath = Path.Combine(scriptDir, $"{tgi.Instance:X8}.lua");

                        File.WriteAllBytes(filePath, data);
                        luaFilesWritten++;
                    }
                }
            }

            return luaFilesWritten;
        }

        private static HashSet<string> GetMaxisFileNamesFromScriptData(IReadOnlyDictionary<string, string> files)
        {
            HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in files)
            {
                using (StreamReader sr = new(kvp.Value))
                {
                    string? line = null;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("dofile", StringComparison.Ordinal))
                        {
                            // Handle dofile('file.lua') and dofile("file.lua").

                            int fileNameStart = line.IndexOfAny(LuaStringQuotationChars);
                            int fileNameEnd = line.LastIndexOfAny(LuaStringQuotationChars);

                            if (fileNameStart != -1 && fileNameEnd != -1 && fileNameEnd > fileNameStart)
                            {
                                ReadOnlySpan<char> fileName = line.AsSpan()[(fileNameStart + 1)..fileNameEnd];

                                if (fileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                                {
                                    set.Add(fileName.ToString());
                                }
                            }
                        }
                        else if (line.StartsWith("--", StringComparison.Ordinal)
                              && line.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                        {
                            // Handle '-- file.lua' comments.

                            ReadOnlySpan<char> fileName = line.AsSpan().TrimStart('-').Trim();

                            // Exclude a few false positives that are part of larger comments.
                            if (!fileName.Contains(' '))
                            {
                                set.Add(fileName.ToString());
                            }
                        }
                    }
                }
            }

            return set;
        }

        private static Dictionary<string, string> GetHashedMaxisFileNameDictionary(IReadOnlyDictionary<string, string> files)
        {
            Dictionary<string, string> dictionary = new(StringComparer.OrdinalIgnoreCase);

            var maxisFileNames = GetMaxisFileNamesFromScriptData(files);

            foreach (var maxisFileName in maxisFileNames)
            {
                string hashedFileName = $"{GetFileNameHash(maxisFileName):X8}.lua";

                dictionary.TryAdd(hashedFileName, maxisFileName);
            }

            // Handle the remaining files that are not named in the Maxis scripts.
            // adv_ep1_fluffnews.lua and examples.lua are the original names for those files, the hashes match.
            //
            // _adv_startup.lua and _scripting_conventions.lua were picked based on the file contents.
            // Trying to discover the original file names through brute forcing the hash is probably not
            // feasible, there are simply too many potential file names.

            dictionary.TryAdd("FF1A27EC.lua", "adv_ep1_fluffnews.lua");
            dictionary.TryAdd("FF8085FD.lua", "_adv_startup.lua");
            dictionary.TryAdd("FFAEC42B.lua", "_scripting_conventions.lua");
            dictionary.TryAdd("FFE6C952.lua", "examples.lua"); // automata scripting examples

            return dictionary;
        }


        private static int RenameHashedMaxisScripts(string extractedScriptRoot)
        {
            var files = Directory.EnumerateFiles(extractedScriptRoot, "*", SearchOption.AllDirectories).ToDictionary(
                k => Path.GetFileName(k), StringComparer.OrdinalIgnoreCase);

            var hashedMaxisFileNameDictionary = GetHashedMaxisFileNameDictionary(files);

            int renamedFileCount = 0;

            foreach (var item in hashedMaxisFileNameDictionary)
            {
                if (files.TryGetValue(item.Key, out string? hashedFilePath))
                {
                    string maxisNameFilePath = Path.Combine(Path.GetDirectoryName(hashedFilePath)!, item.Value);

                    File.Move(hashedFilePath, maxisNameFilePath, true);
                    renamedFileCount++;
                }
            }

            return renamedFileCount;
        }
    }
}
