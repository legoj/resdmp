using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Collections;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace EnumRes
{
    class Program
    {
        private static string ERBLDNUM ="resdmp.01312013";
        private static XmlTextWriter xml = null;
        private static Opt opt = null;
        private static string currPath = null;
        private static string outObj;

        public const uint RT_CURSOR = 1;
        public const uint RT_BITMAP = 2;
        public const uint RT_ICON = 3;
        public const uint RT_MENU = 4;
        public const uint RT_DIALOG = 5;
        public const uint RT_STRING = 6;
        public const uint RT_FONTDIR = 7;
        public const uint RT_FONT = 8;
        public const uint RT_ACCELERATOR = 9;
        public const uint RT_RCDATA = 10;
        public const uint RT_MESSAGETABLE = 11;
        public const uint RT_GROUP_CURSOR = 12;
        public const uint RT_GROUP_ICON = 14;
        public const uint RT_VERSION = 16;
        public const uint RT_DLGINCLUDE = 17;
        public const uint RT_PLUGPLAY = 19;
        public const uint RT_VXD = 20;
        public const uint RT_ANICURSOR = 21;
        public const uint RT_ANIICON = 22;
        public const uint RT_HTML = 23;

        private static string[] RT_TYPES = {"", "cursor", "bitmap", 
                                                "icon", "menu", "dialog", 
                                                "string", "fontdir", "font",
                                                "accelerator","rcdata","messagetable",
                                                "groupcursor","","groupicon","","version",
                                                "dlginclude","","plugplay","vxd",
                                                "anicursor","aniicon","html"
                                           };

        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool EnumResourceNamesWithName(IntPtr hModule, string lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool EnumResourceNamesWithID(IntPtr hModule, uint lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpszType, IntPtr lpName, EnumResLangDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("Kernel32.dll", EntryPoint = "FindResourceExW", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, UInt16 wLanguage);

        [DllImport("Kernel32.dll", EntryPoint = "LockResource")]
        static extern IntPtr LockResource(IntPtr hGlobal);

        [DllImport("Kernel32.dll", EntryPoint = "LoadResource", SetLastError = true)]
        static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResource);

        [DllImport("Kernel32.dll", EntryPoint = "SizeofResource", SetLastError = true)]
        static extern uint SizeofResource(IntPtr hModule, IntPtr hResource);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("User32.dll")]
        public static extern IntPtr LoadImage(IntPtr hInstance, int uID, uint type, int width, int height, int load);

        [DllImport("User32.dll")]
        public static extern IntPtr LoadBitmap(IntPtr hInstance, int uID);

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MESSAGE_RESOURCE_DATA
        {
            public uint NumberOfBlocks;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MESSAGE_RESOURCE_BLOCK
        {
            public uint LowId;
            public uint HighId;
            public uint OffsetToEntries;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MESSAGE_RESOURCE_ENTRY
        {
            public ushort Length;
            public ushort Flags;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.I1)]
            public byte[] Text;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct VS_FIXEDFILEINFO
        {
            public uint dwSignature;
            public uint dwStrucVersion;
            public uint dwFileVersionMS;
            public uint dwFileVersionLS;
            public uint dwProductVersionMS;
            public uint dwProductVersionLS;
            public uint dwFileFlagsMask;
            public uint dwFileFlags;
            public uint dwFileOS;
            public uint dwFileType;
            public uint dwFileSubtype;
            public uint dwFileDateMS;
            public uint dwFileDateLS;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RESOURCE_HEADER
        {
            public UInt16 wLength;
            public UInt16 wValueLength;
            public UInt16 wType;
        }
        //delegates
        private delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
        private delegate bool EnumResTypeDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
        private delegate bool EnumResLangDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, UInt16 wIDlang, IntPtr lParam);


        private static string GET_RESOURCE_DESC(uint resId)
        {
            return (resId < RT_TYPES.Length) ? RT_TYPES[resId] : resId.ToString();
        }
        private static bool IS_INTRESOURCE(IntPtr value)
        {
            return ushort.MaxValue > (uint)value;
        }
        private static uint GET_RESOURCE_ID(IntPtr value)
        {
            if (IS_INTRESOURCE(value))
                return (uint)value;
            throw new System.NotSupportedException("value is not an ID!");
        }
        private static string GET_RESOURCE_NAME(IntPtr value)
        {
            if (IS_INTRESOURCE(value))
                return value.ToString();
            return Marshal.PtrToStringUni((IntPtr)value);
        }

        private static UInt16 HiWord(UInt32 value)
        {
            return (UInt16)((value & 0xFFFF0000) >> 16);
        }

        private static UInt16 LoWord(UInt32 value)
        {
            return (UInt16)(value & 0x0000FFFF);
        }

        private static IntPtr NewPtr(IntPtr ptr, int amount)
        {
            return new IntPtr(ptr.ToInt32() + amount);
        }
        private static IntPtr Align(Int32 ptr)
        {
            int v = (ptr + 3) & ~3;
            return new IntPtr(v);
        }
        static bool EnumResHandler(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, UInt16 wIDlang, IntPtr lParam)
        {
            try
            {
                IntPtr hRes = FindResourceEx(hModule, lpszType, lpszName, wIDlang);
                if (hRes == null) return false;

                uint uResSize = SizeofResource(hModule, hRes);
                if (uResSize == 0) return false;

                IntPtr hLddRes = LoadResource(hModule, hRes);
                if (hLddRes == null) return false;

                IntPtr hLckRes = LockResource(hLddRes);
                if (hLckRes == null) return false;

                uint resId = GET_RESOURCE_ID(lpszName);
                //string name = GET_RESOURCE_NAME(lpszName);
                //Console.WriteLine("lang={0} block:{1} size:{2}", wIDlang, resId, uResSize);

                uint uResType = (uint)lpszType;

                switch (uResType)
                {
                    case RT_STRING:
                        WriteBlock("string", wIDlang.ToString(), resId.ToString(), uResSize.ToString());
                        Console.WriteLine("[string lang={0} block:{1} size:{2}]", wIDlang, resId, uResSize);
                        short uEntry = 0;
                        int uBase = (int)lpszName;
                        uBase = (uBase - 1) * 16;

                        while (uResSize > 0)
                        {
                            uEntry = Marshal.ReadInt16(hLckRes);
                            hLckRes = NewPtr(hLckRes, 2);
                            uResSize -= 2;

                            if (uEntry > 0)
                            {
                                String resValue = Marshal.PtrToStringUni(hLckRes, uEntry);
                                hLckRes = NewPtr(hLckRes, uEntry * 2);
                                uResSize = uResSize - (uint)(uEntry * 2);
                                Console.WriteLine("{0} \t{1}", uBase, resValue);
                                WriteText(uBase.ToString(), resValue);
                            }
                            uBase++;
                        }
                        EndElement();
                        Console.WriteLine();
                        break;
                    case RT_MESSAGETABLE:
                        WriteBlock("message", wIDlang.ToString(), resId.ToString(), uResSize.ToString());
                        Console.WriteLine("[message lang={0} block:{1} size:{2}]", wIDlang, resId, uResSize);
                        MESSAGE_RESOURCE_DATA MRD;
                        MESSAGE_RESOURCE_ENTRY MRE;
                        MESSAGE_RESOURCE_BLOCK MRB;
                        IntPtr pMRD, pMRE, pMRB;

                        pMRD = hLckRes;
                        MRD = (MESSAGE_RESOURCE_DATA)Marshal.PtrToStructure(pMRD, typeof(MESSAGE_RESOURCE_DATA));
                        if (MRD.NumberOfBlocks == 0) break;

                        uint j = 0;
                        pMRB = NewPtr(pMRD, sizeof(Int32));//NewPtr(pMRD, IntPtr.Size);
                        for (int i = 0; i < MRD.NumberOfBlocks; ++i)
                        {
                            MRB = (MESSAGE_RESOURCE_BLOCK)Marshal.PtrToStructure(pMRB, typeof(MESSAGE_RESOURCE_BLOCK));
                            pMRE = NewPtr(pMRD, (int)MRB.OffsetToEntries);
                            MRE = (MESSAGE_RESOURCE_ENTRY)Marshal.PtrToStructure(pMRE, typeof(MESSAGE_RESOURCE_ENTRY));
                            for (j = MRB.LowId; j <= MRB.HighId; ++j)
                            {
                                int flSize = sizeof(ushort) * 2;
                                IntPtr pVal = NewPtr(pMRE, flSize);
                                int valLen = MRE.Length - (flSize);
                                if (MRE.Flags <= 1)
                                {
                                    if (valLen > 0)
                                    {
                                        string v = MRE.Flags == 0 ? Marshal.PtrToStringAnsi(pVal, valLen) : Marshal.PtrToStringUni(pVal, valLen / 2);
                                        //string v = Marshal.PtrToStringAnsi(pVal, valLen);
                                        Console.WriteLine("{0} \t{1}", j, v);

                                        WriteText(j.ToString(), v);
                                    }
                                    pMRE = NewPtr(pMRE, MRE.Length);
                                    MRE = (MESSAGE_RESOURCE_ENTRY)Marshal.PtrToStructure(pMRE, typeof(MESSAGE_RESOURCE_ENTRY));
                                }

                            }
                            pMRB = NewPtr(pMRB, sizeof(Int32) * 3);//NewPtr(pMRB, IntPtr.Size * 3);
                        }
                        EndElement();
                        Console.WriteLine();
                        break;

                    case RT_VERSION:

                        IntPtr pCurrent = hLckRes;
                        RESOURCE_HEADER RH = (RESOURCE_HEADER)Marshal.PtrToStructure(pCurrent, typeof(RESOURCE_HEADER));
                        pCurrent = NewPtr(pCurrent, Marshal.SizeOf(RH));
                        string hdrName = Marshal.PtrToStringUni(pCurrent);
                        pCurrent = Align(pCurrent.ToInt32() + (hdrName.Length + 1) * Marshal.SystemDefaultCharSize);
                        VS_FIXEDFILEINFO VS = (VS_FIXEDFILEINFO)Marshal.PtrToStructure(pCurrent, typeof(VS_FIXEDFILEINFO));
                        ushort major = HiWord(VS.dwFileVersionMS);
                        ushort minor = LoWord(VS.dwFileVersionMS);
                        ushort build = HiWord(VS.dwFileVersionLS);
                        ushort release = LoWord(VS.dwFileVersionLS);

                        IntPtr pVar = Align(pCurrent.ToInt32() + RH.wValueLength);
                        int tX = pVar.ToInt32();
                        int rX = hLckRes.ToInt32() + RH.wLength;

                        Console.WriteLine("FixedFileVersion \t{0}.{1}.{2}.{3}", major, minor, build, release);

                        while (pVar.ToInt32() < (hLckRes.ToInt32() + RH.wLength))
                        {
                            RH = (RESOURCE_HEADER)Marshal.PtrToStructure(pVar, typeof(RESOURCE_HEADER));
                            IntPtr pKey = new IntPtr(pVar.ToInt32() + Marshal.SizeOf(RH));
                            String key = Marshal.PtrToStringUni(pKey);

                            pVar = Align(pKey.ToInt32() + (key.Length + 1) * Marshal.SystemDefaultCharSize);
                            switch (key)
                            {
                                case "StringFileInfo":

                                    while (pVar.ToInt32() < (pCurrent.ToInt32() + RH.wLength))
                                    {
                                        String k = Marshal.PtrToStringUni(pKey);
                                        pVar = Align(pVar.ToInt32() + RH.wLength);
                                    }
                                    break;
                                default:
                                    while (pVar.ToInt32() < (pCurrent.ToInt32() + RH.wLength))
                                    {
                                        String ky = Marshal.PtrToStringUni(pKey);
                                        pVar = Align(pVar.ToInt32() + RH.wLength);
                                    }
                                    break;
                            }
                            Console.WriteLine("TTTT: " + key);
                        }
                        
                        break;


                    case RT_MENU:

                        break;
                    case RT_DIALOG:

                        break;

                    case RT_ACCELERATOR:
                    case RT_BITMAP:
                    case RT_ICON:
                    case RT_CURSOR:                    
                    case RT_FONTDIR:
                    case RT_FONT:
                    case RT_RCDATA:
                    case RT_GROUP_CURSOR:
                    case RT_GROUP_ICON:
                    case RT_DLGINCLUDE:
                    case RT_PLUGPLAY:
                    case RT_VXD:
                    case RT_ANICURSOR:
                    case RT_ANIICON:
                    case RT_HTML:
                        break;
                    default:
                        break;
                }
                xml.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception@EnumResHandler:" + e.Message);
                return false;
            }
            return true;
        }


        static bool EnumResNames(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {

            EnumResLangDelegate resLangDel = new EnumResLangDelegate(EnumResHandler);
            EnumResourceLanguages(hModule, lpszType, lpszName, resLangDel, IntPtr.Zero);

            return true;
        }

        static bool EnumResTypes(IntPtr hModule, IntPtr lpszType, IntPtr lParam)
        {
            if (IS_INTRESOURCE(lpszType))
            {
                Console.WriteLine("Type: {0}", GET_RESOURCE_DESC((uint)lpszType));
                EnumResNameDelegate resNameDel = new EnumResNameDelegate(EnumResNames);
                EnumResourceNamesWithID(hModule, (uint)lpszType, resNameDel, IntPtr.Zero);
            }
            else
            {
                Console.WriteLine("Type: {0}", Marshal.PtrToStringAnsi(lpszType));
            }

            return true;
        }

        //[PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        static bool IsManaged(FileInfo inFile)
        {
            try
            {

                Assembly asm = Assembly.LoadFrom(inFile.FullName);
                //AssemblyName.GetAssemblyName(inFile.FullName);
                
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message.ToString() + "(CLR: " + Environment.Version.ToString() + ")");
                
                return false;
            }
            return true; 
        }

        //not used for now
        static bool IsDotNetAssembly(FileInfo inFile)
        {
            using (FileStream fs = new FileStream(inFile.FullName, FileMode.Open, FileAccess.Read)){
                try{
                    using (BinaryReader binReader = new BinaryReader(fs)){
                        try
                        {

                            fs.Position = 0x3C; //PE Header start offset
                            uint headerOffset = binReader.ReadUInt32();

                            fs.Position = headerOffset + 0x18;
                            UInt16 magicNumber = binReader.ReadUInt16();

                            int dictionaryOffset;
                            switch (magicNumber)
                            {
                                case 0x010B: dictionaryOffset = 0x60; break;
                                case 0x020B: dictionaryOffset = 0x70; break;
                                default:
                                    throw new Exception("Invalid Image Format");
                            }

                            fs.Position = headerOffset + 0x18 + dictionaryOffset + 0x70;


                            //Read the value
                            uint rva15value = binReader.ReadUInt32();
                            return rva15value != 0;
                        }
                        finally
                        {
                            binReader.Close();
                        }
                    }
             }
                finally
                {
                    fs.Close();
                }
            }
        }
        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        static void DumpW32File(FileInfo inFile)
        {

            try
            {
                Console.WriteLine("DumpW32File: " + inFile.Name );
                IntPtr hMod = LoadLibraryEx(inFile.FullName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
                StartXML(inFile);                
                /*
                if (!EnumResourceTypes(hMod, new EnumResTypeDelegate(EnumResTypes), IntPtr.Zero))
                {
                        Console.WriteLine("gle: {0}", Marshal.GetLastWin32Error());
                }
                 */
                EnumResourceNamesWithID(hMod, RT_STRING, new EnumResNameDelegate(EnumResNames), IntPtr.Zero);
                EnumResourceNamesWithID(hMod, RT_MESSAGETABLE, new EnumResNameDelegate(EnumResNames), IntPtr.Zero);
                //EnumResourceNamesWithID(hMod, RT_VERSION, new EnumResNameDelegate(EnumResNames), IntPtr.Zero);
                EndXML();
                FreeLibrary(hMod);
            }
            catch (ApplicationException ex)
            {
                System.Console.WriteLine(ex.Message);
                return;
            }

        }
        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        static void DumpNetFile(FileInfo inFile)
        {
            try
            {
                Console.WriteLine("DumpNetFile: " + inFile.Name);
                StartXML(inFile);
                Assembly fileAss = Assembly.LoadFile(inFile.FullName);
                string[] resourceNames = fileAss.GetManifestResourceNames();
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                foreach (string resource in resourceNames)
                {

                    if (resource.EndsWith(".resources")) // It's a embedded Resource file which itself can contain resources
                    {
                        Console.WriteLine("[string block:" + resource + "]");

                        ResourceReader resReader = new ResourceReader(fileAss.GetManifestResourceStream(resource));
                        IDictionaryEnumerator resEnum = resReader.GetEnumerator();
                        WriteBlock("string", null, resource.Replace(".resources", ""), null);
                        while (resEnum.MoveNext())
                        {
                            string key = (string)resEnum.Key;
                            //Log("Key: " + key);
                            if (!key.StartsWith(">>"))
                            {
                                string resourceType = String.Empty;
                                byte[] resourceData;
                                resReader.GetResourceData(key, out resourceType, out resourceData);
                                resourceType = resourceType.Split(',')[0];
                                //Log("Type: " + resourceType);
                                // Log("    dumping resource: name={0}, type={1}", resEnum.Key, resourceType);
                                if (resourceType.Equals("ResourceTypeCode.String"))
                                {
                                    WriteText(key, (string)resEnum.Value);
                                    //Log(key + " = " + (string)resEnum.Value);//enc.GetString(resourceData,));
                                    // Log("{0} = {1}", key, BytesToString(resourceData));
                                    Console.WriteLine(key + "\t " + (string)resEnum.Value);
                                }
                            }
                            Console.WriteLine();
                        }
                        EndElement();
                        //Log("*******************************************");
                        Console.WriteLine();
                    }
                }
                EndXML();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return;
            }
        }


        static void ConvCmxi(FileInfo inFile)
        {
                Console.WriteLine("ConvCxmiFile: " + inFile.Name);                
                XmlDocument cxmi = new XmlDocument();
                cxmi.Load(inFile.FullName);
                XmlNode doc = cxmi.DocumentElement;
                if ("CXMI".Equals(doc.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (doc.HasChildNodes)
                    {
                        StartXML(inFile);
                        foreach (XmlNode xN in doc.ChildNodes)
                        {
                            XmlNode xLngOrGP = xN;
                            string lang = "en-US"; //default
                            if ("Lang".Equals(xLngOrGP.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                lang = xLngOrGP.Attributes["Id"].Value;
                                
                                xLngOrGP = xLngOrGP.FirstChild;
                            }
                            if ("GlobalProperties".Equals(xLngOrGP.Name))
                            {
                                lang = CultureInfo.CreateSpecificCulture(lang).LCID.ToString();
                                WriteBlock("string", lang, xLngOrGP.Name, null);
                                Console.WriteLine("[string lang={0} block={1}]", lang, xLngOrGP.Name);
                                foreach (XmlNode xNode in xLngOrGP.ChildNodes)
                                {
                                    if ("Property".Equals(xNode.Name))
                                    {
                                        string pName = xNode.Attributes["Id"].Value;
                                        string pValu = xNode.HasChildNodes ? xNode.FirstChild.InnerText : null;
                                        WriteText(pName, pValu);
                                        Console.WriteLine("{0} \t{1}", pName, pValu);
                                    }
                                }
                            }
                        }
                        EndXML();
                    }

                }
                
        }

        static void ConvXMLFile(FileInfo inFile)
        {            
            XmlDocument xml = new XmlDocument();
            xml.Load(inFile.FullName);
            XmlNode doc = xml.DocumentElement;

            //license server format
            //<strings><s id="KEY">VAL</s>....</strings>
            switch (doc.Name)
            {
                case "strings":            
                    if (IsValidCultureName(inFile.Directory.Name) && doc.HasChildNodes)
                    {
                        Console.WriteLine("ConvLicXML: " + inFile.Name);
                        string lang = GetLangId(inFile.Directory.Name);
                        StartXML(inFile);
                        WriteBlock("string", lang, doc.Name, null);
                        Console.WriteLine("[string lang={0} block={1}]", lang, doc.Name);
                        foreach (XmlNode xN in doc.ChildNodes)
                        {
                            if ("s".Equals(xN.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                string pName = xN.Attributes["id"].Value;
                                string pValu = xN.HasChildNodes ? xN.FirstChild.InnerText : null;
                                WriteText(pName, pValu);
                                Console.WriteLine("{0} \t{1}", pName, pValu);
                            }
                        }
                        EndXML();
                    }
                    break;

                case "Autorun":
                    if (doc.HasChildNodes)
                    {
                        Console.WriteLine("ConvAutorunXML: " + inFile.Name);
                        string pDir = IsValidCultureName(inFile.Directory.Name) ? inFile.Directory.Name : "en";
                        string lgId = GetLangId(pDir);
                        const string _SCREEN = "Screen";
                        const string _BUTTON = "Button";
                        const string _TEXT = "Text";
                        StartXML(inFile);

                        foreach (XmlNode xN in doc.ChildNodes) //Screen nodes
                        {
                            if (_SCREEN.Equals(xN.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                string scrnId = xN.Attributes["ID"].Value;
                                string scrnTxt = xN.Attributes[_TEXT].Value;
                                WriteBlock("string", lgId, scrnId, null);
                                Console.WriteLine("[string lang={0} block={1}]", lgId, scrnId);
                                string pName = scrnId + "." + _TEXT;
                                WriteText(pName, scrnTxt);
                                Console.WriteLine("{0} \t{1}", pName, scrnTxt);
                                //int cCnt = xN.ChildNodes.Count;
                                //if (cCnt != 7) Console.WriteLine("Button elements should be 7!");
                                int btnId = 1;
                                foreach (XmlNode xBtn in xN.ChildNodes) //Screen nodes
                                {
                                    if (_BUTTON.Equals(xBtn.Name, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        pName = scrnId + "." + _BUTTON + btnId++;
                                        WriteTest_ButtonAttr(xBtn, _TEXT, pName);
                                        WriteTest_ButtonAttr(xBtn, "Description", pName);
                                        WriteTest_ButtonAttr(xBtn, "Action", pName);
                                        WriteTest_ButtonAttr(xBtn, "PreAction", pName);
                                        WriteTest_ButtonAttr(xBtn, "PostAction", pName);
                                        WriteTest_ButtonAttr(xBtn, "WoW64PreAction", pName);
                                        WriteTest_ButtonAttr(xBtn, "WoW64PostAction", pName);
                                        WriteTest_ButtonAttr(xBtn, "Argument", pName);
                                        WriteTest_ButtonAttr(xBtn, "PreArgument", pName);
                                        WriteTest_ButtonAttr(xBtn, "PostArgument", pName);
                                        WriteTest_ButtonAttr(xBtn, "WoW64Argument", pName);
                                        WriteTest_ButtonAttr(xBtn, "WoW64PreArgument", pName);
                                        WriteTest_ButtonAttr(xBtn, "WoW64PostArgument", pName);
                                        WriteTest_ButtonAttr(xBtn, "Enabled", pName);
                                        WriteTest_ButtonAttr(xBtn, "Visible", pName);
                                        WriteTest_ButtonAttr(xBtn, "SelectedImage", pName);
                                        WriteTest_ButtonAttr(xBtn, "Image", pName);
                                    }
                                }
                                EndElement();
                            }

                        }
                        EndXML();
                    }
                    break;
           // case "other format": //other formats
            }
        }

        private static void WriteTest_ButtonAttr(XmlNode xBtn, string atName, string scName)
        {
            string pValu = GetAttrString(xBtn, atName);
            if (pValu != null)
            {
                string pName = scName + "." + atName;
                WriteText(pName, pValu);
                Console.WriteLine("{0} \t{1}", pName, pValu);
            }
        }

        private static string GetAttrString(XmlNode xN, string atName)
        {
            XmlAttribute xa = xN.Attributes[atName];
            return (xa == null) ? null : xa.Value;            
        }

        static void ConvIdtFile(FileInfo inFile)
        {
            Console.WriteLine("ConvIdtFile: " + inFile.Name );
            Encoding fileEncoding = opt.GetEncoding(inFile.Directory.Name.ToLower());
            Console.WriteLine("Encoding: " + fileEncoding.EncodingName);
            StreamReader re = new StreamReader(inFile.FullName, fileEncoding);
            string txtLine = null;
            string lang = "1033";
            string block = Path.GetFileNameWithoutExtension(inFile.Name);
            //Console.WriteLine("Parent: " + inFile.Directory.Name);
            try { lang = CultureInfo.CreateSpecificCulture(inFile.Directory.Name).LCID.ToString(); }
            catch (Exception e) { ; }
            StartXML(inFile);
            WriteBlock("string", lang, block , null);
            Console.WriteLine("[string lang={0} block:{1}]", lang, block);
            while ((txtLine = re.ReadLine()) != null)
            {
                int tIdx = txtLine.IndexOf('\t');
                if (tIdx > 0)
                {
                    string keyStr = txtLine.Substring(0, tIdx);
                    string valStr = txtLine.Substring(tIdx+1);
                    WriteText(keyStr, valStr);
                    Console.WriteLine("{0} \t{1}", keyStr, valStr);
                }
            }
            EndXML();
            re.Close();
        }
        static void ConvAdmFile(FileInfo inFile)
        {
            Console.WriteLine("ConvAdmFile: " + inFile.Name);
            Encoding fileEncoding = opt.GetEncoding(inFile.Directory.Name.ToLower());
            Console.WriteLine("Encoding: " + fileEncoding.EncodingName);
            StreamReader re = new StreamReader(inFile.FullName,fileEncoding);
            string txtLine = null;
            string lang = "1033";
            string block = Path.GetFileNameWithoutExtension(inFile.Name);
            //Console.WriteLine("Parent: " + inFile.Directory.Name);
            try { lang = CultureInfo.CreateSpecificCulture(inFile.Directory.Name).LCID.ToString(); }
            catch (Exception e) { ; }
            StartXML(inFile);
            WriteBlock("string", lang, block, null);
            Console.WriteLine("[string lang={0} block:{1}]", lang, block);
            bool bSectionFound = false;
            const string SECTNAME="[strings]";
            while ((txtLine = re.ReadLine()) != null )
            {
                if (!txtLine.StartsWith(";"))
                {
                    if (bSectionFound)
                    {
                        int tIdx = txtLine.IndexOf('=');
                        if (tIdx > 0)
                        {
                            string keyStr = txtLine.Substring(0, tIdx);
                            string valStr = txtLine.Substring(tIdx + 1);
                            WriteText(keyStr, valStr);
                            Console.WriteLine("{0} \t{1}", keyStr, valStr);
                        }
                    }
                    else
                    {
                        bSectionFound = SECTNAME.Equals(txtLine,StringComparison.CurrentCultureIgnoreCase);
                    }
                }
             
            }
            EndXML();
            re.Close();
        }
        static string GetLangId(string langCode)
        {
            int test;
            if (Int32.TryParse(langCode, out test)) return langCode;
            return CultureInfo.CreateSpecificCulture(langCode).LCID.ToString();
        }
        static bool IsValidCultureName(string langCode)
        {
            try
            {
                GetLangId(langCode);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        static void DumpFileResource(FileInfo inFile)
        {
            if (opt.IsExcluded(inFile.Name))
            {
                Console.WriteLine("ExludedFileMatch: " + inFile.FullName);
            }else{
                
                try
                {
                    
                    if (opt.IsDotNet || IsManaged(inFile))
                    //if (IsDotNetAssembly(inFile))
                    {
                        Console.WriteLine("*********************************************************");
                        Console.WriteLine("[.Net]: " + inFile.FullName);
                        Console.WriteLine("*********************************************************");
                        DumpNetFile(inFile);
                    }
                    else
                    {
                        if(inFile.Extension.Equals(".cxmi",StringComparison.CurrentCultureIgnoreCase)){
                            Console.WriteLine("*********************************************************");
                            Console.WriteLine("[CXMI]: " + inFile.FullName);
                            Console.WriteLine("*********************************************************");
                            ConvCmxi(inFile);
                        } 
                        else if(inFile.Extension.Equals(".idt",StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("*********************************************************");
                            Console.WriteLine("[IDT-]: " + inFile.FullName);
                            Console.WriteLine("*********************************************************");
                            ConvIdtFile(inFile);
                        }
                        else if (inFile.Extension.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                        {
                            ConvXMLFile(inFile);
                        }
                        else if (inFile.Extension.Equals(".adm", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("*********************************************************");
                            Console.WriteLine("[ADM-]: " + inFile.FullName);
                            Console.WriteLine("*********************************************************");
                            ConvAdmFile(inFile);
                        }

                        else{
                            Console.WriteLine("*********************************************************");
                            Console.WriteLine("[W32-]: " + inFile.FullName);
                            Console.WriteLine("*********************************************************");
                            DumpW32File(inFile);
                        }
                    }
                    Console.WriteLine("*********************************************************");
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine("Skipped: " + inFile.FullName + " - " + e.Message);
                }
                
                Console.WriteLine();
            }
        }

        static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        static void DumpDirResource(string dirPath, string[] extNames)
        {
            List<string> files = new List<string>();
            foreach (string x in extNames)
            {
                GetAllFiles(files, dirPath, x);
            }
            Console.WriteLine("Found " + files.Count + " files.");
            foreach (string p in files)
            {
                DumpFileResource(new FileInfo(p));
            }
            if (opt.IsMerge)
            {
                xml.WriteEndElement();
                xml.Close();
            }
        }

        static void GetAllFiles(List<string> result, string dirPath, string extFileName)
        {
            Stack<string> stack = new Stack<string>();
            stack.Push(dirPath);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                try
                {
                    result.AddRange(Directory.GetFiles(dir, "*." + extFileName));
                    foreach (string dn in Directory.GetDirectories(dir))
                    {
                        stack.Push(dn);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static bool IsFile(string path)
        {
            return File.Exists(path);
        }
        static void Main2(string[] args)
        {
            Regex r = new Regex(args[0]);
            if (r.IsMatch("Microsoft.Policy.Toolkit.dll")) Console.WriteLine("Matched");
        }

        static void Main(string[] args)
        {

            if (args.Length < 1)
            {
                PrintUsage();
            }
            else
            {

                opt = new Opt(args);
                if (opt.SubjectCount > 0)
                {
                    DateTime startTime = DateTime.Now;
                    outObj = opt.OutDir;
                    foreach (string p in opt.Subjects)
                    {

                        currPath = p;
                        if (IsFile(p)) //file
                        {
                            FileInfo inFile = new FileInfo(p);
                            outObj = opt.IsOutDirSet ? opt.OutDir : inFile.DirectoryName + "\\tokendump";
                            outObj = Directory.CreateDirectory(outObj).FullName;
                            DumpFileResource(inFile);
                        }
                        else //dir
                        {
                            DirectoryInfo inDir = new DirectoryInfo(p);
                            if (opt.IsOutDirSet)
                            {
                                outObj = opt.OutDir;
                            }
                            else
                            {
                                if (inDir.Parent == null)
                                    outObj = System.Environment.GetEnvironmentVariable("TEMP") + "\\tokendump";
                                else
                                    outObj = inDir.Parent.FullName + "\\tokendump";
                            }
                            currPath = inDir.FullName;
                            Console.WriteLine("Setting: currPath = " + currPath);
                            if (opt.SubjectCount > 1)
                            {
                                currPath = inDir.Parent.FullName;
                            }
                            //if (opt.IsMerge)
                            //{
                            //    FileInfo o = new FileInfo(outObj);
                            //    Directory.CreateDirectory(o.Directory.FullName);
                            //}

                            outObj = Directory.CreateDirectory(outObj).FullName;
                            DumpDirResource(p, opt.ExtNames);
                        }
                    }
                    Console.WriteLine();
                    TimeSpan execTime = DateTime.Now - startTime;
                    Console.WriteLine("ExecTime: " + execTime.TotalMilliseconds + " ms.");
                }
                else
                {
                    Console.WriteLine("No paths specified.");
                    PrintUsage();
                }
                //if (File.Exists(args[0]))
                //{
                //    FileInfo inFile = new FileInfo(args[0]);
                //    rootDir = inFile.DirectoryName;
                //    outObj = rootDir + "\\tokendump";
                //    if (args.Length == 2) outObj = args[1];
                //    outObj = Directory.CreateDirectory(outObj).FullName;
                //    DumpFileResource(inFile);
                //}
                //else if (Directory.Exists(args[0]))
                //{
                //    DirectoryInfo inDir = new DirectoryInfo(args[0]);
                //    rootDir = inDir.FullName;
                //    string[] xn = { "*" };
                //    if (args.Length >= 2)
                //    {
                //        if (args.Length == 3)
                //        {
                //            outObj = args[2];
                //        }
                //        xn = args[1].Split(';');
                //    }
                //    else
                //    {
                //        if (inDir.Parent == null)
                //            outObj = System.Environment.GetEnvironmentVariable("TEMP") + "\\tokendump";
                //        else
                //            outObj = inDir.Parent.FullName + "\\tokendump";
                //    }

                //    if (opt.IsMerge )
                //    {
                //        FileInfo o = new FileInfo(outObj);
                //        Directory.CreateDirectory(o.Directory.FullName);
                //    }
                //    else
                //    {
                //        outObj = Directory.CreateDirectory(outObj).FullName;
                //    }
                //    DumpDirResource(args[0], xn);
                //}
                //else
                //{
                //    Console.WriteLine(args[0] + " does not exist!");
                //}
                //Console.WriteLine("RootDir: " + rootDir);
                //Console.WriteLine("OutDir: " + outObj);
            }
        }
        static void PrintUsage()
        {
            Console.WriteLine(ERBLDNUM);
            Console.WriteLine("Options:");
            Console.WriteLine("\t/i path1[;path2;path3]    -dir or file paths. required]");
            Console.WriteLine("\t/f extName1[;eN2;eN3]     -extension filenames (dll;exe;vrs;cxmi;idt;adm;xml). optional");
            Console.WriteLine("\t/x fileName1[;fN2;fN3]    -excluded file names. optional");
            Console.WriteLine("\t/o outputDirPath          -output directory. optional");
            Console.WriteLine("\t/e lang1=enc1[;langN=encN]     -file encoding (e.g. ja=Shift-JIS;zh-CN=GB2312). optional");
            //Console.WriteLine("\t/m                        -create merge output. optional");
            //Console.WriteLine("\t/v                        -verbose output. optional");

            Console.WriteLine("Examples:");
            Console.WriteLine(" resdmp.exe /i \"%programfiles%\\citrix\\system32\"");
            Console.WriteLine("\t-dump resources of all files incl sub-dir files under ");
            Console.WriteLine("\t\"%programfiles%\\citrix\\system32\"");
            Console.WriteLine("");
            Console.WriteLine(" resdmp.exe /i \"%programfiles%\\citrix\\system32\" /f dll;exe;vrs /o c:\\temp /x mfcm80.dll;mfcm80u.dll;msvcm80.dll");
            Console.WriteLine(@" resdmp.exe /i D:\ADMFiles /f adm /o D:\ADMFiles\resdmp /e zh-CN=GB2312;es=ISO-8859-1;de=ISO-8859-1;en=ISO-8859-1;fr=ISO-8859-1;ja=Shift-JIS");
        }

        static void StartXML(FileInfo inFile)
        {
            string outFile = outObj;

            if (opt.IsMerge)
            {
                if (xml == null)
                {
                    xml = new XmlTextWriter(outFile, Encoding.UTF8);
                    xml.Formatting = Formatting.Indented;
                    xml.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                    xml.WriteStartDocument();
                    xml.WriteStartElement("resource");
                }
            }
            else
            {
                outFile = outObj + "\\" + inFile.Name + ".xml";
                if (inFile.DirectoryName.Length > currPath.Length)
                {
                    //outFile = outputDir + "\\" + inFile.DirectoryName.Substring(rootDir.Length + 1).Replace('\\', '_') + "_" + inFile.Name + ".xml";
                    string relDir = outObj + inFile.DirectoryName.Substring(currPath.Length);
                    //Console.WriteLine("DEBUG: outObj = " + outObj);
                    //Console.WriteLine("DEBUG: inFile.DirectoryName = " + inFile.DirectoryName);
                    //Console.WriteLine("DEBUG: currPath = " + currPath);
                    //Console.WriteLine("DEBUG: currPath.Length = " + currPath.Length);
                    //Console.WriteLine("DEBUG: relDir = " + relDir);
                    outFile = Directory.CreateDirectory(relDir).FullName + "\\" + inFile.Name + ".xml";
                }
                xml = new XmlTextWriter(outFile, Encoding.UTF8);
                xml.Formatting = Formatting.Indented;
                xml.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
            }
            xml.WriteStartElement("module");
            xml.WriteAttributeString("name", inFile.Name.Replace(".resources", ""));
            xml.WriteAttributeString("size", inFile.Length.ToString());
            xml.WriteAttributeString("modDate", inFile.LastWriteTimeUtc.ToString("MM/dd/yyyy HH:mm:ss"));
            xml.WriteAttributeString("dateLoc", "UTC");
            xml.WriteAttributeString("path", inFile.FullName);
        }

        static void WriteText(string key, string value)
        {
            xml.WriteStartElement("text");
            xml.WriteAttributeString("key", key);
            if (value == null) 
                xml.WriteString(value);
            else
                xml.WriteString(value.Trim('\0'));
            xml.WriteEndElement();
        }
        static void WriteBlock(string restype, string lang, string block, string size)
        {
            xml.WriteStartElement(restype);
            xml.WriteAttributeString("block", block);
            if (lang != null) xml.WriteAttributeString("lang", lang);
            if (size != null) xml.WriteAttributeString("size", size);
        }
        static void EndElement()
        {
            xml.WriteEndElement();
        }
        static void EndXML()
        {
            if (xml == null) return;
            xml.WriteEndElement(); //module
            if (!opt.IsMerge)
            {
                xml.Close();
                xml = null;
            }
        }
    }

    //------------------------------

    internal class Opt
    {
        private Dictionary<string, Regex> excFiles;
        private Dictionary<string, Encoding> langEncoding;
        private List<string> subjPaths;
        private string strOutDir = "tokendump";
        private List<string> extNames;
        private bool bVerbose = false;
        private bool bMerge = false;
        private bool bOutSet = false;
        private bool bDotNet = false;

        public Opt(string[] args)
        {
            excFiles = new Dictionary<string, Regex>();
            langEncoding = new Dictionary<string, Encoding>();
            subjPaths = new List<string>();
            extNames = new List<string>();

            string v = string.Empty;
            if (args[0].StartsWith("/"))
            {

                for (int i = 0; i < args.Length; i++)
                {
                    string s = args[i];
                    if (s.StartsWith("/"))
                    {
                        v = s.ToLower();
                        this.bMerge |= v.Equals("/m"); //merge
                        this.bVerbose |= v.Equals("/v"); //verbose
                        this.bDotNet |= v.Equals("/dn"); //dot net dumping
                    }
                    else
                    {
                        switch (v)
                        {
                            case "/i": //include
                                AddSubject(s.Split(';'));
                                break;
                            case "/f": //filters
                                AddExtNames(s.Split(';'));
                                break;
                            case "/x": //exclude
                                AddExcludedFiles(s.ToLower().Split(';'));
                                break;
                            case "/o": //outpur dir
                                this.strOutDir = s;
                                this.bOutSet = true;
                                break;
                            case "/e":
                                AddFileEncoding(s.Split(';'));
                                break;
                            case "/m": //recursive
                            case "/v": //verbose
                            case "/dn": //verbose
                                Console.WriteLine("{0} should not have a parameter.", v);
                                break;
                            default:
                                if (v.Equals(string.Empty)) Console.WriteLine("No option specified for this parameter.");
                                else Console.WriteLine("Unsupported option: " + v);
                                break;
                        }
                    }
                }
            }
            else
            {
                AddSubject(args[0].Split(';'));
                if (args.Length >= 3)
                {
                    AddExtNames(args[1].ToLower().Split(';'));
                    this.strOutDir = args[2];
                    this.bOutSet = true;
                    if (args.Length >= 4)
                        AddExcludedFiles(args[3].ToLower().Split(';'));
                    if (args.Length == 5)
                        AddFileEncoding(args[4].Split(';'));
                }
            }
        }
        private void AddExtNames(string[] extNames)
        {
            foreach (string s in extNames)
                if (s.Trim() != String.Empty) this.extNames.Add(s);
        }
        private void AddExcludedFiles(string[] fileNames)
        {
            foreach (string s in fileNames)
                if (s.Trim() != String.Empty) this.excFiles.Add(s,new Regex(s, RegexOptions.IgnoreCase));
        }
        private void AddSubject(string[] path)
        {
            foreach (string s in path)
                if(s.Trim() != String.Empty) this.subjPaths.Add(s);
        }
        private void AddFileEncoding(string[] path)
        {
            foreach (string s in path)
            {
                string[] v = s.Trim().Split('=');
                if (v.Length == 2)
                    this.langEncoding.Add(v[0].ToLower(), Encoding.GetEncoding(v[1]));
            }
        }

        public string OutDir
        {
            get { return strOutDir; }
        }
        public bool IsOutDirSet
        {
            get { return bOutSet; }
        }
        public bool IsVerbose
        {
            get { return bVerbose; }
        }
        public bool IsMerge
        {
            get { return bMerge; }
        }
        public string[] Subjects
        {
            get { return this.subjPaths.ToArray(); }
        }
        public int SubjectCount
        {
            get { return this.subjPaths.Count; }
        }
        public string[] ExtNames
        {
            get
            {
                if (ExtNamesCount == 0) return new string[] { "*" };
                else return this.extNames.ToArray();
            }
        }
        public int ExtNamesCount
        {
            get { return this.extNames.Count; }
        }
        public Encoding GetEncoding(string lang)
        {
            if (this.langEncoding.ContainsKey(lang.ToLower())) return this.langEncoding[lang];
            return Encoding.Default;
        }
        public bool IsExcluded(string fName)
        {
            //return this.excFiles.Contains(fName.ToLower());
            if (this.excFiles.ContainsKey(fName.ToLower()))
            {
                return true;
            }
            else
            {
                foreach (Regex r in this.excFiles.Values)
                    if (r.IsMatch(fName)) return true;
            }
            return false;
        }

        public bool IsDotNet { get { return this.bDotNet; } }
    }
}
