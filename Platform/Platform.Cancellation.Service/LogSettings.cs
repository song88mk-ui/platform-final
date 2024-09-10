using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Cancellation.Service
{
    internal class LogSettings
    {
        public bool IsDebug { get; set; }
        public string DirectoryPath { get; set; }

        private int _retentionInDays;
        public int RetentionInDays
        {
            get { return _retentionInDays; }
            set
            {
                _retentionInDays = value < 14 ? 14 : value;
            }
        }
    }
}
