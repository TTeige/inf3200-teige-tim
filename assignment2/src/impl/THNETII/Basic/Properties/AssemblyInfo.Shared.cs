using System.Reflection;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
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
[assembly: AssemblyCompany("TH-NETII Software Solutions")]
[assembly: AssemblyCopyright("Written by Fredrik Høisæther Rasch, 2015")]
[assembly: AssemblyTrademark("")]
