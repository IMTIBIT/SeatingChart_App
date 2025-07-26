using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeatingChartApp.Runtime.Data
{
    /// <summary>
    /// Top‑level serializable container that holds the layout of all seats in
    /// the scene.  This is used by the LayoutManager to persist
    /// seat positions, states and guest assignments to disk between sessions.
    /// </summary>
    [Serializable]
    public class SeatLayoutData
    {
        public List<SeatData> seats = new List<SeatData>();
    }

    /// <summary>
    /// Represents a single seat's serializable state.  Each seat stores a
    /// unique identifier, its anchored position in the UI, its current
    /// SeatState, and an optional guest if the seat is occupied.  Vector2 is
    /// used for the anchored position because it is serializable by JsonUtility.
    /// </summary>
    [Serializable]
    public class SeatData
    {
        public int seatID;
        public Vector2 anchoredPosition;
        public SeatState state;
        public GuestData guest;
        /// <summary>
        /// Capacity of the seat stored for persistence.  Allows tables and
        /// chairs to be restored correctly.
        /// </summary>
        public int capacity;
    }

    /// <summary>
    /// Enumeration of all possible states a seat can be in.  Changing state
    /// affects the visual feedback presented to the user and the seat's
    /// interactability.  Note that 'Reserved' is included for potential
    /// expansion, although the primary workflow simply assigns seats as
    /// occupied.
    /// </summary>
    public enum SeatState
    {
        Available,
        Reserved,
        Occupied,
        Cleaning,
        OutOfService
    }
}