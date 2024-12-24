// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines all of the common stuff that is shared between the Vbc and Csc tasks.
    /// This class is not instantiatable as a Task just by itself.
    /// </summary>
    public abstract class InteractiveCompiler : ManagedToolTask
    {
        internal readonly PropertyDictionary Store = [];

        public InteractiveCompiler()
            : base(ErrorString.ResourceManager)
        {
        }

        #region Properties - Please keep these alphabetized.
        public string[]? AdditionalLibPaths
        {
            set
            {
                Store[nameof(AdditionalLibPaths)] = value;
            }

            get
            {
                return (string[]?)Store[nameof(AdditionalLibPaths)];
            }
        }

        public string[]? AdditionalLoadPaths
        {
            set
            {
                Store[nameof(AdditionalLoadPaths)] = value;
            }

            get
            {
                return (string[]?)Store[nameof(AdditionalLoadPaths)];
            }
        }

        [Output]
        public ITaskItem[]? CommandLineArgs
        {
            set
            {
                Store[nameof(CommandLineArgs)] = value;
            }

            get
            {
                return (ITaskItem[]?)Store[nameof(CommandLineArgs)];
            }
        }

        public string? Features
        {
            set
            {
                Store[nameof(Features)] = value;
            }

            get
            {
                return (string?)Store[nameof(Features)];
            }
        }

        public ITaskItem[]? Imports
        {
            set
            {
                Store[nameof(Imports)] = value;
            }

            get
            {
                return (ITaskItem[]?)Store[nameof(Imports)];
            }
        }

        public bool ProvideCommandLineArgs
        {
            set
            {
                Store[nameof(ProvideCommandLineArgs)] = value;
            }

            get
            {
                return Store.GetOrDefault(nameof(ProvideCommandLineArgs), false);
            }
        }

        public ITaskItem[]? References
        {
            set
            {
                Store[nameof(References)] = value;
            }

            get
            {
                return (ITaskItem[]?)Store[nameof(References)];
            }
        }

        public ITaskItem[]? ResponseFiles
        {
            set
            {
                Store[nameof(ResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[]?)Store[nameof(ResponseFiles)];
            }
        }

        public string[]? ScriptArguments
        {
            set
            {
                Store[nameof(ScriptArguments)] = value;
            }

            get
            {
                return (string[]?)Store[nameof(ScriptArguments)];
            }
        }

        public ITaskItem[]? ScriptResponseFiles
        {
            set
            {
                Store[nameof(ScriptResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[]?)Store[nameof(ScriptResponseFiles)];
            }
        }

        public bool SkipInteractiveExecution
        {
            set
            {
                Store[nameof(SkipInteractiveExecution)] = value;
            }

            get
            {
                return Store.GetOrDefault(nameof(SkipInteractiveExecution), false);
            }
        }

        public ITaskItem? Source
        {
            set
            {
                Store[nameof(Source)] = value;
            }

            get
            {
                return (ITaskItem?)Store[nameof(Source)];
            }
        }
        #endregion

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can go into a response file.
        /// </summary>
        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendSwitch("/i-");

            ManagedCompiler.AddFeatures(commandLine, Features);

            if (ResponseFiles != null)
            {
                foreach (var response in ResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", response.ItemSpec);
                }
            }

            commandLine.AppendFileNameIfNotNull(Source);

            if (ScriptArguments != null)
            {
                foreach (var scriptArgument in ScriptArguments)
                {
                    commandLine.AppendArgumentIfNotNull(scriptArgument);
                }
            }

            if (ScriptResponseFiles != null)
            {
                foreach (var scriptResponse in ScriptResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", scriptResponse.ItemSpec);
                }
            }
        }
    }
}
