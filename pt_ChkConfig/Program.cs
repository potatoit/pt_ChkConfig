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
        static string usage = "";
        static void Main(string[] args)
        {
            // file <path>
            // ionapi <path>
            // /checksortorder
            if(args.Length > 1)
            {
                List<string> files = new List<string>();
                string ionapiFile = "";
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
                    }
                }
                if(files.Count > 0)
                {
                    (new Program()).readFiles(files);
                }
            }
            else
            {
                Console.Write(usage);
                Console.WriteLine("");
            }
        }

        public void readFiles(List<string> files)
        {
            if(null != files && files.Count > 0)
            {
                foreach(string file in files)
                {
                    using(ZipArchive zip = ZipFile.OpenRead(file))
                    {
                        foreach(ZipArchiveEntry currentFile in zip.Entries)
                        {
                            if (currentFile.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                string extractPath = Path.GetTempFileName();
                                currentFile.ExtractToFile(extractPath, true);

                                if(File.Exists(extractPath))
                                {
                                    XmlDocument config = new XmlDocument();
                                    config.Load(extractPath);

                                    XmlNodeList sortingOrders = config.SelectNodes("//Check[contains(Transaction, 'CheckSortOption')]");
                                    if(null != sortingOrders && sortingOrders.Count > 0)
                                    {
                                        foreach(XmlNode currentNode in sortingOrders)
                                        {
                                            string apiProgram = currentNode.SelectSingleNode("APIProgram")?.InnerText;
                                            XmlNodeList fields = currentNode.SelectNodes("Fields");

                                            SortingOrderDefinition sortingOrder = new SortingOrderDefinition();

                                            foreach(XmlNode currentTable in fields)
                                            {
                                                if(currentTable.HasChildNodes)
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

                                            Console.WriteLine(apiProgram + "," + sortingOrder.TableName + "," + sortingOrder.SortingOrder + "," + string.Join(",", sortingOrder.Fields));
                                        }
                                        
                                    }
                                }
                                File.Delete(extractPath);
                            }
                        }
                    }
                }
            }
        }
    }
}
