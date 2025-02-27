// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base model that represents a dynamically defined node.
    /// </summary>
    [Serializable]
    abstract class NodeModel : InputOutputPortsNodeModel, IHasProgress, ICollapsible
    {
        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, Constant> m_InputConstantsById;

        [SerializeField, HideInInspector]
        ModelState m_State;

        protected OrderedPorts m_InputsById;
        protected OrderedPorts m_OutputsById;
        protected OrderedPorts m_PreviousInputs;
        protected OrderedPorts m_PreviousOutputs;

        [SerializeField, HideInInspector]
        bool m_Collapsed;

        /// <inheritdoc />
        public override string IconTypeString => "node";

        /// <inheritdoc />
        public override ModelState State
        {
            get => m_State;
            set => m_State = value;
        }

        /// <inheritdoc />
        public override bool AllowSelfConnect => false;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> InputsById => m_InputsById;

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, PortModel> OutputsById => m_OutputsById;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> InputsByDisplayOrder => m_InputsById;

        /// <inheritdoc />
        public override IReadOnlyList<PortModel> OutputsByDisplayOrder => m_OutputsById;

        public IReadOnlyDictionary<string, Constant> InputConstantsById => m_InputConstantsById;

        /// <inheritdoc />
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
            }
        }

        /// <inheritdoc />
        public virtual bool HasProgress => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeModel"/> class.
        /// </summary>
        protected NodeModel()
        {
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_InputConstantsById = new SerializedReferenceDictionary<string, Constant>();
        }

        internal void ClearPorts_Internal()
        {
            m_InputsById = new OrderedPorts();
            m_OutputsById = new OrderedPorts();
            m_PreviousInputs = null;
            m_PreviousOutputs = null;
        }

        /// <summary>
        /// Instantiates the ports of the nodes.
        /// </summary>
        public void DefineNode()
        {
            OnPreDefineNode();

            m_PreviousInputs = m_InputsById;
            m_PreviousOutputs = m_OutputsById;
            m_InputsById = new OrderedPorts(m_InputsById?.Count ?? 0);
            m_OutputsById = new OrderedPorts(m_OutputsById?.Count ?? 0);

            OnDefineNode();

            RemoveObsoleteWiresAndConstants();
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/> before the <see cref="OrderedPorts"/> lists are modified.
        /// </summary>
        protected virtual void OnPreDefineNode()
        {
        }

        /// <summary>
        /// Called by <see cref="DefineNode"/>. Override this function to instantiate the ports of your node type.
        /// </summary>
        protected abstract void OnDefineNode();

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            DefineNode();
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);

            DefineNode();
            CloneInputConstants();
        }

        void RemoveObsoleteWiresAndConstants()
        {
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, PortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
            }

            foreach (var kv in m_PreviousOutputs
                     .Where<KeyValuePair<string, PortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
            }

            // remove input constants that aren't used
            var idsToDeletes = m_InputConstantsById
                .Select(kv => kv.Key)
                .Where(id => !m_InputsById.ContainsKey(id)).ToList();
            foreach (var id in idsToDeletes)
            {
                m_InputConstantsById.Remove(id);
            }
        }

        static PortModel ReuseOrCreatePortModel(PortModel model, IReadOnlyDictionary<string, PortModel> previousPorts, OrderedPorts newPorts)
        {
            // reuse existing ports when ids match, otherwise add port
            if (previousPorts.TryGetValue(model.UniqueName, out var portModelToAdd))
            {
                if (portModelToAdd is IHasTitle toAddHasTitle && model is IHasTitle hasTitle)
                {
                    toAddHasTitle.Title = hasTitle.Title;
                }
                portModelToAdd.DataTypeHandle = model.DataTypeHandle;
                portModelToAdd.PortType = model.PortType;
            }
            else
            {
                portModelToAdd = model;
            }

            newPorts.Add(portModelToAdd);
            return portModelToAdd;
        }

        /// <summary>
        /// Creates a new port on the node.
        /// </summary>
        /// <param name="direction">The direction of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="portName">The name of the port to create.</param>
        /// <param name="portType">The type of port to create.</param>
        /// <param name="dataType">The type of data the new port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options of the port model to create.</param>
        /// <returns>The newly created port model.</returns>
        protected virtual PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new PortModel(this, direction, orientation, portName, portType, dataType, portId, options);
        }

        /// <summary>
        /// Deletes all the wires connected to a given port.
        /// </summary>
        /// <param name="portModel">The port model to disconnect.</param>
        protected virtual void DisconnectPort(PortModel portModel)
        {
            if (GraphModel != null)
            {
                var wireModels = GraphModel.GetWiresForPort(portModel);
                GraphModel.DeleteWires(wireModels.ToList());
            }
        }

        /// <inheritdoc />
        public override PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<Constant> initializationCallback = null)
        {
            var portModel = CreatePort(PortDirection.Input, orientation, portName, portType, dataType, portId, options);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, initializationCallback);
            return portModel;
        }

        /// <inheritdoc />
        public override PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, PortOrientation orientation = PortOrientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default)
        {
            var portModel = CreatePort(PortDirection.Output, orientation, portName, portType, dataType, portId, options);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant to be called right after the constant is created.</param>
        protected void UpdateConstantForInput(PortModel inputPort, Action<Constant> initializationCallback = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById.Remove(id);
                return;
            }

            if (m_InputConstantsById.TryGetValue(id, out var constant))
            {
                // Destroy existing constant if not compatible
                var embeddedConstantType = GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle);
                Type portDefinitionType;
                if (embeddedConstantType != null)
                {
                    var instance = (Constant)Activator.CreateInstance(embeddedConstantType);
                    portDefinitionType = instance.Type;
                }
                else
                {
                    portDefinitionType = inputPort.DataTypeHandle.Resolve();
                }

                if (!constant.Type.IsAssignableFrom(portDefinitionType) || (constant.Type.IsEnum && constant.Type != portDefinitionType))
                {
                    m_InputConstantsById.Remove(id);
                }
            }

            // Create new constant if needed
            if (!m_InputConstantsById.ContainsKey(id)
                && inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown
                && GraphModel.Stencil.GetConstantType(inputPort.DataTypeHandle) != null)
            {
                var embeddedConstant = GraphModel.Stencil.CreateConstantValue(inputPort.DataTypeHandle);
                initializationCallback?.Invoke(embeddedConstant);
                m_InputConstantsById[id] = embeddedConstant;
                GraphModel.Asset.Dirty = true;
            }
        }

        public ConstantNodeModel CloneConstant(ConstantNodeModel source)
        {
            var clone = Activator.CreateInstance(source.GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
            return (ConstantNodeModel)clone;
        }

        public void CloneInputConstants()
        {
            foreach (var id in m_InputConstantsById.Keys.ToList())
            {
                var inputConstant = m_InputConstantsById[id];
                var newConstant = inputConstant.Clone();
                m_InputConstantsById[id] = newConstant;
                GraphModel.Asset.Dirty = true;
            }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_PreviousInputs = null;
            m_PreviousOutputs = null;
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();

            // DefineNode() will be called by the GraphModel.
        }

        /// <inheritdoc />
        public override bool RemoveUnusedMissingPort(PortModel portModel)
        {
            if (portModel.PortType != PortType.MissingPort || portModel.GetConnectedWires().Any())
                return false;

            return portModel.Direction == PortDirection.Input ? m_InputsById.Remove(portModel) : m_OutputsById.Remove(portModel);
        }
    }
}
