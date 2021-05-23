using SugiBpm.Execution.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SugiBpm.Definition.Domain;
using SunStone.Data;
using Microsoft.Practices.ServiceLocation;
using SugiBpm.Delegation.Interface.Organization;
using SugiBpm.Delegation.Interface;
using System.ComponentModel.Composition;
using SugiBpm.Delegation.Domain;
using Serilog;
using System.Xml.Linq;
using System.ComponentModel.Composition.Hosting;

namespace SugiBpm.Execution.Test
{
    public class FakeDelegationHelper : IDelegationHelper
    {
        [ImportMany]
        public IEnumerable<System.Lazy<IActionHandler, IClassNameMetadata>> ActionHanders { get; set; }
        [ImportMany]
        public IEnumerable<System.Lazy<IDecisionHandler, IClassNameMetadata>> DecisionHandlers { get; set; }
        [ImportMany]
        public IEnumerable<System.Lazy<IForkHandler, IClassNameMetadata>> ForkHandlers { get; set; }
        [ImportMany]
        public IEnumerable<System.Lazy<IJoinHandler, IClassNameMetadata>> JoinHandlers { get; set; }
        [ImportMany]
        public IEnumerable<System.Lazy<ISerializer, IClassNameMetadata>> Serializers { get; set; }
        [ImportMany]
        public IEnumerable<System.Lazy<IAssignmentHandler, IClassNameMetadata>> AssignmentHandlers { get; set; }
        public FakeDelegationHelper()
        {
            try
            {
                DirectoryCatalog catalog = null;
                if (SunStone.Storage.Store.IsWebApplication)
                    catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory + "/bin");
                else
                    catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);

                var container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void DelegateAction(DelegationDef delegation, ExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public IActor DelegateAssignment(DelegationDef delegation, ExecutionContext executionContext)
        {
            IActor actor = null;
            try
            {
                foreach (var assignmentHandler in AssignmentHandlers)
                {
                    if ((string)assignmentHandler.Metadata.ClassName == delegation.ClassName)
                    {
                        executionContext.Configuration = (ParseConfiguration(delegation));
                        actor = assignmentHandler.Value.SelectActor(executionContext);
                    }
                }
            }
            catch (Exception t)
            {
                HandleException(delegation, executionContext, t);
            }

            return actor;
        }

        public Transition DelegateDecision(DelegationDef delegation, ExecutionContext executionContext)
        {
            IRepository<Transition> transitionRepository = ServiceLocator.Current.GetInstance<IRepository<Transition>>();
            //return transitionRepository.With(s => s.To).SingleOrDefault(s => s.To is EndState);
            return transitionRepository.With(s=>s.To).SingleOrDefault(s => s.To.Name == "approved holiday fork");
        }

        public void DelegateFork(DelegationDef delegation, ExecutionContext executionContext)
        {
            
        }

        public bool DelegateJoin(DelegationDef delegation, ExecutionContext executionContext)
        {
            return false;
        }

        public ISerializer DelegateSerializer(DelegationDef delegation)
        {
            throw new NotImplementedException();
        }

        private IDictionary<string, object> ParseConfiguration(DelegationDef delegation)
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            try
            {
                string configuration = delegation.Configuration;
                if (!string.IsNullOrEmpty(configuration))
                {
                    XElement xElement = XElement.Parse(configuration);

                    var parameterXmlElements = xElement.Elements("parameter");
                    foreach (XElement element in parameterXmlElements)
                    {
                        string name = element.Attribute("name").Value;
                        if (string.IsNullOrEmpty(name))
                        {
                            throw new SystemException("invalid delegation-configuration : " + configuration);
                        }

                        parameters.Add(name, element.Value);
                    }
                }
            }
            catch (Exception t)
            {
                Log.Error("can't parse configuration : ", t);
                throw new SystemException("can't parse configuration : " + t.Message);
            }

            return parameters;
        }
        private void HandleException(DelegationDef delegation, ExecutionContext executionContext, Exception exception)
        {
            Log.Debug("handling delegation exception :", exception);

            string exceptionClassName = exception.GetType().FullName;
            string delegationClassName = delegation.ClassName;

            ExceptionHandlingType exceptionHandlingType = delegation.ExceptionHandlingType;

            if (exceptionHandlingType != 0)
            {
                if (exceptionHandlingType == ExceptionHandlingType.IGNORE)
                {
                    Log.Debug("ignoring '" + exceptionClassName + "' in delegation '" + delegationClassName + "' : " + exception.Message);
                }
                else if (exceptionHandlingType == ExceptionHandlingType.LOG)
                {
                    Log.Debug("logging '" + exceptionClassName + "' in delegation '" + delegationClassName + "' : " + exception.Message);
                    //executionContext.AddLogDetail(new ExceptionReportImpl(exception));
                }
                else if (exceptionHandlingType == ExceptionHandlingType.ROLLBACK)
                {
                    Log.Debug("rolling back for '" + exceptionClassName + "' in delegation '" + delegationClassName + "' : " + exception.Message);
                    throw new SystemException("rolling back for '" + exceptionClassName + "' in delegation '" + delegationClassName + "' : " + exception.Message);
                }
                else
                {
                    throw new SystemException("unknown exception handler '" + exceptionHandlingType + "' : " + exception.Message);
                }
            }
            else
            {
                Log.Debug("'" + exceptionClassName + "' in delegation '" + delegationClassName + "' : " + exception.Message);
                //executionContext.AddLogDetail(new ExceptionReportImpl(exception));
            }
        }
    }
}