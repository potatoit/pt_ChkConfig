using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace pt_ChkConfig
{
    class Program
    {
        static string usage = @"pt_ChkConfig
This application will look at an exported config file from the M3 Business Engine and output the sorting orders in the file.  You must use the .zip file.
If the /checksortorder is selected and an ionapi file specified, it will also check M3 to see if that sorting order matches.
Arguments:
  /file <path to file or directory> - if a directory it will scan the files in that directory
  /ionapi <path to ionapi file> - this is the path to a valid .ionapi with a service account
  /checksortorder - this will check M3 if the .ionapi file is specified
  /createsortingorder - this will create the sorting order, but not activate it
  /pause - if this argument is specified, it will pause the output before exiting the program
  /st - suppress the prining of the tenant from IONAPI file
  /processFile - attempt to process the file by calling the APIs, this should not be done unless you really know what you are doing it can corrupt data
  /step
";
        APICalls apiService = null;
        static void Main(string[] args)
        {
            // file <path>
            // ionapi <path>
            // /checksortorder
            // /pause
            if (args.Length > 0)
            {
                List<string> files = new List<string>();
                string ionapiFile = "";
                bool checkSortingOrder = false;
                bool createSortingOrder = false;
                bool pause = false;
                bool supressTenant = false;
                bool processFile = false;
                bool step = false;
                for (int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];
                    if (false == string.IsNullOrEmpty(argument))
                    {
                        if ((argument.Trim() == "/file" || argument.Trim() == "/files") && args.Length > (i + 1))
                        {
                            if (File.Exists(args[i + 1]))
                            {
                                files.Add(args[i + 1]);
                            }
                            else if (Directory.Exists(args[i + 1]))
                            {
                                string[] directories = Directory.GetFiles(args[i + 1]);
                                if (null != directories && directories.Length > 0)
                                {
                                    files.AddRange(directories);
                                }
                            }
                            i++;
                        }
                        else if (argument.Trim() == "/ionapi" && args.Length > (i + 1))
                        {
                            if (File.Exists(args[i + 1]))
                            {
                                ionapiFile = args[i + 1];
                            }
                            i++;
                        }
                        else if (argument.Trim() == "/checksortorder")
                        {
                            checkSortingOrder = true;
                        }
                        else if (argument.Trim() == "/createsortingorder")
                        {
                            createSortingOrder = true;
                        }
                        else if (argument.Trim() == "/pause")
                        {
                            pause = true;
                        }
                        else if (argument.Trim() == "/st")
                        {
                            //
                            supressTenant = true;
                        }
                        else if (argument.Trim() == "/processFile")
                        {
                            processFile = true;
                        }
                        else if(argument.Trim() == "/step")
                        {
                            step = true;
                        }
                    }
                }
                if (files.Count > 0)
                {
                    Program program = new Program();
                    List<SortingOrderDefinition> sortingOrderDefinitions = program.readFiles(files);
                    if (sortingOrderDefinitions != null && true == checkSortingOrder && null != ionapiFile)
                    {
                        program.apiService = (APICalls)(new APICalls()).Load(ionapiFile);
                        if (null != program.apiService)
                        {
                            if (!supressTenant) Console.WriteLine("Checking Tenant: " + program.apiService.getTenant());
                            foreach (SortingOrderDefinition currentSortingOrder in sortingOrderDefinitions)
                            {
                                program.checkM3SortingOrders(currentSortingOrder, createSortingOrder);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Failed to load ionapi file");
                        }
                    }
                    else if(sortingOrderDefinitions != null && true == processFile && null != ionapiFile)
                    {
                        program.apiService = (APICalls)(new APICalls()).Load(ionapiFile);

                        if(null != program.apiService)
                        {
                            Console.WriteLine("This is experimental and completely unsupported and may result in broken data in your system");
                            Console.WriteLine("Do not use this unless you are absolutely certain you know what you are doing");
                            Console.WriteLine("This is used to isolate specific issues in the loading process");
                            Console.Write("Are you sure you want to continue? y/N: ");
                            ConsoleKeyInfo keyRead = Console.ReadKey();
                            Console.WriteLine("");
                            if (keyRead.KeyChar == "Y"[0] || keyRead.KeyChar == "y"[0])
                            {
                                program.processFiles(files, step);
                            }
                            else
                            {
                                Console.WriteLine("Terminated");
                            }
                        }
                    }
                }
                if (pause)
                {
                    Console.WriteLine("Press enter to continue");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.Write(usage);
                Console.WriteLine("");
            }

        }

        public void processFiles(List<string> aFiles, bool aStep)
        {
            bool cancel = false;
            if (null != aFiles && aFiles.Count > 0)
            {
                foreach (string file in aFiles)
                {
                    if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        using (ZipArchive zip = ZipFile.OpenRead(file))
                        {
                            foreach (ZipArchiveEntry currentFile in zip.Entries)
                            {
                                if (currentFile.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                                {
                                    string extractPath = Path.GetTempFileName();
                                    currentFile.ExtractToFile(extractPath, true);

                                    if (File.Exists(extractPath))
                                    {
                                        cancel = processFile(extractPath, aStep);
                                    }
                                    File.Delete(extractPath);
                                    if(cancel)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool processFile(string aPath, bool aStep)
        {
            bool cancelAll = false;
            try
            {
                XElement document = XElement.Load(aPath);
                if (null != document)
                {
                    IEnumerable<XElement> steps = document.Descendants("Content")?.Elements();

                    if (null != steps && steps.Count() > 0)
                    {
                        foreach (XElement currentStep in steps)
                        {
                            string apiProgram = currentStep.Element("APIProgram")?.Value;
                            string transaction = currentStep.Element("Transaction")?.Value;
                            IEnumerable<XElement> fieldElements = currentStep.Elements("Fields");
                            Dictionary<string, string> fields = new Dictionary<string, string>();

                            if (null != fieldElements && fieldElements.Count() > 0)
                            {
                                foreach (XElement currentTable in fieldElements)
                                {
                                    if(currentTable.HasElements)
                                    {
                                        foreach (XElement currentField in currentTable.Elements())
                                        {
                                            string fieldName = currentField.Name.ToString();

                                            if (fieldName.Length > 2)
                                            {
                                                // get the last 4 characters of the field name
                                                fieldName = fieldName.Substring(fieldName.Length - 4);
                                                string value = currentField.Value;
                                                if (!string.IsNullOrWhiteSpace(value))
                                                {
                                                    fields.Add(fieldName, value.TrimEnd());
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                            if(null != apiService)
                            {
                                bool execute = true;
                                string dataUrl = apiService.generateGenericCallUrl(apiProgram, transaction, fields);
                                Console.WriteLine(dataUrl);
                                if(aStep)
                                {
                                    Console.Write("Execute Y/n/c(ancel)/a(ll): ");
                                    int cursorRow = Console.CursorTop;
                                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                                    Console.SetCursorPosition(0, cursorRow);
                                    Console.Write(new string(' ', Console.WindowWidth));
                                    Console.SetCursorPosition(0, cursorRow);

                                    if (keyInfo.KeyChar == "n"[0] || keyInfo.KeyChar == "N"[0])
                                    {
                                        execute = false;
                                    }
                                    else if (keyInfo.KeyChar == "c"[0] || keyInfo.KeyChar == "C"[0])
                                    {
                                        execute = false;
                                        cancelAll = true;
                                        break;
                                    }
                                    else if (keyInfo.KeyChar == "a"[0] || keyInfo.KeyChar == "A"[0])
                                    {
                                        aStep = false;
                                    }
                                }
                                if (execute)
                                {
                                    string callResult = apiService.genericCalls(dataUrl);
                                    string outputResult = callResult;
                                    if(!string.IsNullOrEmpty(callResult))
                                    {
                                        try
                                        {
                                            miresults apiResults = JsonConvert.DeserializeObject<miresults>(callResult);
                                            if(null != apiResults.results && apiResults.results.Count > 0)
                                            {
                                                if(false == string.IsNullOrEmpty(apiResults.results[0].errorMessage) || false == string.IsNullOrEmpty(apiResults.results[0].errorCode))
                                                {
                                                    outputResult = "NOK " + apiResults.results[0].errorMessage + " [" + apiResults.results[0].errorCode + "]";
                                                }
                                                else
                                                {
                                                    outputResult = "OK";
                                                }
                                            }
                                        }
                                        catch (Exception deserialiseEx)
                                        {
                                            outputResult += deserialiseEx;
                                        }
                                    }
                                    Console.WriteLine(" +-- " + outputResult);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return (cancelAll);
        }

        private SortingOrderDefinition handleCheckSortingOptions(string aPath)
        {
            SortingOrderDefinition result = null;

            try
            {
                XElement document = XElement.Load(aPath);
                if (null != document)
                {
                    IEnumerable<XElement> checkSortOption = document.Descendants("Check");

                    if (null != checkSortOption && checkSortOption.Count() > 0)
                    {
                        foreach (XElement currentNode in checkSortOption)
                        {
                            string apiProgram = currentNode.Elements("APIProgram")?.First()?.Value;
                            string transaction = currentNode.Elements("Transaction")?.First()?.Value;
                            IEnumerable<XElement> fields = currentNode.Elements("Fields");

                            SortingOrderDefinition sortingOrder = new SortingOrderDefinition();

                            if (null != fields && fields.Count() > 0)
                            {
                                foreach (XElement currentTable in fields.Elements())
                                {
                                    string fieldName = currentTable.Name.ToString();

                                    if (fieldName.Length > 5)
                                    {
                                        fieldName = fieldName.Substring(2);

                                        if (fieldName == "FILE")
                                        {
                                            sortingOrder.TableName = currentTable.Value;
                                        }
                                        else if (fieldName == "SOPT")
                                        {
                                            sortingOrder.SortingOrder = currentTable.Value;
                                        }
                                        else if (Regex.IsMatch(fieldName, "((..KEY[0-9])|(..KE[0-9]{2}))"))
                                        {
                                            sortingOrder.Fields.Add(currentTable.Value);
                                        }
                                    }
                                }
                            }

                            result = sortingOrder;

                            Console.WriteLine(apiProgram + "," + sortingOrder.TableName + "," + sortingOrder.SortingOrder + "," + string.Join(",", sortingOrder.Fields));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            return (result);
        }

        private List<SortingOrderDefinition> readFiles(List<string> files)
        {
            List<SortingOrderDefinition> result = new List<SortingOrderDefinition>();
            if (null != files && files.Count > 0)
            {
                foreach (string file in files)
                {
                    if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        using (ZipArchive zip = ZipFile.OpenRead(file))
                        {
                            foreach (ZipArchiveEntry currentFile in zip.Entries)
                            {
                                if (currentFile.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                                {
                                    string extractPath = Path.GetTempFileName();
                                    currentFile.ExtractToFile(extractPath, true);

                                    if (File.Exists(extractPath))
                                    {
                                        SortingOrderDefinition sortingOrder = handleCheckSortingOptions(extractPath);

                                        if(null != sortingOrder)
                                        {
                                            result.Add(sortingOrder);
                                        }
                                    }
                                    File.Delete(extractPath);
                                }
                            }
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                result = null;
            }

            return (result);
        }

        private void checkM3SortingOrders(SortingOrderDefinition aSortingOrderDef, bool aCreate = false)
        {
            try
            {
                if (null != apiService)
                {
                    Console.Write("Check: " + aSortingOrderDef.TableName + aSortingOrderDef.SortingOrder + " ... ");
                    string result = apiService.CRS021MI_GetSrtOpt(aSortingOrderDef.TableName, aSortingOrderDef.SortingOrder);

                    miresults res = JsonConvert.DeserializeObject<miresults>(result);

                    if (null != res && null != res.results && res.results.Count > 0 && null != res.results[0].records && res.results[0].records.Length > 0)
                    {
                        SortingOrderDefinition m3SortingOrderInfo = new SortingOrderDefinition(res.results[0], aSortingOrderDef.TableName, aSortingOrderDef.SortingOrder);

                        string[] m3Values = m3SortingOrderInfo.Fields.ToArray();
                        string[] configValues = aSortingOrderDef.Fields.ToArray();

                        var variances = m3Values.Union(configValues).Except(m3Values.Intersect(configValues));   //m3Values.Except(configValues);

                        if (null != variances && Enumerable.Count(variances) > 0)
                        {
                            Console.WriteLine("has variations");
                            foreach (string key in variances)
                            {
                                Console.WriteLine("  " + key);
                            }
                        }
                        else
                        {
                            Console.WriteLine("matches");

                        }
                    }
                    else
                    {
                        if(null != res && null != res.results && res.results.Count > 0)
                        {
                            if(res.results[0].errorCode == "WOI0203")
                            {
                                Console.WriteLine("doesn't exist");
                                // sorting order doesn't exist, perhaps we should create it
                                if(aCreate)
                                {
                                    Console.WriteLine(createM3SortingOrder(aSortingOrderDef));
                                }
                                else
                                {
                                    Console.Write("");
                                }
                                
                            }
                            else
                            {
                                Console.WriteLine(res.results[0].errorMessage);
                            }
                        }
                        else
                        {
                            string additionalMessage = "";
                            if(aCreate)
                            {
                                additionalMessage = ", not creating";
                            }
                            Console.WriteLine(" no result from M3" + additionalMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private string createM3SortingOrder(SortingOrderDefinition aSortingOrderDef)
        {
            string result = null;

            if(null != aSortingOrderDef && null != apiService)
            {
                string addResult = apiService.CRS021MI_AddSrtOpt(aSortingOrderDef.TableName, aSortingOrderDef.SortingOrder, aSortingOrderDef.Fields.ToArray());
                if (false == string.IsNullOrEmpty(addResult))
                {
                    try
                    {
                        miresults res = JsonConvert.DeserializeObject<miresults>(addResult);
                        if(null != res && null != res.results && res.results.Count > 0)
                        {
                            if(!string.IsNullOrEmpty(res.results[0].errorCode) && !string.IsNullOrEmpty(res.results[0].errorMessage))
                            {
                                result = res.results[0].errorCode + " " + res.results[0].errorMessage;
                            }
                            else
                            {
                                result = " created, must be manually activated";
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        result = ex.Message;
                    }
                }
            }

            return (result);
        }
    }

}
