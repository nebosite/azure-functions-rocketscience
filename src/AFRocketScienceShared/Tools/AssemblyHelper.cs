﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.Azure.Functions.AFRocketScience
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Tools for dealing with assemblies  
    /// </summary>
    //--------------------------------------------------------------------------------
    public static class AssemblyHelper
    {
        //--------------------------------------------------------------------------------
        /// <summary>
        /// Redirection hack because Azure functions don't support it.
        /// How to use:  
        ///     If you get an error that a certain version of a dll can't be found:
        ///         1) deploy that particular dll in any project subfolder by adding the 
        ///            dll to your project as "content" that is "copied if newer"
        ///         2) In your azure function static constructor, Call 
        ///             AssemblyHelper.IncludeSupplementalDllsWhenBinding()
        ///         
        /// This will hook the binding calls and look for a matching dll anywhere 
        /// in the $HOME folder tree.  
        /// </summary>
        //--------------------------------------------------------------------------------
        public static void IncludeSupplementalDllsWhenBinding()
        {
            var searching = false;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // This prevents a stack overflow
                if(searching) return null;
                var requestedAssembly = new AssemblyName(args.Name);
                searching = true;

                Assembly foundAssembly = null;
                try
                {
                    foundAssembly = Assembly.Load(requestedAssembly);
                }
                catch(Exception e)
                {
                    Debug.WriteLine($"Could not load assembly: {args.Name} because {e.Message}");
                }

                searching  = false;

                if(foundAssembly == null)
                {
                    var home = Environment.GetEnvironmentVariable("HOME") ?? ".";
                    
                    var possibleFiles = Directory.GetFiles(home, requestedAssembly.Name + ".dll", SearchOption.AllDirectories);
                    Debug.WriteLine("Requested version: " + requestedAssembly.Version);
                    foreach (var file in possibleFiles)
                    {
                        var possibleAssembly = AssemblyName.GetAssemblyName(file);
                        Debug.WriteLine("Found version: " + possibleAssembly.Version + " at " + file);

                        if (possibleAssembly.Version == requestedAssembly.Version)
                        {
                            foundAssembly = Assembly.Load(possibleAssembly);
                            break;
                        }
                    }
                }

                return foundAssembly;
            };
        }
    }
}
