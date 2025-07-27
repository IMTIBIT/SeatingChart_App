using System.Collections.Generic;
using UnityEngine;
using SeatingChartApp.Runtime.Systems;
using System.Linq;
using System;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Manages the selection of multiple seats for group operations.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        public List<SeatController> SelectedSeats { get; private set; } = new List<SeatController>();
        public static event Action<List<SeatController>> OnSelectionChanged;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            ServiceProvider.Register(this);
            // Create a LineRenderer for visualizing the group
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false; // Use local space relative to the canvas
            _lineRenderer.startWidth = 10f;
            _lineRenderer.endWidth = 10f;
            _lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            _lineRenderer.startColor = new Color(1f, 1f, 0f, 0.7f);
            _lineRenderer.endColor = new Color(1f, 1f, 0f, 0.7f);
            _lineRenderer.positionCount = 0;
            _lineRenderer.sortingOrder = 5; // Ensure it renders above other UI
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<SelectionManager>();
        }

        private void Update()
        {
            // The line renderer needs to be updated every frame to follow the seats
            UpdateLineRenderer();
        }

        /// <summary>
        /// Adds or removes a seat from the current selection.
        /// </summary>
        public void ToggleSelection(SeatController seat)
        {
            if (SelectedSeats.Contains(seat))
            {
                SelectedSeats.Remove(seat);
            }
            else
            {
                SelectedSeats.Add(seat);
            }
            OnSelectionChanged?.Invoke(new List<SeatController>(SelectedSeats));
        }

        /// <summary>
        /// Clears the entire selection.
        /// </summary>
        public void ClearSelection()
        {
            if (SelectedSeats.Count == 0) return;
            SelectedSeats.Clear();
            OnSelectionChanged?.Invoke(new List<SeatController>());
        }

        /// <summary>
        /// 🆕 NEW: Assigns a single seat to the selection, clearing any previous selections.
        /// </summary>
        public void SelectOnly(SeatController seat)
        {
            // If the seat is already the only one selected, do nothing.
            if (SelectedSeats.Count == 1 && SelectedSeats[0] == seat) return;

            SelectedSeats.Clear();
            SelectedSeats.Add(seat);
            OnSelectionChanged?.Invoke(new List<SeatController>(SelectedSeats));
        }

        private void UpdateLineRenderer()
        {
            if (SelectedSeats.Count > 1)
            {
                // Sort seats by their horizontal position to draw a clean line
                var sortedSeats = SelectedSeats.OrderBy(s => s.transform.position.x).ToList();
                _lineRenderer.positionCount = sortedSeats.Count;
                for (int i = 0; i < sortedSeats.Count; i++)
                {
                    // Use the seat's local position within the canvas for the line
                    _lineRenderer.SetPosition(i, sortedSeats[i].GetComponent<RectTransform>().localPosition);
                }
            }
            else
            {
                _lineRenderer.positionCount = 0;
            }
        }
    }
}
