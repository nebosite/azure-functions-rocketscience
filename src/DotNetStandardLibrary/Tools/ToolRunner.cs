using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace icrosoft.Azure.Functions.AFRocketScience.Tools
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// A convenient way to access a tool from an azure function
    /// </summary>
    //---------------------------------------------------------------------------------
    public class ToolRunner
    {
        public string Path { get; private set; }

        public class ToolOutput
        {
            List<string> _stdOut = new List<string>();
            public string[] StdOut => _stdOut.ToArray();

            List<string> _stdError = new List<string>();
            public string[] StdError => _stdError.ToArray();

            public void AddStdOut(string line)
            {
                if (line != null) _stdOut.Add(line);
            }
            public void AddStdError(string line)
            {
                if (line != null) _stdError.Add(line);
            }
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// Note: This could be expensive because it searches for the exucutable in the HOME
        /// directory.  If you want to limit the search, the directoryHint will be
        /// appended to the home directory.
        /// </summary>
        //---------------------------------------------------------------------------------
        public ToolRunner(string executableName, string directoryHint = null)
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if(home == null)
            {
                home = ".";
            }

            if (directoryHint != null) home = System.IO.Path.Combine(home, directoryHint);
            var possibleFiles = Directory.GetFiles(home, executableName, SearchOption.AllDirectories);

            if(possibleFiles.Length > 1)
            {
                throw new ApplicationException($"More than one location found for {executableName}:\r\n"
                    + string.Join("\r\n", possibleFiles));
            }
            if(possibleFiles.Length == 0)
            {
                throw new ApplicationException($"Could not fine {executableName} anywhere under {home}");
            }

            Path = possibleFiles[0];
        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// Run the command asynchrounously and return the console output
        /// </summary>
        //---------------------------------------------------------------------------------
        public Task<ToolOutput> RunAsync(params string[] args)
        {
            return Task.Run(() =>
            {
                var output = new ToolOutput();
                var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (sender, eventsArgs) => output.AddStdOut(eventsArgs.Data);
                process.ErrorDataReceived += (sender, eventsArgs) => output.AddStdError(eventsArgs.Data);
                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.FileName = Path;

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.CancelOutputRead();
                return output;
            });
        }
    }
}
