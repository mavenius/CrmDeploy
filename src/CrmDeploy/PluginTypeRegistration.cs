﻿using System;
using System.Collections.Generic;
using CrmDeploy.Entities;
using Microsoft.Xrm.Sdk;

namespace CrmDeploy
{
    public class PluginTypeRegistration
    {

        public PluginAssemblyRegistration PluginAssemblyRegistration { get; set; }

        public PluginTypeRegistration(PluginAssemblyRegistration pluginAssemblyRegistration, Type type)
            : this(pluginAssemblyRegistration, type, type.Name, null)
        {   
        }

        public PluginTypeRegistration(PluginAssemblyRegistration pluginAssemblyRegistration, Type type, string name, string workflowActivityGroupName)
        {
            PluginAssemblyRegistration = pluginAssemblyRegistration;
            PluginType = new PluginType { TypeName = type.FullName, FriendlyName = type.FullName, Name = name, WorkflowActivityGroupName = workflowActivityGroupName };
            Type = type;
            PluginStepRegistrations = new List<PluginStepRegistration>();
            pluginAssemblyRegistration.PluginAssembly.PropertyChanged += PluginAssembly_PropertyChanged;
        }

        void PluginAssembly_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var pluginAssembly = PluginAssemblyRegistration.PluginAssembly;

            if (e.PropertyName == "PluginAssemblyId")
            {
                if (pluginAssembly.PluginAssemblyId == null)
                {
                    PluginType.PluginAssemblyId = null;
                }
                else
                {
                    PluginType.PluginAssemblyId = new EntityReference(pluginAssembly.LogicalName, pluginAssembly.PluginAssemblyId.Value);
                }
            }
        }

        public PluginType PluginType { get; set; }

        public Type Type { get; set; }

        public List<PluginStepRegistration> PluginStepRegistrations { get; set; }
    }
}