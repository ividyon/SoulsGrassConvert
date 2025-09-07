[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

# SoulsGrassConvert
A quick tool for Souls modding (ELDEN RING, Sekiro, Armored Core VI) to convert and edit GRASS files as GLTF files.

## Basic explanation

A GRASS file is a model which contains per-vertex information about which grass meshes should be generated in a given area. It is typically bundled with map piece or asset FLVER files and generally matches their position.

The map file (MSB) contains the grass parameter setup for the associated asset or mappiece, assigning a GrassParam entry to one of 6 indices (0-5).

For each such index, every vertex in the GRASS file contains a weight, describing how frequently the associated GrassParam entry should spawn near that vertex.

This converter turns GRASS into the GLTF 3D format, and assigns the grass weights to Vertex Color channels. This way, you can import it into Blender, and use the Vertex Paint mode to literally paint grasses as you wish.

You can then convert the new, modified GLTF back into GRASS.

## Common issues & solutions

- You must provide a fully triangulated mesh. No quads!
- Adding new vertices and triangles has not been tested.

## Credits

* All SoulsFormatsNEXT contributors
* *ivi* - Author

## License

This work is licensed under GPL v3. View the implications for your forks [here](https://www.tldrlegal.com/license/gnu-general-public-license-v3-gpl-3).

SoulsGrassConvert is built using the following licensed works:
* [SoulsFormats](https://github.com/JKAnderson/SoulsFormats/tree/er) by JKAnderson (see [License](licenses/LICENSE-SoulsFormats.md))
* [PromptPlus](https://github.com/FRACerqueira/PromptPlus), Copyright 2021 @ Fernando Cerqueira (see [License](licenses/LICENSE-PromptPlus.md))

## Changelog

### 1.0.0.0

* Initial release.
