﻿using JeremyTCD.DotNetCore.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JeremyTCD.ProjectRunner
{
    public class ProjectRunner
    {
        private ILoggingService<ProjectRunner> _loggingService { get; }
        private IPathService _pathService { get; }
        private IMSBuildService _msBuildService { get; }
        private IDirectoryService _directoryService { get; }

        public ProjectRunner(ILoggingService<ProjectRunner> loggingService, IPathService pathService, IMSBuildService msbuildService,
            IDirectoryService directoryService)
        {
            _loggingService = loggingService;
            _pathService = pathService;
            _msBuildService = msbuildService;
            _directoryService = directoryService;
        }

        // TODO split up into smaller methods and add exceptions for irrecoverable situations like if no Main method exists
        /// <summary>
        /// Restores, builds and publishes project specified by <paramref name="projFile"/>. Loads entry assembly specified by <paramref name="entryAssemblyFile"/> in an <see cref="AssemblyLoadContext"/>.
        /// Calls main method with args <paramref name="args"/>.
        /// </summary>
        /// <param name="projFile"></param>
        /// <param name="args"></param> 
        /// <returns>
        /// Integer return value of entry method or null if entry method returns void
        /// </returns>
        public virtual int? Run(string projFile, string entryAssemblyFile, string entryClassName, string[] args)
        {
            if (_loggingService.IsEnabled(LogLevel.Information))
            {
                _loggingService.LogInformation(Strings.Log_RunningProject, projFile, String.Concat(args, ','));
            }

            string absProjFile = _pathService.GetAbsolutePath(projFile);
            string directory = _directoryService.GetParent(absProjFile).FullName;

            // Build project
            IEnumerable<string> targetFrameworks = _msBuildService.GetTargetFrameworks(projFile);
            _msBuildService.Build(absProjFile, $"/t:restore,publish /p:configuration=release,targetframework={targetFrameworks.First()}");

            // Load entry assembly
            DirectoryAssemblyLoadContext dalc = new DirectoryAssemblyLoadContext(directory);
            Assembly entryAssembly = dalc.LoadFromAssemblyPath(entryAssemblyFile);

            // Run entry method
            Type entryType = entryAssembly.GetType(entryClassName);
            MethodInfo method = entryType.GetMethod("Main", 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Object entryObject = Activator.CreateInstance(entryType);
            object result = method.Invoke(entryObject, args);

            return result as int?;
        }
    }
}
