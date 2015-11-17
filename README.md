# CastleDBGen

CastleDB: http://castledb.org/
Github: https://github.com/ncannasse/castle

Process CastleDB database JSON files and generates source code for both the types contained in the database and code to load and resolve foreign keys.

**Supported CastleDB data types**
* Unique Identifier
* Text
* bool
* Int
* Float
* Enum
* Flags
* Lists
* References
* Color
* Image (as String)
* File (as String)

**Unsupported CastleDB data types**
* Layer
* TilePos
* TileLayer
* Custom
* Dynamic

## Command line usage printout:

    CastleDBGen - (C) JSandusky 2015
    usage:
    CastleDBGen <input-db-path> [outputfilename]

    switches:
    -ns: namespace, follow with namespace
        default: none
        C#: REQUIRED
    -lang: language: cpp, as, cs, lua
        default: cpp
    -hd: header path string, C++ only
    -db: name for database class
        default: GameDatabase

**Examples**

    CastleDBGen C:\MyDdatabase.cdb -lang cpp -ns MyNamespace
    CastleDBGen C:\MyDdatabase.cdb -lang as
    CastleDBGen C:\MyDdatabase.cdb -lang cpp -hd "../HeaderPath/"

## Dependencies
Newtonsoft.JSON via Nuget
