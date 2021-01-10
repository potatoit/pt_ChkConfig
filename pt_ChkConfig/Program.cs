using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
  /pause - if this argument is specified, it will pause the output before exiting the program
";
        GetData apiService = null;
        static void Main(string[] args)
        {
            // file <path>
            // ionapi <path>
            // /checksortorder
            // /pause
            if(args.Length > 0)
            {
                List<string> files = new List<string>();
                string ionapiFile = "";
                bool checkSortingOrder = false;
                bool createSortingOrder = false;
                bool pause = false;
                for(int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];
                    if (false == string.IsNullOrEmpty(argument))
                    {
                        if(argument.Trim() == "/file" && args.Length > (i + 1))
                        {
                            if(File.Exists(args[i + 1]))
                            {
                                files.Add(args[i + 1]);
                            }
                            else if(Directory.Exists(args[i + 1]))
                            {
                                string[] directories = Directory.GetFiles(args[i + 1]);
                                if(null != directories && directories.Length > 0)
                                {
                                    files.AddRange(directories);
                                }
                            }
                            i++;
                        }
                        else if (argument.Trim() == "/ionapi" && args.Length > (i + 1))
                        {
                            if(File.Exists(args[i + 1]))
                            {
                                ionapiFile = args[i + 1];
                            }
                            i++;
                        }
                        else if (argument.Trim() == "/checksortorder")
                        {
                            checkSortingOrder = true;
                        }
                        else if(argument.Trim() == "/createsortingorder")
                        {
                            createSortingOrder = true;
                        }
                        else if (argument.Trim() == "/pause")
                        {
                            pause = true;
                        }
                    }
                }
                if(files.Count > 0)
                {
                    Program program = new Program();
                    List<SortingOrderDefinition> sortingOrderDefinitions = program.readFiles(files);
                    if(sortingOrderDefinitions != null && true == checkSortingOrder && null != ionapiFile)
                    {
                        program.apiService = (GetData)(new GetData()).Load(ionapiFile);
                        if (null != program.apiService)
                        {
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

        public List<SortingOrderDefinition> readFiles(List<string> files)
        {
            List<SortingOrderDefinition> result = new List<SortingOrderDefinition>();
            if(null != files && files.Count > 0)
            {
                foreach(string file in files)
                {
                    if(file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
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
                                        try
                                        {
                                            XmlDocument config = new XmlDocument();
                                            config.Load(extractPath);

                                            XmlNodeList sortingOrders = config.SelectNodes("//Check[contains(Transaction, 'CheckSortOption')]");
                                            if (null != sortingOrders && sortingOrders.Count > 0)
                                            {
                                                foreach (XmlNode currentNode in sortingOrders)
                                                {
                                                    string apiProgram = currentNode.SelectSingleNode("APIProgram")?.InnerText;
                                                    XmlNodeList fields = currentNode.SelectNodes("Fields");

                                                    SortingOrderDefinition sortingOrder = new SortingOrderDefinition();

                                                    foreach (XmlNode currentTable in fields)
                                                    {
                                                        if (currentTable.HasChildNodes)
                                                        {
                                                            foreach (XmlNode currentField in currentTable.ChildNodes)
                                                            {
                                                                if (currentField.Name.EndsWith("FILE", StringComparison.OrdinalIgnoreCase))
                                                                {
                                                                    sortingOrder.TableName = currentField.InnerText;
                                                                }
                                                                else if (currentField.Name.EndsWith("SOPT", StringComparison.OrdinalIgnoreCase))
                                                                {
                                                                    sortingOrder.SortingOrder = currentField.InnerText;
                                                                }
                                                                else if (currentField.Name.ToUpper().Contains("KEY"))
                                                                {
                                                                    sortingOrder.Fields.Add(currentField.InnerText);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    result.Add(sortingOrder);

                                                    Console.WriteLine(apiProgram + "," + sortingOrder.TableName + "," + sortingOrder.SortingOrder + "," + string.Join(",", sortingOrder.Fields));
                                                }

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex);
                                        }

                                    }
                                    File.Delete(extractPath);
                                }
                            }
                        }
                    }
                    
                }
            }

            if(result.Count == 0)
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
                                Console.Write("doesn't exist");
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
