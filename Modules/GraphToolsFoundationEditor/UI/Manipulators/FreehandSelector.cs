// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manipulator to select elements by drawing a lasso around them.
    /// </summary>
    class FreehandSelector : MouseManipulator
    {
        static readonly List<ModelView> k_OnMouseUpAllUIs = new List<ModelView>();

        readonly FreehandElement m_FreehandElement;
        bool m_Active;
        GraphView m_GraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="FreehandSelector"/> class.
        /// </summary>
        public FreehandSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift | EventModifiers.Alt });
            m_FreehandElement = new FreehandElement();
            m_FreehandElement.StretchToParentSize();
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            m_GraphView = target as GraphView;
            if (m_GraphView == null)
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);

            m_GraphView = null;
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (e.target != target)
            {
                return;
            }

            if (CanStartManipulation(e))
            {
                m_GraphView.Dispatch(new ClearSelectionCommand());

                m_GraphView.ContentViewContainer.Add(m_FreehandElement);

                m_FreehandElement.Points.Clear();
                m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));
                m_FreehandElement.DeleteModifier = e.altKey;

                m_Active = true;
                target.CaptureMouse();
                e.StopImmediatePropagation();
            }
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            m_GraphView.ContentViewContainer.Remove(m_FreehandElement);

            m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));

            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ModelView> newSelection = new List<ModelView>();
            m_GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable())
                .GetAllViewsInList_Internal(m_GraphView, null, k_OnMouseUpAllUIs);
            foreach (var element in k_OnMouseUpAllUIs)
            {
                for (int i = 1; i < m_FreehandElement.Points.Count; i++)
                {
                    // Apply offset
                    Vector2 start = m_GraphView.ContentViewContainer.ChangeCoordinatesTo(element, m_FreehandElement.Points[i - 1]);
                    Vector2 end = m_GraphView.ContentViewContainer.ChangeCoordinatesTo(element, m_FreehandElement.Points[i]);
                    float minx = Mathf.Min(start.x, end.x);
                    float maxx = Mathf.Max(start.x, end.x);
                    float miny = Mathf.Min(start.y, end.y);
                    float maxy = Mathf.Max(start.y, end.y);

                    var rect = new Rect(minx, miny, maxx - minx + 1, maxy - miny + 1);
                    if (element.Overlaps(rect))
                    {
                        newSelection.Add(element);
                        break;
                    }
                }
            }
            k_OnMouseUpAllUIs.Clear();

            var selectedModels = newSelection.Where(elem => !(elem is Placemat)).Select(elem => elem.Model).OfType<GraphElementModel>().ToList();

            if (e.altKey)
            {
                m_GraphView.Dispatch(new DeleteElementsCommand(selectedModels));
            }
            else
            {
                m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, selectedModels));
            }

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the MouseMove event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_FreehandElement.Points.Add(m_GraphView.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, e.localMousePosition));
            m_FreehandElement.DeleteModifier = e.altKey;
            m_FreehandElement.MarkDirtyRepaint();

            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the KeyDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyDown(KeyDownEvent e)
        {
            if (m_Active)
                m_FreehandElement.DeleteModifier = e.altKey;
        }

        /// <summary>
        /// Callback for the KeyUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyUp(KeyUpEvent e)
        {
            if (m_Active)
                m_FreehandElement.DeleteModifier = e.altKey;
        }

        public void MarkDirtyRepaint()
        {
            m_FreehandElement?.MarkDirtyRepaint();
        }

        class FreehandElement : VisualElement
        {
            public List<Vector2> Points { get; } = new List<Vector2>();

            public FreehandElement()
            {
                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                generateVisualContent += GenerateVisualContent;
            }

            bool m_DeleteModifier;
            public bool DeleteModifier
            {
                private get { return m_DeleteModifier; }
                set
                {
                    if (m_DeleteModifier == value)
                        return;
                    m_DeleteModifier = value;
                    MarkDirtyRepaint();
                }
            }

            static readonly CustomStyleProperty<float> k_SegmentSizeProperty = new CustomStyleProperty<float>("--segment-size");
            static readonly CustomStyleProperty<Color> k_SegmentColorProperty = new CustomStyleProperty<Color>("--segment-color");
            static readonly CustomStyleProperty<Color> k_DeleteSegmentColorProperty = new CustomStyleProperty<Color>("--delete-segment-color");

            static float DefaultSegmentSize => 5f;
            static Color DefaultSegmentColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.38f);
                    }

                    return new Color(255 / 255f, 255 / 255f, 255 / 255f, 0.67f);
                }
            }

            static Color DefaultDeleteSegmentColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(1f, 0f, 0f);
                    }

                    return new Color(1f, 0f, 0f);
                }
            }

            public float SegmentSize { get; private set; } = DefaultSegmentSize;

            Color SegmentColor { get; set; } = DefaultSegmentColor;

            Color DeleteSegmentColor { get; set; } = DefaultDeleteSegmentColor;

            void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                ICustomStyle styles = e.customStyle;
                Color segmentColorValue;
                Color deleteColorValue;

                if (styles.TryGetValue(k_SegmentSizeProperty, out var segmentSizeValue))
                    SegmentSize = segmentSizeValue;

                if (styles.TryGetValue(k_SegmentColorProperty, out segmentColorValue))
                    SegmentColor = segmentColorValue;

                if (styles.TryGetValue(k_DeleteSegmentColorProperty, out deleteColorValue))
                    DeleteSegmentColor = deleteColorValue;
            }

            void GenerateVisualContent(MeshGenerationContext mgc)
            {
                if (Points.Count < 2)
                    return;

                var segmentSize = Mathf.Max(0.01f, SegmentSize / parent.transform.scale.x);
                var painter = mgc.painter2D;
                painter.strokeColor = DeleteModifier ? DeleteSegmentColor : SegmentColor;
                painter.lineWidth = 1.5f / parent.transform.scale.x;
                painter.BeginPath();
                var offset = 0f;

                for (var i = 0; i < Points.Count - 1; i++)
                {
                    offset = DrawDashedLine(painter, Points[i], Points[i + 1], segmentSize, offset);
                }
                painter.Stroke();
            }

            float DrawDashedLine(Painter2D painter, Vector2 p1, Vector2 p2, float segmentsLength, float offset = 0f)
            {
                // count how many segments are needed on this line. 1 segment = 1 dash or 1 gap
                var maxT = Vector2.Distance(p1, p2) / segmentsLength;

                // dashes are separated by 1, gaps too, starting with a dash
                // example with a distance of p1 to p2 being 7 times the segmentLength:
                //  (t)0  1  2  3  4  5  6  7
                // (p1)---   ---   ---   ---(p2)

                // Instead of 0 we start counting at -offset but only start the drawing at position 0
                // example with offset of 0.5f (meaning half a dash was already drawn in the previous line)
                // -1  t  0     1     2     3     4 (...)
                //        --      ------      ------(...)

                // skip a loop turn if we start by drawing a gap rather than a line
                var startT = offset >= 1 ? 2 - offset : -offset;
                for (var t = startT; t < maxT; t += 2f)
                {
                    painter.MoveTo(Vector2.Lerp(p1, p2, Mathf.Max(0, t / maxT)));
                    painter.LineTo(Vector2.Lerp(p1, p2, Mathf.Min(1, (t + 1f) / maxT)));
                }

                // return where we left, e.g. half a dash drawn -> 0.5f, dash + half a gap drawn: 1.5f
                return (maxT + offset) % 2f;
            }
        }
    }
}
