using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeatingChartApp.Runtime.Data
{
    [Serializable]
    public class SeatLayoutData
    {
        public List<SeatData> seats = new List<SeatData>();
    }

    [Serializable]
    public class SeatData
    {
        public int seatID;
        public Vector2 anchoredPosition;
        public SeatState state;
        public GuestData guest;
        public int capacity;

        // Prefab identifier used for restoration
        public string prefabKey;

        // 🆕 NEW: Rotation data
        public float rotation;
    }

    public enum SeatState
    {
        Available,
        Reserved,
        Occupied,
        Cleaning,
        OutOfService
    }
}
