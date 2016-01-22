namespace AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module internal Const =
    [<Literal>]
    let Version = "0.1.0" // Assembly semantic version

[<assembly: AssemblyTitle("Angara.Serialization")>]
[<assembly: AssemblyDescription("A framework for conversion of CLR objects to and from special data type called InfoSet suitable for persistence and transport.")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("Microsoft Research")>]
[<assembly: AssemblyProduct("Angara")>]
[<assembly: AssemblyCopyright("Copyright © Microsoft Research 2016")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[<assembly: Guid("f4fa4cad-18c9-46c3-aea9-6614d0898bb5")>]

[<assembly: AssemblyVersion(Const.Version + ".0")>]
[<assembly: AssemblyFileVersion(Const.Version + ".0")>]

// Does nothing but makes compiler happy
do
    ()