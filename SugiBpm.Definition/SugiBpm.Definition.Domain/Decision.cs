using System;
using System.Xml;

namespace SugiBpm.Definition.Domain
{
    /// <summary>
    /// 决心、决议
    /// </summary>
    public class Decision : Node
    {
        public Guid? DecisionDelegationId { get; set; }
        public DelegationDef DecisionDelegation { get; set; }

        public Decision() : base()
        {
        }

        public Decision(ProcessDefinitionCreationContext creationContext) : base(creationContext)
        {
        }

        public override void ReadProcessData(XmlElement xmlElement)
        {
            base.ReadProcessData(xmlElement);
            creationContext.DelegatingObject = this;
            this.DecisionDelegation = new DelegationDef(creationContext);
            this.DecisionDelegation.ReadProcessData(xmlElement);
            creationContext.DelegatingObject = null;
        }
    }
}
