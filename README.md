# Harmony Patch Change Parser

This doc will be a bit rough as I am unsure if anyone else will find this utility useful.

A command line utility that creates a report that matches changes in a game's git repo to Harmony patches in mods.

This is a personal utility to map git changes of a project to mods that use Harmony Patches.

Currently it has some logic that is specific to the Quasimorph game, but I could update it if someone is interested.


# Operation
This utility assumes that the user has the source code of the target game in a git repository.

use `.\HarmonyPatchChangeParser.exe --help` to get the list of parameters.  See the [Help](#help-output) section below.

Example:
`.\HarmonyPatchChangeParser.exe -a "HEAD~4" -b "HEAD" -s C:\SRC\Decompiled\Quasimorph\ -m C:\src\Quasimorph` 

The user provides the tree-ish git commits to get the change list from, the directory that the game's source is in, and the folder that contains the mod or mods.

Quasimorph Specific Logic:  This mod only includes types that are in the MGSC namespace, which is where Quasimorph classes are located.


The utility will then output GameFileChanges.tsv and a HarmonyReport.tsv.  

GameFileChanges.tsv is somewhat the list of the files/types that were changed.  
The mod assumes that all the files in the MGSC source folder are types.  

The HarmonyReport.tsv joins any mod's Harmony lines to the "types" that were change in the game's git commits.  A line is included in the report if it had the word `Harmony`, a `HarmonyPatch(typeof(Foo)...`, and/or a `HarmonyMethod(typeof(Foo)...`.

It also optionally includes any lines that included the text "copy".  I currently use that as a convention to indicate functions that are a copy and replace or copies logic from another game's function.

Example: `//COPY: This is a partial copy of the Foo.Bar logic`.


# HarmonyReport.tsv Columns
|Name|Description|
|--|--|
|ChangeType|See [ChangeType](#changetype) below.|
|PatchObjectType|The typeof() type that was found in a HarmonyPatch or HarmonyMethod line.|
|FileName|The full path to the file the line was found in|
|HarmonyPatchLine|The full text of the source code line for this entry.|

## Example Output: (Formatted)
|ChangeType        |PatchObjectType|FileName                                                                            |HarmonyPatchLine                                                                   |
|------------------|---------------|------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
|None              |CorpseStorage  |C:\src\Quasimorph\ChangeExploredColor\src\CorpseStorage_Patch.cs                    |[HarmonyPatch(typeof(CorpseStorage), nameof(CorpseStorage.Highlight))]             |
|CopyWarning       |               |C:\src\Quasimorph\ChangeExploredColor\src\Creature3dView_HighlightAsCorpse__Patch.cs|//--- This is a copy of the original function, just with the new color property id.|
|HarmonyPatchChange|Creature3dView |C:\src\Quasimorph\ChangeExploredColor\src\Creature3dView_HighlightAsCorpse__Patch.cs|[HarmonyPatch(typeof(Creature3dView), nameof(Creature3dView.HighlightAsCorpse))]   |
|None              |ItemOnFloor    |C:\src\Quasimorph\ChangeExploredColor\src\ItemOnFloorHighlightPatch.cs              |[HarmonyPatch(typeof(ItemOnFloor), nameof(ItemOnFloor.Highlight))]                 |


## ChangeType
|Value|Description|
|--|--|
|HarmonyPatchChange| HarmonyPatch or HarmonyMethod had a typeof() that was changed in the git commits|
|CopyWarning| Found the text 'copy' in the line|
|None|Found the text Harmony but did not include a typeof(), or the typeof() type had no git changes.|



# Help Output

The output from the --help option:

```
HarmonyPatchChangeParser 1.0.0+93ad0351c3525b278765c46bc999377a8ffadff1
Copyright (C) 2025 HarmonyPatchChangeParser

  -a, --git-commit-a             Required. The first Git commit hash or
                                 reference.

  -b, --git-commit-b             Required. The second Git commit hash or
                                 reference.

  -s, --game-source-path         Required. Path to the game source directory.

  -m, --harmony-mods-path        Required. Path to the Harmony mods directory.

  -h, --harmony-output-file      (Default: HarmonyReport.tsv) The path to the
                                 output file for the harmony patches report.
                                 Use - to output to the console.  Use '' to not
                                 export.

  -f, --game-file-changes        (Default: GameFileChanges.tsv) The path to
                                 output the files that were changed in the git
                                 commits. Use - to output to the console.  Use
                                 '' to not export.

  -c, --include-copy-warnings    (Default: true) If set, will include any lines
                                 which contain the text 'copy' to try to find
                                 any copy and replace patches.  The word 'copy'
                                 is by convention.

  -g, --git path                 (Default: ) The path to the git executable. Use
                                 '' to require git to be in the path.

  --help                         Display this help screen.

  --version                      Display version information.


```
