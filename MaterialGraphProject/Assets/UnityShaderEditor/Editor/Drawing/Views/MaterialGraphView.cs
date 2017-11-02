using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public AbstractMaterialGraph graph { get; private set; }
public MaterialGraphView() // FIXME constructor deleted
        {
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            }

        public override List<NodeAnchor> GetCompatibleAnchors(NodeAnchor startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<NodeAnchor>();
            var startSlot = startAnchor.userData as MaterialSlot;
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.shaderStage;
            if (startStage == ShaderStage.Dynamic)
                startStage = NodeUtils.FindEffectiveShaderStage(startSlot.owner, startSlot.isOutputSlot);

            foreach (var candidateAnchor in anchors.ToList())
            {
                var candidateSlot = candidateAnchor.userData as MaterialSlot;
                if (!startSlot.IsCompatibleWith(candidateSlot))
                    continue;

                if (startStage != ShaderStage.Dynamic)
                {
                    var candidateStage = candidateSlot.shaderStage;
                    if (candidateStage == ShaderStage.Dynamic)
                        candidateStage = NodeUtils.FindEffectiveShaderStage(candidateSlot.owner, !startSlot.isOutputSlot);
                    if (candidateStage != ShaderStage.Dynamic && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
        }

        public delegate void OnSelectionChanged(IEnumerable<INode> nodes);

        public OnSelectionChanged onSelectionChanged;

        public MaterialGraphView(AbstractMaterialGraph graph)
        {
            this.graph = graph;
        }

        void SelectionChanged()
        {
            var selectedNodes = selection.OfType<MaterialNodeView>().Where(x => x.userData is INode);
            if (onSelectionChanged != null)
                onSelectionChanged(selectedNodes.Select(x => x.userData as INode));
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionChanged();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionChanged();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            SelectionChanged();
        }
    }

    public static class GraphViewExtensions
    {
        internal static CopyPasteGraph SelectionAsCopyPasteGraph(this MaterialGraphView graphView)
        {
            return new CopyPasteGraph(graphView.selection.OfType<MaterialNodeView>().Select(x => (INode) x.node), graphView.selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>());
        }

        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            using (var remappedNodesDisposable = ListPool<INode>.GetDisposable())
            using (var remappedEdgesDisposable = ListPool<IEdge>.GetDisposable())
            {
                var remappedNodes = remappedNodesDisposable.value;
                var remappedEdges = remappedEdgesDisposable.value;
                copyGraph.InsertInGraph(graphView.graph, remappedNodes, remappedEdges);

                // Add new elements to selection
                graphView.ClearSelection();
                graphView.graphElements.ForEach(element =>
                {
                    var edge = element as Edge;
                    if (edge != null && remappedEdges.Contains(edge.userData as IEdge))
                        graphView.AddToSelection(edge);

                    var nodeView = element as MaterialNodeView;
                    if (nodeView != null && remappedNodes.Contains(nodeView.node))
                        graphView.AddToSelection(nodeView);
                });
            }
        }

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var graph = CreateCopyPasteGraph(elements);
            return JsonUtility.ToJson(graph, true);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            return DeserializeCopyBuffer(serializedData) != null;
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            var mgp = GetPresenter<MaterialGraphPresenter>();
            mgp.graph.owner.RegisterCompleteObjectUndo(operationName);
            var pastedGraph = DeserializeCopyBuffer(serializedData);
            mgp.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            var mgp = GetPresenter<MaterialGraphPresenter>();
            mgp.graph.owner.RegisterCompleteObjectUndo(operationName);
            mgp.RemoveElements(selection.OfType<MaterialNodeView>(), selection.OfType<Edge>());
        }

        internal static CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElement> selection)
        {
            var graph = new CopyPasteGraph();
            foreach (var element in selection)
            {
                var nodeView = element as MaterialNodeView;
                if (nodeView != null)
                {
                    graph.AddNode(nodeView.node);
                    foreach (var edge in NodeUtils.GetAllEdges(nodeView.userData as INode))
                        graph.AddEdge(edge);
                }

                var edgeView = element as Edge;
                if (edgeView != null)
                    graph.AddEdge(edgeView.userData as IEdge);
            }
            return graph;
        }

        internal static CopyPasteGraph DeserializeCopyBuffer(string copyBuffer)
        {
            try
            {
                return JsonUtility.FromJson<CopyPasteGraph>(copyBuffer);
            }
            catch
            {
                // ignored. just means copy buffer was not a graph :(
                return null;
            }
        }


    }
}
