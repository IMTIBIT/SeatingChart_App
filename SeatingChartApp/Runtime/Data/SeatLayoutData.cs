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
        public string prefabKey;
        public float rotation;

        // 🆕 NEW: Stores the name of the parent area container
        public string parentAreaName;
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
