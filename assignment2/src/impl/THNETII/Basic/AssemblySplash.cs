using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace THNETII
{
    /// <summary>
    /// A static class that provides functionality for writing assembly runtime information to a specified writer.
    /// </summary>
    public static class AssemblySplash
    {
        /// <summary>
        /// Gets the most common attributes of the assembly of the executing process and writes these attributes 
        /// together with current runtime information into the console.
        /// </summary>
        public static void WriteAssemblySplash()
        {
            WriteAssemblySplash(new AssemblyInfo(), System.Console.Out);
        }

        /// <summary>
        /// Gets the most common attributes of the assembly the specified type is located in and writes these attributes
        /// together with current runtime information into the console.
        /// </summary>
        /// <param name="t">A type that resides in the target assembly</param>
        public static void WriteAssemblySplash(Type t)
        {
            WriteAssemblySplash(t, writer: System.Console.Out);
        }

        /// <summary>
        /// Gets the most common attributes of the specified assembly and writes these attributes 
        /// together with current runtime information into the console.
        /// </summary>
        /// <param name="prog_assem">The target assembly.</param>
        public static void WriteAssemblySplash(Assembly prog_assem)
        {
            WriteAssemblySplash(prog_assem, writer: System.Console.Out);
        }
		
		public static void WriteAssemblySplash(AssemblyInfo assem_info)
		{
			WriteAssemblySplash(assem_info, writer: System.Console.Out);
		}

        /// <summary>
        /// Gets the most common attributes of the assembly the specified type is located in and writes these attributes
        /// together with current runtime information into the specified <see cref="TextWriter"/> instance.
        /// </summary>
        /// <param name="t">A type that resides in the target assembly</param>
        /// <param name="writer">A <see cref="TextWriter"/> instance to which the Assembly Splash text should be written.</param>
        public static void WriteAssemblySplash(Type t, TextWriter writer)
        {
            WriteAssemblySplash(new AssemblyInfo(t), writer);
        }

        /// <summary>
        /// Gets the most common attributes of the assembly the currently executed code resides in
        /// together with current runtime information into the specified <see cref="TextWriter"/> instance.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> instance to which the Assembly Splash text should be written.</param>
        public static void WriteAssemblySplash(TextWriter writer)
        {
            WriteAssemblySplash(new AssemblyInfo(), writer);
        }

        /// <summary>
        /// Gets the most common attributes of the specified assembly and writes these attributes 
        /// together with current runtime information into the specified <see cref="TextWriter"/> instance.
        /// </summary>
        /// <param name="prog_assem">The target assembly.</param>
        /// <param name="writer">A <see cref="TextWriter"/> instance to which the Assembly Splash text should be written.</param>
        public static void WriteAssemblySplash(Assembly prog_assem, TextWriter writer)
        {
            WriteAssemblySplash(new AssemblyInfo(prog_assem), writer);
        }

        public static void WriteAssemblySplash(AssemblyInfo assem_info, TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("{0} Version {1}", assem_info.AssemblyName.Name, assem_info.AssemblyName.Version);
            writer.WriteLine(assem_info.ProductString);
            writer.WriteLine(assem_info.ConfigurationString);
            writer.WriteLine(assem_info.CopyrightString);
            writer.WriteLine(assem_info.TrademarkString);
            writer.WriteLine(assem_info.CompanyString);

            writer.WriteLine();
            writer.WriteLine("User: {0}, Domain: {1}", Environment.UserName, Environment.UserDomainName);
            writer.WriteLine("Executing on: {0}, PID: {1} ({2}){3}", Environment.MachineName, Process.GetCurrentProcess().Id,
                Process.GetCurrentProcess().ProcessName, Environment.Is64BitProcess ? ", 64-bit" : "");
            writer.WriteLine("OS: {0}{1}", Environment.OSVersion, Environment.Is64BitOperatingSystem ? " (64-bit)" : "");
            writer.WriteLine("Common Language Runtime Version {0}", Environment.Version);
			writer.Flush();
        }

        /// <summary>
        /// Gets the most common attributes of the assembly the specified type is located in and logs these attributes
        /// together with current runtime information as Debug Trace-Messages.
        /// </summary>
        /// <param name="t">A type that resides in the target assembly</param>
        /// <remarks>Calls to this method are ignored if the DEBUG preprocessing identifier is not defined.
        /// <para>This method relies on calls to <see cref="Debug.WriteLine(string, object[])"/> to log Debug Trace-Messages.</para>
        /// </remarks>
        [Conditional("DEBUG")]
        public static void WriteDebugAssemblySplash(Type t)
        {
            WriteDebugAssemblySplash(new AssemblyInfo(t));
        }

        /// <summary>
        /// Gets the most common attributes of the specified assembly and logs these attributes
        /// together with current runtime information as Debug Trace-Messages.
        /// </summary>
        /// <param name="prog_assem">A type that resides in the target assembly</param>
        /// <remarks>Calls to this method are ignored if the DEBUG preprocessing identifier is not defined.
        /// <para>This method relies on calls to <see cref="Debug.WriteLine(string)"/> and <see cref="Debug.WriteLine(string, object[])"/> to log Debug Trace-Messages.</para>
        /// </remarks>
        [Conditional("DEBUG")]
        public static void WriteDebugAssemblySplash(Assembly prog_assem)
        {
            WriteDebugAssemblySplash(new AssemblyInfo(prog_assem));
        }
        
        [Conditional("DEBUG")]
        public static void WriteDebugAssemblySplash(AssemblyInfo assem_info)
        {
            Debug.WriteLine(string.Empty);
            Debug.WriteLine("{0} Version {1}", assem_info.AssemblyName.Name, assem_info.AssemblyName.Version);
            Debug.WriteLine(assem_info.ProductString);
            Debug.WriteLine(assem_info.ConfigurationString);
            Debug.WriteLine(assem_info.CopyrightString);
            Debug.WriteLine(assem_info.TrademarkString);
            Debug.WriteLine(assem_info.CompanyString);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("User: {0}, Domain: {1}", Environment.UserName, Environment.UserDomainName);
            Debug.WriteLine("Executing on: {0}, PID: {1} ({2}){3}", Environment.MachineName, Process.GetCurrentProcess().Id,
                Process.GetCurrentProcess().ProcessName, Environment.Is64BitProcess ? ", 64-bit" : "");
            Debug.WriteLine("OS: {0}{1}", Environment.OSVersion, Environment.Is64BitOperatingSystem ? " (64-bit)" : "");
            Debug.WriteLine("Common Language Runtime Version {0}", Environment.Version);
            Debug.Flush();
        }

        /// <summary>
        /// Gets the most common attributes of the assembly the specified type is located in and logs these attributes
        /// together with current runtime information as Information Trace-Messages.
        /// </summary>
        /// <param name="t">A type that resides in the target assembly</param>
        /// <remarks>Calls to this method are ignored if the TRACE preprocessing identifier is not defined.
        /// <para>This method relies on calls to <see cref="Trace.TraceInformation(string)"/> and <see cref="Trace.TraceInformation(string, object[])"/> to log Information Trace-Messages.</para>
        /// </remarks>
        [Conditional("TRACE")]
        public static void TraceInformationAssemblySplash(Type t)
        {
            TraceInformationAssemblySplash(new AssemblyInfo(t));
        }

        /// <summary>
        /// Gets the most common attributes of the specified assembly and logs these attributes
        /// together with current runtime information as Information Trace-Messages.
        /// </summary>
        /// <param name="prog_assem">A type that resides in the target assembly</param>
        /// <remarks>Calls to this method are ignored if the TRACE preprocessing identifier is not defined.
        /// <para>This method relies on calls to <see cref="Trace.TraceInformation(string)"/> and <see cref="Trace.TraceInformation(string)"/> to log Information Trace-Messages.</para>
        /// </remarks>
        [Conditional("TRACE")]
        public static void TraceInformationAssemblySplash(Assembly prog_assem)
        {
            TraceInformationAssemblySplash(new AssemblyInfo(prog_assem));
        }

        [Conditional("TRACE")]
        public static void TraceInformationAssemblySplash(AssemblyInfo assem_info)
        {
            Trace.TraceInformation(string.Empty);
            Trace.TraceInformation("{0} Version {1}", assem_info.AssemblyName.Name, assem_info.AssemblyName.Version);
            Trace.TraceInformation(assem_info.ProductString);
            Trace.TraceInformation(assem_info.ConfigurationString);
            Trace.TraceInformation(assem_info.CopyrightString);
            Trace.TraceInformation(assem_info.TrademarkString);
            Trace.TraceInformation(assem_info.CompanyString);

            Trace.TraceInformation("User: {0}, Domain: {1}", Environment.UserName, Environment.UserDomainName);
            Trace.TraceInformation("Executing on: {0}, PID: {1} ({2}){3}", Environment.MachineName, Process.GetCurrentProcess().Id,
                Process.GetCurrentProcess().ProcessName, Environment.Is64BitProcess ? ", 64-bit" : "");
            Trace.TraceInformation("OS: {0}{1}", Environment.OSVersion, Environment.Is64BitOperatingSystem ? " (64-bit)" : "");
            Trace.TraceInformation("Common Language Runtime Version {0}", Environment.Version);
            Trace.Flush();
        }

#if ASSEMBLYSPLASH_EXE
		internal static class Program
		{
			internal static void Main(string[] args)
			{
				WriteAssemblySplash();
			}
		}
#endif
	}
}
