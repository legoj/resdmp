**resdmp**,*also referred to as enumres*, dumps the string resources from a native PE module into an XML format,
also parses and converts text-based resource files like idt and adm files. 
Supported files types are dll, exe, vrs, cxmi, idt, adm and xml files.

### note:
if you are dumping modules located in a network share, there might be some cases
where the modules could not be loaded due to security settings of your PC where 
your are running the tool. I would recommend you to copy the binaries locally and dump them.

### [Usage]
    Options:
    /i path1[;path2;path3]    -dir or file paths. required
    /f extName1[;eN2;eN3]     -extension filenames (dll;exe;vrs;cxmi;idt;adm;xml). optional
    /x fileName1[;fN2;fN3]    -excluded file names. optional
    /o outputDirPath          -output directory. optional
    /e lang1=enc1[;langN=encN]     -file encoding (e.g. ja=Shift-JIS;zh-CN=GB2312). optional

### [Examples]
- dump resources of all files incl sub-dir files under "%programfiles%\citrix\system32" with default configs

      enumres.exe /i "%programfiles%\citrix\system32"
- dump files with extension names dll,exe,vrs under "%programfiles%\citrix\system32" and its sub-folders excluding mfc dlls to the output directory c:\temp
 
      enumres.exe /i "%programfiles%\citrix\system32" /f dll;exe;vrs /o c:\temp /x mfcm80.dll;mfcm80u.dll
- dump adm files under D:\ADMFiles to the output directory D:\ADMFiles\enumres using a specified encoding for each language resources
  
      enumres.exe /i D:\ADMFiles /f adm /o D:\ADMFiles\enumres /e zh-CN=GB2312;es=ISO-8859-1;de=ISO-8859-1;en=ISO-8859-1;fr=ISO-8859-1;ja=Shift-JIS


### change history:
* 07/29/10 - bug fix: use of IntPtr.Size changed to sizeof(Int32) for pointer adjustment.
* 08/04/10 - added Regex implementation in /x option.
* 08/26/10 - added .cxmi files processing. cxmi files will be converted to enumres xml format  
* 10/12/10 - added .idt files procession. .idt files will be converted to enumress xml format     
* 12/14/10 - bug fix: in cxmi conversion, the lang parameter should be in LCID.
* 12/20/10 - added .xml (License Server) <strings> format conversion to enumres xml format
* 01/06/11 - added .adm file support; added /e for file encoding
* 08/31/11 - added trailing nul remover in text .Trim('\0')
* 11/11/11 - bug fix. value reference for the fix introduced in 08/31 could sometimes be null. added null handler.
* 11/16/11 - added autorun xml file <Autorun> format conversion to enumres xml format
* 01/22/13 - switched back to use LoadAssembly in managed module identification. CLR4 had a different behavior on GetAssemblyName API.
