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
* File (as String)

**Unsupported CastleDB data types**
* Layer
* TilePos
* TileLayer
* Custom
* Dynamic
* Image (seperate IMG database, base64 encoding - will probably fork CastleDB to work with Urho3D resources in the future)

## Command line usage printout:

    CastleDBGen - (C) JSandusky 2015
    usage:
    CastleDBGen <input-db-path> [outputfilename]

    switches:
    -ns: namespace, follow with namespace
        default: none
        C#: REQUIRED
    -lang: <language>
        default: cpp
        option: as
        option: cs
        option: lua
        option: asbind (generate AS bindings)
    -hd: <header path string>, C++ only
    -db: name for database class
        default: GameDatabase
    -bin: <setting>, type of binary read/write suppprt
        default: none
        option: on
        option: only, only generates binary read/write, no JSON
    -inherit: <classname>
        default: none
        Required as "RefCounted" for AS binding generation
        Note: in C++ inheriting RefCounted will use SharedPtr for all things

**Examples**

    CastleDBGen C:\MyDdatabase.cdb -lang cpp -ns MyNamespace
    CastleDBGen C:\MyDdatabase.cdb -lang as
    CastleDBGen C:\MyDdatabase.cdb -lang cpp -hd "../HeaderPath/"

## Dependencies
Newtonsoft.JSON via Nuget

## Using custom types

To use CastleDB custom types and their constructors a specific set of rules must be used to make the generator happy. These necessities were irrelevant to Haxe. Eventually a CastleDB fork will account for the necessity.

* The first "constructor" must be the variable type and must not be used in your data.
* All other constructors will be used to construct that data

    enum MyCustom {
        float;
        random(min : float, max : float);
    }
    
The above snippet is valid for a field that is a float and is constructed via a call to random(value,value). The effect of the constructed code would be to call random() to set a float.
