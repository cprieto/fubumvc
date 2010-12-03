﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using FubuCore;
using FubuCore.Util;

namespace FubuMVC.Core.Packaging
{
    public class PackagingDiagnostics : IPackagingDiagnostics
    {
        private readonly Cache<object, PackageRegistryLog> _logs = new Cache<object, PackageRegistryLog>(o => new PackageRegistryLog(){
            Description = o.ToString()
        });
    
        public void LogObject(object target, string provenance)
        {
            _logs[target].Provenance = provenance;
        }

        public PackageRegistryLog LogFor(object target)
        {
            return _logs[target];
        }

        public void LogExecution(object target, Action continuation)
        {
            var log = _logs[target];
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                continuation();
            }
            catch (Exception e)
            {
                log.MarkFailure(e);
            }
            finally
            {
                stopwatch.Stop();
                log.TimeInMilliseconds = stopwatch.ElapsedMilliseconds;
            }
            
        }

        public void LogPackage(IPackageInfo package, IPackageLoader loader)
        {
            LogObject(package, "Loaded by " + loader);
            LogFor(loader).AddChild(package);
        }

        public void LogBootstrapperRun(IBootstrapper bootstrapper, IEnumerable<IPackageActivator> activators)
        {
            var provenance = "Loaded by Bootstrapper:  " + bootstrapper;
            var bootstrapperLog = LogFor(bootstrapper);
            
            activators.Each(a =>
            {
                LogObject(a, provenance);
                bootstrapperLog.AddChild(a);
            });
        }

        // TODO:  Try to find the assembly file version here. 
        public void LogAssembly(IPackageInfo package, Assembly assembly, string provenance)
        {
            LogObject(assembly, provenance);
            var packageLog = LogFor(package);
            packageLog.Trace("Loaded assembly " + assembly.GetName().FullName);
            packageLog.AddChild(assembly);
        }

        // just in log to package
        public void LogDuplicateAssembly(IPackageInfo package, string assemblyName)
        {
            LogFor(package).Trace("Assembly '{0}' was ignored because it is already loaded".ToFormat(assemblyName));
        }

        public void LogAssemblyFailure(IPackageInfo package, string fileName, Exception exception)
        {
            var log = LogFor(package);
            log.MarkFailure(exception);
            log.Trace("Failed to load assembly at '{0}'".ToFormat(fileName));
        }

    }
}