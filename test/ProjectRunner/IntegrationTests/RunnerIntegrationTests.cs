﻿using JeremyTCD.DotNetCore.Utils;
using Moq;
using StructureMap;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace JeremyTCD.ProjectRunner.Tests.IntegrationTests
{
    public class RunnerIntegrationTests
    {
        private MockRepository _mockRepository { get; }
        private string _tempDir { get; } = Path.Combine(Path.GetTempPath(), $"{nameof(Runner)}Temp");
        private DirectoryService _directoryService { get; }

        public RunnerIntegrationTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };

            Mock<ILoggingService<DirectoryService>> mockDSLS = _mockRepository.Create<ILoggingService<DirectoryService>>();
            _directoryService = new DirectoryService(mockDSLS.Object);
            _directoryService.DeleteIfExists(_tempDir, true);
            _directoryService.Create(_tempDir);
            _directoryService.SetCurrentDirectory(_tempDir);
        }

        [Fact]
        public void Run_RunsEntryMethod()
        {
            // Arrange
            string solutionDir = Path.GetFullPath(typeof(RunnerIntegrationTests).GetTypeInfo().Assembly.Location + "../../../../../../../");
            string projectDir = "StubProject.EntryPoint";
            string projectName = projectDir;
            string projectAbsSrcDir = $"{solutionDir}test/{projectDir}";
            string projectAbsDestDir = $"{_tempDir}/{projectDir}";
            string projectAbsFilePath = $"{_tempDir}/{projectDir}/{projectName}.csproj";
            string entryAssemblyName = projectName;
            string entryClassName = $"{projectName}.EntryPointStubClass";
            int testExitCode = 10; // Arbitrary 
            string[] stubArgs = new string[] { testExitCode.ToString() };

            _directoryService.Copy(projectAbsSrcDir, projectAbsDestDir, excludePatterns: new string[] { "^bin$", "^obj$" });

            IContainer container = new Container(new ProjectRunnerRegistry());
            Runner runner = container.GetInstance<Runner>();

            // Act
            int result = runner.Run(projectAbsFilePath, entryAssemblyName, entryClassName, publishConfiguration: "Debug", args: stubArgs);

            // Assert
            Assert.Equal(testExitCode, result);
        }
    }
}
