using System.Collections.Generic;

namespace pt_ChkConfig
{
    public class transactionResult
    {
        public string transaction { get; set; }
        public string errorMessage { get; set; }
        public string errorCode { get; set;        }
        public string errorCfg { get; set; }
        public string errorField { get; set; }
        public string errorType { get; set; }
        public bool notProcessed { get; set; }

        //public KeyValuePair<string, string>[] records { get; set; }
        public Dictionary<string, string>[] records { get; set; }
        //public List

    }
}
