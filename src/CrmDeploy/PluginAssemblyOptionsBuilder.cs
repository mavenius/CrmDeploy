﻿using System;
using System.Activities;
using System.IO;
using System.Linq;
using CrmDeploy.Enums;
using Microsoft.Xrm.Sdk;

namespace CrmDeploy
{
    public class PluginAssemblyOptionsBuilder
    {

        protected PluginAssemblyRegistration PluginAssemblyRegistration { get; set; }

        public DeploymentBuilder RegistrationOptions { get; set; }

        public PluginAssemblyOptionsBuilder(DeploymentBuilder componentRegistrationnBuilder, PluginAssemblyRegistration pluginAssemblyRegistration)
        {
            PluginAssemblyRegistration = pluginAssemblyRegistration;
            RegistrationOptions = componentRegistrationnBuilder;
        }

        public PluginAssemblyOptionsBuilder RegisterInDatabase()
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.SourceType = new OptionSetValue()
                {
                    Value = (int)AssemblySourceType.Database
                };
            pl.Content = Convert.ToBase64String(File.ReadAllBytes(PluginAssemblyRegistration.Assembly.Location));

            return this;
        }

        public PluginAssemblyOptionsBuilder RegisterInDatabase(string assemblyFilePath)
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.SourceType = new OptionSetValue()
                {
                    Value = (int)AssemblySourceType.Database
                };
            pl.Content = Convert.ToBase64String(File.ReadAllBytes(assemblyFilePath));
            return this;
        }

        public PluginAssemblyOptionsBuilder RegisterOnServerFileSystem(string serverFilename)
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.SourceType = new OptionSetValue()
                {
                    Value = (int)AssemblySourceType.Disk
                };
            pl.Path = serverFilename;
            return this;
        }

        public PluginAssemblyOptionsBuilder RegisterOnServerGac()
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.SourceType = new OptionSetValue()
                {
                    Value = (int)AssemblySourceType.GAC
                };
            return this;
        }

        public PluginAssemblyOptionsBuilder HasVersion(string version)
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.Version = version;
            return this;
        }

        public PluginAssemblyOptionsBuilder HasPublicKeyToken(string publicKeyToken)
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.PublicKeyToken = publicKeyToken;
            return this;
        }

        public PluginAssemblyOptionsBuilder RunsInIsolationMode(IsolationMode isolationMode)
        {
            var pl = PluginAssemblyRegistration.PluginAssembly;
            pl.IsolationMode = new OptionSetValue()
                {
                    Value = (int)isolationMode
                };
            return this;
        }

        public PluginAssemblyOptionsBuilder RunsInSandboxMode()
        {
            RunsInIsolationMode(IsolationMode.Sandbox);
            return this;
        }

        public PluginAssemblyOptionsBuilder DiscoverPluginTypes()
        {
            var assy = PluginAssemblyRegistration.Assembly;
            var types = assy.GetTypes().Where(i => i.IsClass && typeof(IPlugin).IsAssignableFrom(i));

            foreach (var type in types)
            {
                var typeReg = new PluginTypeRegistration(PluginAssemblyRegistration, type);
                this.PluginAssemblyRegistration.PluginTypeRegistrations.Add(typeReg);
            }

            return this;

            //  return true;
        }

        public PluginAssemblyOptionsBuilder DiscoverPluginTypes(Action<PluginTypeOptionsBuilder, Type> configurePluginTypeCallback)
        {
            var assy = PluginAssemblyRegistration.Assembly;
            var types = assy.GetTypes().Where(i => i.IsClass && typeof(IPlugin).IsAssignableFrom(i));

            foreach (var type in types)
            {
                var typeReg = new PluginTypeRegistration(PluginAssemblyRegistration, type);
                this.PluginAssemblyRegistration.PluginTypeRegistrations.Add(typeReg);
                if (configurePluginTypeCallback != null)
                {
                    var builder = new PluginTypeOptionsBuilder(this, typeReg);
                    configurePluginTypeCallback(builder, typeReg.Type);
                }
            }

            return this;

            //  return true;
        }

        public PluginTypeOptionsBuilder HasPlugin<T>() where T : IPlugin
        {
            //var assy = PluginAssemblyRegistration.Assembly;
            //var types = assy.GetTypes().Where(i => i.IsClass && typeof(IPlugin).IsAssignableFrom(i));
            var typeReg = new PluginTypeRegistration(PluginAssemblyRegistration, typeof(T));
            PluginAssemblyRegistration.PluginTypeRegistrations.Add(typeReg);
            var builder = new PluginTypeOptionsBuilder(this, typeReg);
            return builder;
        }

        public PluginActivityOptionsBuilder HasActivity<T>(string name, string group) where T : CodeActivity
        {
            var typeReg = new PluginTypeRegistration(PluginAssemblyRegistration, typeof(T), name, group);
            PluginAssemblyRegistration.PluginTypeRegistrations.Add(typeReg);
            return new PluginActivityOptionsBuilder(this, typeReg);
        }
    }
}