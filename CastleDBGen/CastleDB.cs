using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CastleDBGen
{
    // Utility class for sorting CastleSheets based on their references to their other sheets
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

    public class CastleRef
    {
        public CastleLine ReferenceLine;
        public string ReferencedString;
    }

    public class CastleLine
    {
        public List<object> Values = new List<object>();
    }

    public class CastleCustomCtor
    {
        public String Name;
        public List<string> ArgNames = new List<string>();
        public List<string> ArgTypes = new List<string>();
    }

    public class CastleCustom
    {
        public String Name;
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

        public bool HasReferences()
        {
            foreach (CastleColumn col in Columns)
                if (col.TypeID == CastleType.Ref)
                    return true;
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
                                    ReferencedString = text, ReferenceLine = 
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
                        string newSheetName = String.Format("{0}@{1}", targetSheet.Name, col.Name);
                        CastleSheet newTarget = Sheets.FirstOrDefault(s => s.Name.Equals(newSheetName)).Clone();
                        if (newTarget != null)
                            FillSheetData(newTarget, property.Value as JArray);
                        newLine.Values.Add(newTarget);
                    }
                    else if (col.TypeID == CastleType.Ref)
                        // Push a string for now
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
                        JArray argsArray = obj.Property("args").Value as JArray;
                        
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
    }
}
