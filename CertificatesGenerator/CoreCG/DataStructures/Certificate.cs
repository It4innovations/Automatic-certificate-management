using System;
using System.Collections.Generic;
using System.Text;

namespace CoreCG.DataStructures
{
   public class Certificate
    {
        public string certificateRequest { get; set; }
        public string certificateType { get; set; }
        public string certificateValidity { get; set; }

        public string subjectLanguage { get; set; }

        public List<string> notificationMail { get; set; }

        public string requesterPhone { get; set; }
    }

    public class RequestAnswer
    {
        public string status { get; set; }

        public string id { get; set; }

        public string message { get; set; }

        public string detail { get; set; }
    }

    public class StatusAnswer
    {
        public string status { get; set; }

        public string certificate { get; set; }

        public string message { get; set; }

        public string detail { get; set; }
    }
}
