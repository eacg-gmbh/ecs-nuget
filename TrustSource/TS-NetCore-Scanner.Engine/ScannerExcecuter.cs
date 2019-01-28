﻿using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TS_NetCore_Scanner.Engine
{
    internal class ScannerExcecuter
    {
        internal static Target ProcessDependencies(string solutionName, PackageSpec project)
        {
            Target projectTarget = new Target();
            projectTarget.project = solutionName;
            projectTarget.moduleId = $"netcore:{project.Name}";
            projectTarget.module = project.Name;
            projectTarget.release = project.Version.ToFullString();

            projectTarget.dependencies = new List<Dependency>();

            // Generate lock file
            var lockFileService = new LockFileService();
            var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);

            foreach (var targetFramework in project.TargetFrameworks)
            {
                var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
                if (lockFileTargetFramework != null)
                {
                    foreach (var dependency in targetFramework.Dependencies)
                    {
                        var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);
                        ReportDependency(projectTarget.dependencies, projectLibrary, lockFileTargetFramework, dependency.AutoReferenced);
                    }
                }
            }

            return projectTarget;
        }

        private static void ReportDependency(List<Dependency> dependencies, LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, bool AutoReferenced)
        {
            Dependency targetDependency = new Dependency();
            dependencies.Add(targetDependency);

            targetDependency.name = projectLibrary.Name;
            targetDependency.key = $"netcore:{projectLibrary.Name}";
            targetDependency.versions.Add(projectLibrary.Version.ToNormalizedString());

            //targetDependency.checksum = "";
            //targetDependency.homepageUrl = "";
            //targetDependency.repoUrl = "";
            //targetDependency.description = projectLibrary.Version.ToFullString();
            //targetDependency.licences.Add(new licence() { name = projectLibrary.Version.ToFullString(), url = "" });

            if (!AutoReferenced)
                foreach (var childDependency in projectLibrary.Dependencies)
                {
                    var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);
                    bool SystemReferenced = MetaPackagesSkipper.MetaPackages.Any(x => x == childLibrary.Name);
                    ReportDependency(targetDependency.dependencies, childLibrary, lockFileTargetFramework, SystemReferenced);
                }
        }
    }
}