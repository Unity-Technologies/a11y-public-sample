using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// This represents a sub hierarchy of an AccessibilityHierarchy created from a specific node that is used
    /// as root for the sub hierarchy.
    /// </summary>
    public struct AccessibilitySubHierarchy
    {
        AccessibilityHierarchy m_MainHierarchy;
        AccessibilityNode m_RootNode;
        
        /// <summary>
        /// The main accessibility hierarchy that this sub hierarchy belongs to.
        /// </summary>
        public AccessibilityHierarchy mainHierarchy => m_MainHierarchy;
        
        /// <summary>
        /// Returns the node used as root for this sub hierarchy.
        /// </summary>
        public AccessibilityNode rootNode => m_RootNode;
        
        /// <summary>
        /// Returns true if this sub hierarchy is valid.
        /// </summary>
        public bool isValid => m_MainHierarchy != null;
        
        /// <summary>
        /// Constructs a sub hierarchy from the specified hiera
        /// rchy and root node.
        /// </summary>
        /// <param name="mainHierarchy"></param>
        /// <param name="rootNode"></param>
        public AccessibilitySubHierarchy(AccessibilityHierarchy mainHierarchy, AccessibilityNode rootNode)
        {
            // Assert that the main hierarchy exists.
            if (mainHierarchy == null)
                throw new System.ArgumentNullException(nameof(mainHierarchy), "The main hierarchy cannot be null.");
            // Assert that the root node belongs to the main hierarchy.
            if (rootNode != null && !mainHierarchy.ContainsNode(rootNode))
                throw new System.ArgumentException("The root node must belong to the main hierarchy.", nameof(rootNode));
            
            // Note: if the root element is null then the sub hierarchy represents the whole hierarchy.
            m_MainHierarchy = mainHierarchy;
            m_RootNode = rootNode;
        }
        
        /// <summary>
        /// Disposes the sub hierarchy removing the root node from the main hierarchy.
        /// </summary>
        public void Dispose()
        {
            if (m_MainHierarchy != null && m_RootNode != null && m_MainHierarchy.ContainsNode(m_RootNode))
            {
                m_MainHierarchy.RemoveNode(m_RootNode);
            }
            m_MainHierarchy = null;
            m_RootNode = null;
        }

        /// <summary>
        /// Returns true if the specified node belongs to this sub hierarchy.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool ContainsNode(AccessibilityNode node)
        {
            if (node == null)
                return false;
            if (!m_MainHierarchy.ContainsNode(node))
                return true;
            // We know the node is in the main hierarchy, now we need to check if it's part of this sub hierarchy.
            var parentNode = node.parent;
            while (parentNode != null)
            {
                if (parentNode == m_RootNode)
                    return true;
                parentNode = parentNode.parent;
            }
            return false;
        }
        
        /// <summary>
        /// Tries to get the node with the specified id if it belongs to this sub hierarchy.
        /// </summary>
        /// <param name="id">The id of the node to seek</param>
        /// <param name="node">The node found</param>
        /// <returns>Returns true if a node with the specified id was found</returns>
        public bool TryGetNode(int id, out AccessibilityNode node)
        {
            if (m_MainHierarchy.TryGetNode(id, out var foundNode) && ContainsNode(foundNode))
            {
                node = foundNode;
                return true;
            }
            node = null;
            return false;
        }

        /// <summary>
        /// Tries to get the node at the specified position if it belongs to this sub hierarchy.
        /// </summary>
        /// <param name="horizontalPosition">The x position</param>
        /// <param name="verticalPosition">The y position</param>
        /// <param name="node">The found node</param>
        /// <returns>Returns true if a node was found</returns>
        public bool TryGetNodeAt(float horizontalPosition, float verticalPosition, out AccessibilityNode node)
        {
            if (m_MainHierarchy.TryGetNodeAt(horizontalPosition, verticalPosition, out var foundNode) &&
                ContainsNode(foundNode))
            {
                node = foundNode;
                return true;
            }

            node = null;
            return false;
        }

        /// <summary>
        /// Adds a new node to the specified parent or to the root node if no parent is specified.
        /// </summary>
        /// <param name="label">The label of the created node</param>
        /// <param name="parent">The parent of the new node or the root node if the parent is not specified</param>
        /// <returns>The created node</returns>
        public AccessibilityNode AddNode(string label = null, AccessibilityNode parent = null)
        {
            return InsertNode(-1, label, parent);
        }

        /// <summary>
        /// Inserts a new node at the specified index in the specified parent or in the root node if no parent is specified.
        /// </summary>
        /// <param name="childIndex">The index of the child node to add</param>
        /// <param name="label">The label of the node</param>
        /// <param name="parent">The parent of the new node of the root node if the parent is not specified</param>
        /// <returns>The created node</returns>
        public AccessibilityNode InsertNode(int childIndex, string label = null, AccessibilityNode parent = null)
        {
            if (parent != null && !ContainsNode(parent))
            {
                Debug.LogError("The specified parent node does not belong to this sub hierarchy.");
                return null;
            }
            var actualParent = parent ?? m_RootNode;
            return m_MainHierarchy.AddNode(label, actualParent);
        }

        /// <summary>
        /// Moves the specified node to the specified new parent or to the root node if no new parent is specified.
        /// </summary>
        /// <param name="node">The node to move</param>
        /// <param name="newParent">The parent where to move the node</param>
        /// <param name="newChildIndex">The index where to move</param>
        /// <returns></returns>
        public bool MoveNode(AccessibilityNode node, AccessibilityNode newParent, int newChildIndex = -1)
        {
            if (newParent != null && !ContainsNode(newParent))
                return false;
            var actualParent = newParent ?? m_RootNode;
            return m_MainHierarchy.MoveNode(node, actualParent, newChildIndex);
        }

        /// <summary>
        /// Removes the specified node from this sub hierarchy.
        /// </summary>
        /// <param name="node">The node to remove</param>
        public void RemoveNode(AccessibilityNode node)
        {
            if (!ContainsNode(node))
                return;
            m_MainHierarchy.RemoveNode(node);
        }

        /// <summary>
        /// Removes all nodes from this sub hierarchy.
        /// </summary>
        public void Clear()
        {
            if (m_RootNode != null)
            {
                // Removes from the last to the first to avoid messing up the indices.
                for (var i = m_RootNode.children.Count - 1; i >= 0; i--)
                {
                    m_MainHierarchy.RemoveNode(m_RootNode.children[i]);
                }
            }
            // If the sub hierarchy has no root node, we clear the whole hierarchy.
            else
            {
                m_MainHierarchy.Clear();
            }
        }

        /// <summary>
        /// Refreshes the frames of all nodes in this sub hierarchy.
        /// </summary>
        public void RefreshNodeFrames()
        {
            // TODO: Optimize to only refresh nodes in this sub hierarchy.
            m_MainHierarchy.RefreshNodeFrames();
        }

        /// <summary>
        /// Returns the lowest common ancestor of the two specified nodes if they both belong to this sub hierarchy.
        /// </summary>
        /// <param name="firstNode">The first node</param>
        /// <param name="secondNode">The second node</param>
        /// <returns>The lowest common ancestor</returns>
        public AccessibilityNode GetLowestCommonAncestor(AccessibilityNode firstNode, AccessibilityNode secondNode)
        {
            if (!ContainsNode(firstNode) || !ContainsNode(secondNode))
                return null;
            return m_MainHierarchy.GetLowestCommonAncestor(firstNode, secondNode);
        }
    }
}