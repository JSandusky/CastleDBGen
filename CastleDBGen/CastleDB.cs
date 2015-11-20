using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CastleDBGen
{
    public static class CastleTypeConverter
    {
        public static object ConvertType(CastleType type, object value)
        {
            switch (type)
            {
            case CastleType.UniqueIdentifier:
            case CastleType.Text:
                return value.ToString();
            case CastleType.Integer:
                return int.Parse(value.ToString());
            case CastleType.Float:
                return float.Parse(value.ToString());
            case CastleType.Boolean:
                return bool.Parse(value.ToString());
            case CastleType.Flags:
                return uint.Parse(value.ToString());
            case CastleType.Enum:
                return int.Parse(value.ToString());
            case CastleType.Image:
                return value.ToString();
            case CastleType.Color:
                return uint.Parse(value.ToString());
            case CastleType.Ref:
                CastleRef refObj = value as CastleRef;
                if (refObj == null)
                    return null;
                return refObj.Referencedstring;
            }
            return null;
        }
    }

    // Utility 
    public sealed class DependencySort : Comparer<CastleSheet>
    {
        public override int Compare(CastleSheet x, CastleSheet y)
        {
            int xRefCt = 0;
            foreach (CastleColumn col in x.Columns)
            {
                if (col.TypeID == CastleType.Ref && col.Key.Equals(y.Name))
                    return 1; //X is greater
                else if (col.TypeID == CastleType.Ref)
                    ++xRefCt;
            }
            int yRefCt = 0;
            foreach (CastleColumn col in y.Columns)
            {
                if (col.TypeID == CastleType.Ref && col.Key.Equals(x.Name))
                    return -1; //Y is greater
                else if (col.TypeID == CastleType.Ref)
                    ++yRefCt;
            }
            
            // Make sure that if only one has refs that we end up moving it back in line
            if (xRefCt > 0 && yRefCt == 0)
                return 1;
            else if (yRefCt > 0 && xRefCt == 0)
                return -1;
            return 0;
        }
    }

    public enum CastleType
    {
        UniqueIdentifier, //0
        Text,        //1
        Boolean,     //2
        Integer,     //3
        Float,       //4
        Enum,        //5
        Ref,         //6
        Image,       //7
        List,        //8
        Custom,      //9
        Flags,       //10
        Color,       //11
        Layer,       //12
        File,        //13
        TilePos,     //14
        TileLayer,   //15
        Dynamic      //16
    }

    public class Opts
    {
        public string name { get; set; }
        public int priority { get; set; }
        public string borderIn { get; set; }
        public string borderOut { get; set; }
        public string borderMode { get; set; }
        public int? value { get; set; }
    }

    public class Tile
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public string t { get; set; }
        public Opts opts { get; set; }
    }

    public class TileSet
    {
        public int stride { get; set; }
        public List<Tile> sets { get; set; }
        public List< Dictionary<string, object> > props { get; set; }
    }

    public class CastleTileSheet
    {
        public string Name { get; set; }
        public List<string> TypeNames = new List<string>();
        public List<TileSet> TileSets = new List<TileSet>();

        public CastleTileSheet(JObject jobject)
        {

        }

        public Type CoalescePropertyType(string name)
        {
            Type curType = null;
            foreach (TileSet set in TileSets)
            {
                foreach (Dictionary<string,object> dict in set.props)
                {
                    foreach (object value in dict.Values)
                    {
                        if (curType == null)
                            curType = value.GetType();
                        else if (curType != value.GetType())
                            return typeof(string);
                    }
                }
            }
            return curType == null ? typeof(string) : curType;
        }
    }

    public class CastleRef
    {
        public CastleLine ReferenceLine;
        public string Referencedstring;
    }

    public class CastleImage
    {
        public string MD5 { get; set; }
        public string Path { get; set; }
    }

    public class CastleTilePos
    {
        public CastleTilePos()
        {
            Width = Height = 1;
        }

        public string File { get; set; }
        public int Size { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class CastleCustomInst
    {
        public int ConstructorIndex;
        public List<string> Parameters = new List<string>();
    }

    public class CastleLine
    {
        public List<object> Values = new List<object>();
    }

    public class CastleCustomCtor
    {
        public string Name;
        public string returnType = "void";
        public List<string> ArgNames = new List<string>();
        public List<string> ArgTypes = new List<string>();

        public string GetCtor(string typeName, int langCode, CastleDB database)
        {
            StringBuilder ret = new StringBuilder();

            ret.Append(Name.Replace("_","::"));
            if (ArgNames.Count > 0)
            {
                ret.Append("(");

                for (int i = 0; i < ArgNames.Count; ++i)
                {
                    if (i > 0)
                        ret.Append(", ");
                    string typeStr = ArgTypes[i];
                    int typeID = 0;
                    if (typeStr.Contains(":"))
                    {
                        string[] words = typeStr.Split(CastleColumn.STRSPLIT, StringSplitOptions.RemoveEmptyEntries);
                        typeID = int.Parse(words[0]);
                        typeStr = words[0];
                    }
                    else
                        typeID = int.Parse(typeStr);

                    switch (langCode)
                    { 
                        case 0: //CPP
                        case 1: //Angelscript
                        switch ((CastleType)typeID)
                        {
                            case CastleType.Boolean:
                                ret.AppendFormat("{0}Array[{1}].GetBool()", typeName, i + 1);
                                break;
                            case CastleType.Integer:
                                ret.AppendFormat("{0}Array[{1}].GetInt()", typeName, i + 1);
                                break;
                            case CastleType.Float:
                                ret.AppendFormat("{0}Array[{1}].GetFloat()", typeName, i + 1);
                                break;
                            case CastleType.Text:
                                ret.AppendFormat("{0}Array[{1}].GetString()", typeName, i + 1);
                                break;
                            case CastleType.Custom:
                                // Custom type constructors need to take JSONValue parameters and process themselves
                                ret.AppendFormat("{0}Array[{1}]", typeName, i + 1);
                                break;
                        }
                        break;
                        case 3: //Lua
                        switch ((CastleType)typeID)
                        {
                            case CastleType.Boolean:
                                ret.AppendFormat("{0}Array[{1}]:GetBool()", typeName, i + 1);
                                break;
                            case CastleType.Integer:
                                ret.AppendFormat("{0}Array[{1}]:GetInt()", typeName, i + 1);
                                break;
                            case CastleType.Float:
                                ret.AppendFormat("{0}Array[{1}]:GetFloat()", typeName, i + 1);
                                break;
                            case CastleType.Text:
                                ret.AppendFormat("{0}Array[{1}]:GetString()", typeName, i + 1);
                                break;
                            case CastleType.Custom:
                                // Custom type constructors need to take JSONValue parameters and process themselves
                                ret.AppendFormat("{0}Array[{1}]", typeName, i + 1);
                                break;
                        }
                        break;
                    }
                }

                ret.Append(")");
            }

            return ret.ToString();
        }
    }

    public class CastleCustom
    {
        public string Name;
        public List<CastleCustomCtor> Constructors = new List<CastleCustomCtor>();
    }

    public class CastleColumn
    {
        public static readonly char[] STRSPLIT = { ':' };
        public static readonly string[] TypeNames = { 
                                                 "Unique Identifier", //0
                                                 "Text",        //1
                                                 "Boolean",     //2
                                                 "Integer",     //3
                                                 "Float",       //4
                                                 "Enum",        //5
                                                 "Ref",         //6
                                                 "Image",       //7
                                                 "List",        //8
                                                 "Custom",      //9
                                                 "Flags",       //10
                                                 "Color",       //11
                                                 "Layer",       //12
                                                 "File",        //13
                                                 "TilePos",     //14
                                                 "TileLayer",   //15
                                                 "Dynamic"      //16
                                                 };

        public string Name = "";
        public string Key = "";
        public CastleType TypeID = CastleType.UniqueIdentifier;
        public List<string> Enumerations = new List<string>();
        public bool Optional = true;
        public bool Display = true;

        public CastleColumn Clone()
        {
            CastleColumn clone = new CastleColumn { Name = this.Name, Key = this.Key, TypeID = this.TypeID };
            return clone;
        }
    }

    public class CastleSheet
    {
        public string Name = "";
        public List<CastleColumn> Columns = new List<CastleColumn>();
        public CastleColumn IDColumn;
        public List<CastleLine> Lines = new List<CastleLine>();

        public int IndexOfID()
        {
            if (IDColumn == null)
                return -1;
            return Columns.IndexOf(IDColumn);
        }

        public CastleSheet Clone()
        {
            CastleSheet clone = new CastleSheet { Name = this.Name };
            foreach (CastleColumn col in this.Columns)
                clone.Columns.Add(col.Clone());
            return clone;
        }

        public void AddColumn(CastleColumn col)
        {
            if (col.TypeID == CastleType.UniqueIdentifier)
                IDColumn = col;
            Columns.Add(col);
        }

        public string GetKeyName()
        {
            if (IDColumn != null)
                return IDColumn.Name;
            else
                return "";
        }

        public bool HasReferences(CastleDB database)
        {
            foreach (CastleColumn col in Columns)
            {
                if (col.TypeID == CastleType.Ref)
                    return true;
                else if (col.TypeID == CastleType.List)
                {
                    string searchSheet = string.Format("{0}@{1}", Name, col.Name);
                    CastleSheet dbSheet = database.Sheets.FirstOrDefault(s => s.Name.Equals(searchSheet));
                    if (dbSheet != null && dbSheet.HasReferences(database))
                        return true;
                }
            }
            return false;
        }
    }

    public class CastleDB
    {
        public List<CastleSheet> Sheets = new List<CastleSheet>();
        public List<CastleCustom> CustomTypes = new List<CastleCustom>();

        JObject root;
        public CastleDB(string file)
        {
            root = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(file));
            JArray customArray = root.Property("customTypes").Value as JArray;
            LoadCustomTypes(customArray);
            JArray sheetsArray = root.Property("sheets").Value as JArray;
            LoadSheets(sheetsArray);
        }

        void LoadSheets(JArray sheetsArray)
        {
            if (sheetsArray == null)
                throw new Exception("Unexpected data for sheets");
            foreach (JObject sheet in sheetsArray)
            {
                string sheetName = sheet.Property("name").Value.ToString();
                JProperty cols = sheet.Property("columns");
                //TODO create CastleSheet and CastleColumns
                CastleSheet newSheet = new CastleSheet { Name = sheetName };
                if (cols.Value is JArray)
                {
                    JArray colsArray = cols.Value as JArray;
                    foreach (JObject column in colsArray)
                    {
                        string typeID = column.Property("typeStr").Value.ToString();
                        string key = "";
                        string colName = column.Property("name").Value.ToString();
                        string enumText = "";
                        if (typeID.Contains(':'))
                        {
                            string[] words = typeID.Split(CastleColumn.STRSPLIT, StringSplitOptions.RemoveEmptyEntries);
                            typeID = words[0];
                            if (words[1].Contains(','))
                                enumText = words[1];
                            else
                                key = words[1];
                        }
                        CastleColumn newColumn = new CastleColumn { Name = colName, TypeID = (CastleType)Enum.Parse(typeof(CastleType), typeID), Key = key };
                        if (enumText.Length > 0)
                            newColumn.Enumerations.AddRange(enumText.Split(','));
                        newSheet.AddColumn(newColumn);
                    }
                }
                Sheets.Add(newSheet);
            }

            // Iterate again to fill data
            foreach (JObject sheet in sheetsArray)
            {
                string sheetName = sheet.Property("name").Value.ToString();
                CastleSheet targetSheet = Sheets.FirstOrDefault(p => p.Name.Equals(sheetName));
                if (targetSheet == null)
                    continue;
                JProperty lines = sheet.Property("lines");
                if (lines.Value is JArray)
                {
                    JArray linesArray = lines.Value as JArray;
                    if (linesArray != null)
                        FillSheetData(targetSheet, linesArray);
                }
            }

            // Now that all "base" data is loaded, resolve the references
            foreach (CastleSheet sheet in Sheets)
            {
                foreach (CastleLine line in sheet.Lines)
                {
                    for (int i = 0; i < sheet.Columns.Count; ++i)
                    {
                        CastleColumn col = sheet.Columns[i];
                        if (col.TypeID == CastleType.Ref)
                        {
                            string text = line.Values[i].ToString();
                            CastleSheet lookupSheet = Sheets.FirstOrDefault(s => s.Name.Equals(col.Key));
                            if (lookupSheet != null)
                            {
                                line.Values[i] = new CastleRef { 
                                    Referencedstring = text, ReferenceLine = 
                                    lookupSheet.Lines.FirstOrDefault(l => l.Values[lookupSheet.IndexOfID()].Equals(text)) };
                            }
                            else
                                line.Values[i] = null;
                        }
                    }
                }
            }
        }

        void FillSheetData(CastleSheet targetSheet, JArray linesArray)
        {
            foreach (JObject line in linesArray)
            {
                CastleLine newLine = new CastleLine();
                foreach (JProperty property in line.Properties())
                {
                    string propName = property.Name;
                    CastleColumn col = targetSheet.Columns.FirstOrDefault(c => c.Name.Equals(propName));
                    if (col == null)
                        continue;
                    if (col.TypeID == CastleType.List)
                    {
                        string newSheetName = string.Format("{0}@{1}", targetSheet.Name, col.Name);
                        CastleSheet newTarget = Sheets.FirstOrDefault(s => s.Name.Equals(newSheetName)).Clone();
                        if (newTarget != null)
                            FillSheetData(newTarget, property.Value as JArray);
                        newLine.Values.Add(newTarget);
                    }
                    else if (col.TypeID == CastleType.Custom)
                    {
                        CastleCustom customType = CustomTypes.FirstOrDefault(c => c.Name.Equals(col.Key));
                        if (customType != null)
                        {
                            JArray valArray = property.Value as JArray;
                            CastleCustomInst inst = new CastleCustomInst();
                            for (int i = 0; i < valArray.Count; ++i)
                            {
                                JToken token = valArray[i];
                                string tokenText = token.ToString();
                                if (i == 0)
                                    inst.ConstructorIndex = int.Parse(tokenText);
                                else
                                    inst.Parameters.Add(tokenText);
                            }
                        }
                        else
                            throw new Exception("Unable to find custom type: " + col.Key);
                    }
                    else if (col.TypeID == CastleType.Image)
                    {
                        string strValue = property.Value.ToString();
                        if (strValue.Contains(":"))
                        {
                            string[] words = strValue.Split(':');
                            newLine.Values.Add(new CastleImage { MD5 = words[0], Path = words[1] });
                        }
                        else
                            newLine.Values.Add(new CastleImage { MD5 = strValue, Path = "" });
                    }
                    else if (col.TypeID == CastleType.TilePos)
                    {
                        JObject tileObj = property.Value as JObject;
                        if (tileObj != null)
                        {
                            newLine.Values.Add(new CastleTilePos { 
                                File = tileObj.Property("file").Value.ToString(), 
                                Size = int.Parse(tileObj.Property("size").Value.ToString()),
                                X = int.Parse(tileObj.Property("x").Value.ToString()),
                                Y = int.Parse(tileObj.Property("y").Value.ToString()),
                                Width = tileObj.Property("width") != null ? int.Parse(tileObj.Property("width").Value.ToString()) : 0,
                                Height = tileObj.Property("height") != null ? int.Parse(tileObj.Property("height").Value.ToString()) : 0
                            });
                        }
                    }
                    else if (col.TypeID == CastleType.Layer)
                    {

                    }
                    else if (col.TypeID == CastleType.TileLayer)
                    {

                    }
                    else if (col.TypeID == CastleType.Dynamic)
                    {
                        // Just straight add the JToken to it
                        newLine.Values.Add(property.Value);
                    }
                    else if (col.TypeID == CastleType.Ref)
                        newLine.Values.Add(property.Value.ToString());
                    else
                        newLine.Values.Add(property.Value.ToString());
                }
            }
        }

        void LoadCustomTypes(JArray customArray)
        {
            if (customArray == null)
                throw new Exception("No data found for custom types");
            foreach (JObject obj in customArray)
            {
                string typeName = obj.Property("name").Value.ToString();
                JArray casesArray = obj.Property("cases").Value as JArray;
                CastleCustom customType = new CastleCustom { Name = typeName };
                if (casesArray != null)
                {
                    foreach (JObject ctor in casesArray)
                    {
                        string ctorName = ctor.Property("name").Value.ToString();
                        JArray argsArray = ctor.Property("args").Value as JArray;
                        
                        CastleCustomCtor customCtor = new CastleCustomCtor { Name = ctorName };

                        if (argsArray != null)
                        {
                            foreach (JObject arg in argsArray)
                            {
                                string typeStr = arg.Property("typeStr").Value.ToString();
                                string argName = arg.Property("name").Value.ToString();
                                customCtor.ArgNames.Add(argName);
                                customCtor.ArgTypes.Add(typeStr);
                            }
                        }
                        customType.Constructors.Add(customCtor);
                    }
                }
                if (customType.Constructors.Count > 0)
                    CustomTypes.Add(customType);
            }
        }

        public void Save(string fileName)
        {
            Newtonsoft.Json.Linq.JObject root = new JObject();
            JArray sheetsObject = new JArray();
            root.Add(new JProperty("sheets", sheetsObject));
            foreach (CastleSheet sheet in Sheets)
                SaveSheet(sheetsObject, sheet);

            JArray customObj = new JArray();
            root.Add(new JProperty("customTypes", customObj));
            foreach (CastleCustom custom in CustomTypes)
                SaveCustomTypes(customObj, custom);

            root.WriteTo(new Newtonsoft.Json.JsonTextWriter(new System.IO.StreamWriter(fileName)));
        }

        void SaveSheet(JArray sheetArray, CastleSheet sheet)
        {
            JObject sheetObject = new JObject();
            sheetArray.Add(sheetObject);

            sheetObject.Add(new JProperty("name", sheet.Name));
            JArray columnsArray = new JArray();
            sheetObject.Add(new JProperty("columns", columnsArray));
            foreach (CastleColumn col in sheet.Columns)
            {
                JObject column = new JObject();
                if (col.Key.Length > 0)
                    column.Add(new JProperty("typeStr", string.Format("{0}:{1}", (int)col.TypeID, col.Key)));
                else
                    column.Add(new JProperty("typeStr", ((int)col.TypeID).ToString()));
                column.Add(new JProperty("name", col.Name));
                if (col.Optional)
                    column.Add(new JProperty("opt", true));
                columnsArray.Add(column);
            }

            JArray linesArray = new JArray();
            SaveSheet(linesArray, sheet);
            sheetObject.Add(new JProperty("lines", linesArray));
        }

        void SaveCustomTypes(JArray customArray, CastleCustom customType)
        {
            JObject typeObj = new JObject();
            typeObj.Add(new JProperty("name", customType.Name));
            JArray casesArray = new JArray();
            foreach (CastleCustomCtor ctor in customType.Constructors)
            {
                JObject ctorObj = new JObject();
                ctorObj.Add(new JProperty("name", ctor.Name));
                JArray argsArray = new JArray();
                ctorObj.Add(new JProperty("args", argsArray));
                for (int i = 0; i < ctor.ArgNames.Count; ++i)
                {
                    JObject argObject = new JObject();
                    argObject.Add(new JProperty("name", ctor.ArgNames[i]));
                    argObject.Add(new JProperty("typeStr", ctor.ArgTypes[i]));
                    argsArray.Add(argObject);
                }
                casesArray.Add(ctorObj);
            }
            typeObj.Add(new JProperty("cases", casesArray));
        }

        void SaveSheetLines(JArray holderArray, CastleSheet sheet)
        {
            foreach (CastleLine line in sheet.Lines)
                SaveLine(holderArray, line, sheet);
        }

        void SaveLine(JArray linesArray, CastleLine line, CastleSheet sheet)
        {
            JObject lineObject = new JObject();
            linesArray.Add(lineObject);

            for (int i = 0; i < sheet.Columns.Count; ++i)
            {
                switch (sheet.Columns[i].TypeID)
                {
                case CastleType.List:
                    JArray subList = new JArray();
                    CastleSheet subSheet = line.Values[i] as CastleSheet;
                    if (subSheet != null)
                        SaveSheetLines(subList, subSheet);
                    lineObject.Add(new JProperty(sheet.Columns[i].Name, subList));
                    break;
                case CastleType.Custom:
                    CastleCustomInst ci = line.Values[i] as CastleCustomInst;
                    JArray ctorArray = new JArray();
                    if (ci == null)
                    {
                        
                    }
                    lineObject.Add(new JProperty(sheet.Columns[i].Name, ctorArray));
                    break;
                default:
                    lineObject.Add(new JProperty(sheet.Columns[i].Name, CastleTypeConverter.ConvertType(sheet.Columns[i].TypeID, line.Values[i])));
                    break;
                }
            }
        }
    }
}
