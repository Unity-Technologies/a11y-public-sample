using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.LetterSpell
{
    public class AccessibilityHierarchyVisitor
    {
        public void Visit(AccessibilityHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                return;
            }

            VisitNodes(hierarchy.rootNodes);
        }

        void VisitNodes(IReadOnlyList<AccessibilityNode> nodes)
        {
            foreach (var node in nodes)
            {
                BeginVisit(node);
                VisitNode(node);
                VisitNodes(node.children);
                EndVisit(node);
            }
        }

        protected virtual void BeginVisit(AccessibilityNode node)
        {
        }

        protected virtual void EndVisit(AccessibilityNode node)
        {
        }

        protected virtual void VisitNode(AccessibilityNode node)
        {
        }
    }
}
