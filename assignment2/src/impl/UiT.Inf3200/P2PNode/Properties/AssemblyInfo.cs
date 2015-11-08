using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("P2PNode")]
[assembly: AssemblyDescription("")]
#region [assembly: AssemblyConfiguration("")]
#if DEBUG
#if ANYCPU
[assembly: AssemblyConfiguration("Debug configuration for any CPU")]
#elif X86 // !ANYCPU && X86
[assembly: AssemblyConfiguration("Debug configuration for x86")]
#elif X64 // !ANYCPU && !X86 && X64
[assembly: AssemblyConfiguration("Debug configuration for x64")]
#else // !ANYCPU && !X86 && !X64
[assembly: AssemblyConfiguration("Debug configuration")]
#endif
#else // !DEBUG
#if ANYCPU
[assembly: AssemblyConfiguration("Release for any CPU")]
#elif X86 // !ANYCPU && X86
[assembly: AssemblyConfiguration("Release for x86")]
#elif X64 // !ANYCPU && !X86 && X64
[assembly: AssemblyConfiguration("Release for x64")]
#else // !ANYCPU && !X86 && !X64
[assembly: AssemblyConfiguration("Release configuration")]
#endif
#endif // !DEBUG
#endregion
[assembly: AssemblyCompany("UiT The arctic university of Norway, Department of Computer Science")]
[assembly: AssemblyCopyright("Written by Tim Alexander Teige, Fredrik Høisæther Rasch, 2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyProduct("Inf-3200 - Assignment 2")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("640d747c-b71c-4a36-8f44-5d468badf8ca")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.120.*")]
[assembly: AssemblyFileVersion("2.0.120")]
