using System;
using UnityEngine;

namespace SeatingChartApp.Runtime.Data
{
    /// <summary>
    /// Represents a single guest record.  This serializable class stores the
    /// personal details for an individual or party occupying a seat.  It
    /// intentionally avoids any behaviour so that it can be easily persisted
    /// with JsonUtility.
    /// </summary>
    [Serializable]
    public class GuestData
    {
        public string FirstName;
        public string LastName;
        public string RoomNumber;
        public int PartySize;

        // Optional additional data for extended functionality
        public string GuestID;
        public string Notes;

        public GuestData() { }

        public GuestData(string firstName, string lastName, string roomNumber, int partySize, string guestID = "", string notes = "")
        {
            FirstName = firstName;
            LastName = lastName;
            RoomNumber = roomNumber;
            PartySize = partySize;
            GuestID = guestID;
            Notes = notes;
        }

        public override string ToString()
        {
            string idPart = !string.IsNullOrEmpty(GuestID) ? $" ID:{GuestID}" : string.Empty;
            string notesPart = !string.IsNullOrEmpty(Notes) ? $" Notes:{Notes}" : string.Empty;
            return $"{FirstName} {LastName}{idPart} (Room: {RoomNumber}, Party: {PartySize}){notesPart}";
        }
    }
}