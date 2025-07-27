using System;

namespace SeatingChartApp.Runtime.Data
{
    /// <summary>
    /// Represents a single guest record. This serializable class stores the
    /// personal details and timing information for an individual or party occupying a seat.
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

        // 🆕 NEW: Timestamps for analytics
        public DateTime TimeSeated;
        public DateTime TimeCleared;

        public GuestData()
        {
            // Generate a unique ID for each new guest instance for reliable tracking
            GuestID = Guid.NewGuid().ToString();
        }

        public GuestData(string firstName, string lastName, string roomNumber, int partySize, string guestID = "", string notes = "")
        {
            FirstName = firstName;
            LastName = lastName;
            RoomNumber = roomNumber;
            PartySize = partySize;
            GuestID = string.IsNullOrEmpty(guestID) ? Guid.NewGuid().ToString() : guestID;
            Notes = notes;
        }

        public override string ToString()
        {
            string idPart = !string.IsNullOrEmpty(GuestID) ? $" ID:{GuestID.Substring(0, 8)}" : string.Empty;
            string notesPart = !string.IsNullOrEmpty(Notes) ? $" Notes:{Notes}" : string.Empty;
            return $"{FirstName} {LastName}{idPart} (Room: {RoomNumber}, Party: {PartySize}){notesPart}";
        }
    }
}
