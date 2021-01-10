using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace pt_ChkConfig
{
    public class SortingOrderDefinition
    {
        public string TableName { get; set; }
        public string SortingOrder { get; set; }
        public List<string> Fields { get; set; } = new List<string>();

        public SortingOrderDefinition()
        {

        }
        public SortingOrderDefinition(transactionResult aTransaction, string aTableName, string aSortingOrder)
        {
            TableName = aTableName;
            SortingOrder = aSortingOrder;

            if(null != aTransaction && null != aTransaction.records && aTransaction.records.Length > 0 && null != aTransaction.records[0])
            {
                foreach (string currentKey in aTransaction.records[0].Keys)
                {
                    string value = null;
                    aTransaction.records[0].TryGetValue(currentKey, out value);

                    if(false == string.IsNullOrEmpty(value))
                    {
                        if(true == Regex.IsMatch(currentKey, "^(KE([Y0-9]{2}))"))
                        {
                            Fields.Add(value);
                        }
                        //if (currentKey.Contains("KEY"))
                        //{
                        //    Fields.Add(value);
                        //}
                    }
                }
            }
        }


        public string[] getCleanFields()
        {
            string[] results = null;

            if(null != Fields && Fields.Count > 0)
            {
                results = new string[Fields.Count];

                for(int i = 0; i < Fields.Count; i++)
                {
                    if(Fields[i].Length > 4)
                    {
                        results[i] = Fields[i].Substring(2);
                    }
                    else
                    {
                        results[i] = Fields[i];
                    }
                }
            }

            return (results);
        }


    }
}
