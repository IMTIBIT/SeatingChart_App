using System;
using System.Collections.Generic;

namespace SeatingChartApp.Runtime.Data
{
    /// <summary>
    /// Represents a single day's worth of analytics data, including all guest
    /// interactions that occurred.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public string SessionDate;
        public List<GuestData> GuestInteractions;

        public SessionData()
        {
            SessionDate = DateTime.Now.ToString("yyyy-MM-dd");
            GuestInteractions = new List<GuestData>();
        }
    }
}
