# dsm_testing

This is a quick and dirty (read: highly experimental) [Synthesis](https://mutagen-modding.github.io/Synthesis/) 
patcher which I hope will be useful for analyzing and _maybe_ creating patches for the [DarkStar Manufacturing](https://www.nexusmods.com/starfield/mods/9963) Starfield mod.

Massive props to everybody on the DarkStar (especially WykkydGaming and DaAngryMechanic) and Synthesis (especially Noggog) Discord servers for their help.

## Current Usage

At the moment this requires the use of pre-release versions of [Synthesis](https://github.com/Mutagen-Modding/Synthesis/releases/tag/0.33.0-pre-release.2) and [Mutagen](https://www.nuget.org/packages/Mutagen.Bethesda.Starfield/0.49.0-alpha.74).

In Synthesis, add this repo as a Git Repository patcher, making sure to Match the Mutagen and Synthesis versions in the UI -- the Profile and Latest are too old, and Manual defaults to a bunch of zeroes.

Then, just run the thing. The patcher will then:

* verify that your load order includes both the [DarkStar Manufacturing](https://www.nexusmods.com/starfield/mods/9963) mod and my [DSM Cosplay Workbench](https://www.nexusmods.com/starfield/mods/12868) mod
* make sure that it can find the `if_Crafting_ForceQuality01` Keyword
* search through all ConstructibleObject (COBJ) records for any that (a) have the `WorkbenchIndustrial` keyword and (b) take one armor record as input and output one armor record, which I can pretty safely assume are Mk.II Enhancement recipes
* For each found Mk.II recipe, check that it has the `if_Crafting_ForceQuality01` Instantiation Filter keyword set and log a `** WARNING **` if it doesn't
