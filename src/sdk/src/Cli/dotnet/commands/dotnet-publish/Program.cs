﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using Microsoft.VisualBasic.CompilerServices;
using Parser = Microsoft.DotNet.Cli.Parser;


namespace Microsoft.DotNet.Tools.Publish
{
    public class PublishCommand : RestoringCommand
    {
        private PublishCommand(
            IEnumerable<string> msbuildArgs,
            bool noRestore,
            string msbuildPath = null)
            : base(msbuildArgs, noRestore, msbuildPath)
        {
        }

        public static PublishCommand FromArgs(string[] args, string msbuildPath = null)
        {
            var parser = Parser.Instance;
            var parseResult = parser.ParseFrom("dotnet publish", args);
            return FromParseResult(parseResult);
        }

        public static PublishCommand FromParseResult(ParseResult parseResult, string msbuildPath = null)
        {
            parseResult.HandleDebugSwitch();
            parseResult.ShowHelpOrErrorIfAppropriate();

            var msbuildArgs = new List<string>()
            {
                "-target:Publish",
                "--property:_IsPublishing=true" // This property will not hold true for MSBuild /t:Publish. VS should also inject this property when publishing in the future.
            };

            IEnumerable<string> slnOrProjectArgs = parseResult.GetValueForArgument(PublishCommandParser.SlnOrProjectArgument);

            CommonOptions.ValidateSelfContainedOptions(parseResult.HasOption(PublishCommandParser.SelfContainedOption),
                parseResult.HasOption(PublishCommandParser.NoSelfContainedOption));

            msbuildArgs.AddRange(parseResult.OptionValuesToBeForwarded(PublishCommandParser.GetCommand()));
            ReleasePropertyProjectLocator projectLocator = new ReleasePropertyProjectLocator(Environment.GetEnvironmentVariable(EnvironmentVariableNames.ENABLE_PUBLISH_RELEASE_FOR_SOLUTIONS) != null);
            msbuildArgs.AddRange(projectLocator.GetCustomDefaultConfigurationValueIfSpecified(parseResult, MSBuildPropertyNames.PUBLISH_RELEASE,
                slnOrProjectArgs, PublishCommandParser.ConfigurationOption) ?? Array.Empty<string>());
            msbuildArgs.AddRange(slnOrProjectArgs ?? Array.Empty<string>());

            bool noRestore = parseResult.HasOption(PublishCommandParser.NoRestoreOption)
                          || parseResult.HasOption(PublishCommandParser.NoBuildOption);

            return new PublishCommand(
                msbuildArgs,
                noRestore,
                msbuildPath);
        }

        public static int Run(ParseResult parseResult)
        {
            parseResult.HandleDebugSwitch();

            return FromParseResult(parseResult).Execute();
        }
    }
}
