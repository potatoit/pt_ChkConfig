
using System.Collections.Generic;

namespace pt_ChkConfig
{
    public class miresults
    {
        public bool wasTerminated { get; set; }
        public string terminationReason { get; set; }
        public long nrOfSuccessfullTransactions { get; set; }
        public long nrOfNotProcessedTransactions { get; set; }
        public long nrOfFailedTransactions { get; set; }
        public string terminationErrorType { get; set; }
        public string bulkJobId { get; set; }

        public List<transactionResult> results { get; set; }
    }
}
