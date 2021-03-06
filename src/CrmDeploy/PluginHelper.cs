﻿using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using CrmDeploy.Connection;
using CrmDeploy.Entities;
using Microsoft.Xrm.Client.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace CrmDeploy
{
    public class PluginHelper
    {
        private ICrmServiceProvider _ServiceProvider;

        public PluginHelper(ICrmServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        public EntityExists DoesPluginAssemblyExist(AssemblyName assemblyName)
        {
            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                var query = new QueryByAttribute(PluginAssembly.EntityLogicalName);
                query.ColumnSet = new ColumnSet(true);
                query.Attributes.AddRange("name");
                query.Values.AddRange(assemblyName.Name);
                var results = orgService.RetrieveMultiple(query);
                if (results.Entities != null && results.Entities.Count > 0)
                {
                    var reference = new EntityReference(PluginAssembly.EntityLogicalName, results.Entities[0].Id);
                    return EntityExists.Yes(reference);
                }
                else
                {
                    return EntityExists.No();
                }
            }
        }
        
        public void CleanOutPlugin(PluginAssemblyRegistration par)
        {
            using (var orgService = (OrganizationServiceContext) _ServiceProvider.GetOrganisationService())
            {
                foreach (var ptr in par.PluginTypeRegistrations)
                {
                    var e = DoesPluginTypeExist(ptr.PluginType.TypeName);
                    if(e.Exists == false)
                        continue;

                    DeleteStepsForPlugin(e.EntityReference.Id);

                    var q = new QueryByAttribute(PluginType.EntityLogicalName);
                    q.ColumnSet = new ColumnSet(true);
                    q.Attributes.AddRange("typename");
                    q.Values.AddRange(ptr.PluginType.TypeName);
                    var res = orgService.RetrieveMultiple(q);
                    if (res.Entities != null)
                    {
                        foreach (var entity in res.Entities)
                        {
                            orgService.Attach(entity);
                            orgService.DeleteObject(entity);
                        }
                    }
                }

                orgService.SaveChanges();
            }

        }

        public EntityExists DoesPluginAssemblyExist(string pluginName)
        {
            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                var query = new QueryByAttribute(PluginAssembly.EntityLogicalName);
                query.ColumnSet = new ColumnSet(true);
                query.Attributes.AddRange("name");
                query.Values.AddRange(pluginName);
                var results = orgService.RetrieveMultiple(query);
                if (results.Entities != null && results.Entities.Count > 0)
                {
                    var reference = new EntityReference(PluginAssembly.EntityLogicalName, results.Entities[0].Id);
                    return EntityExists.Yes(reference);
                }
                else
                {
                    return EntityExists.No();
                }
            }
        }

        public EntityExists DoesPluginTypeExist(string typename)
        {
            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                var query = new QueryByAttribute(PluginType.EntityLogicalName);
                query.ColumnSet = new ColumnSet(true);
                query.Attributes.AddRange("typename");
                query.Values.AddRange(typename);
                var results = orgService.RetrieveMultiple(query);
                if (results.Entities != null && results.Entities.Count > 0)
                {
                    var reference = new EntityReference(PluginType.EntityLogicalName, results.Entities[0].Id);
                    return EntityExists.Yes(reference);
                }
                else
                {
                    return EntityExists.No();
                }
            }
        }

        public Guid RegisterAssembly(PluginAssembly pluginAssembly)
        {
            try
            {
                using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
                {
                    var response = (CreateResponse)orgService.Execute(new CreateRequest() { Target = pluginAssembly });
                    return response.id;
                }
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>)
            {
                throw;
            }
        }

        public Guid RegisterType(PluginType pluginType)
        {
            try
            {
                using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
                {
                    var response = (CreateResponse)orgService.Execute(new CreateRequest() { Target = pluginType });
                    return response.id;
                }
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>)
            {
                throw;
            }
        }

        public Guid RegisterStep(SdkMessageProcessingStep step)
        {
            try
            {
                using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
                {
                    if (step.plugintype_sdkmessageprocessingstep != null)
                    {
                        if (!orgService.IsAttached(step.plugintype_sdkmessageprocessingstep))
                        {
                            orgService.Attach(step.plugintype_sdkmessageprocessingstep);
                        }

                    }

                    orgService.AddObject(step);
                    orgService.SaveChanges();
                    return step.Id;
                    //  var response = (CreateResponse)orgService.Execute(new CreateRequest() { Target = step });
                    //return response.id;
                }
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>)
            {
                throw;
            }
        }

        public void DeleteStepsForPlugin(Guid pluginId)
        {

            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                var sdkMessageFilters = from s in orgService.CreateQuery("sdkmessageprocessingstep")
                                        where (Guid)s["plugintypeid"] == pluginId
                                        select s;

                foreach (var result in sdkMessageFilters)
                {
                    orgService.DeleteObject(result);
                }
                orgService.SaveChanges();

            }
        }

        public Guid GetSdkMessageFilterId(string primaryEntity, string secondaryEntity, Guid messageId)
        {
            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                if (string.IsNullOrEmpty(secondaryEntity) || (!string.IsNullOrEmpty(secondaryEntity) && secondaryEntity.ToLower() == "none"))
                {
                    // only a primary entity.
                    var sdkMessageFilters = from s in orgService.CreateQuery("sdkmessagefilter")
                                            where (string)s["primaryobjecttypecode"] == primaryEntity &&
                                            (Guid)s["sdkmessageid"] == messageId
                                            select s;
                    return sdkMessageFilters.First().Id;
                }

                var query = from s in orgService.CreateQuery("sdkmessagefilter")
                            where (string)s["primaryobjecttypecode"] == primaryEntity &&
                              (string)s["secondaryobjecttypecode"] == secondaryEntity &&
                             (Guid)s["sdkmessageid"] == messageId
                            select s;
                return query.First().Id;
            }
        }

        public Guid GetMessageId(string sdkMessageName)
        {
            using (var orgService = (OrganizationServiceContext)_ServiceProvider.GetOrganisationService())
            {
                var sdkMessages = from s in orgService.CreateQuery("sdkmessage")
                                  where (string)s["name"] == sdkMessageName
                                  select s;
                return sdkMessages.First().Id;
            }
        }

    }
}
