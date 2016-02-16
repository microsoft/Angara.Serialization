namespace AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module internal Const =
    [<Literal>]
    let Version = "0.1.0" // Assembly semantic version

[<assembly: AssemblyTitle("Angara.Serialization.Json")>]
[<assembly: AssemblyDescription("Contains converters of InfoSets into and from JSON representation with type information preserved. " +
                                "Together with Angara.Serialization provides full framework for JSON serialization of arbitrary objects.")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("Microsoft Research")>]
[<assembly: AssemblyProduct("Angara")>]
[<assembly: AssemblyCopyright("Copyright (c) 2016 Microsoft Corporation")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[<assembly: Guid("583d3785-8507-46ec-9a7f-35c0ee8f1756")>]

[<assembly: AssemblyVersion(Const.Version + ".0")>]
[<assembly: AssemblyFileVersion(Const.Version + ".0")>]

// Does nothing but makes compiler happy
do
    ()