using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Infiniminer
{
    /* Loads in a datafile consisting of key/value pairs, in the format of
     * "key = value", which can be read out through the Data dictionary.
     */

    public class DatafileWriter
    {
        public string fullContent="";
        public Dictionary<string, string> Data = new Dictionary<string, string>();
        /*public Dictionary<string, string> Data
        {
            get { return dataDict; }
        }*/

        public DatafileWriter(string filename)
        {
            Data = new Dictionary<string, string>();
            try
            {
                FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);

                fullContent = "";
                string line = sr.ReadLine();
                while (line != null)
                {
                    fullContent += line;
                    string[] args = line.Split("=".ToCharArray());
                    if (args.Length == 2 && line[0] != '#')
                    {
                        Data[args[0].Trim()] = args[1].Trim();
                    }
                    line = sr.ReadLine();
                }

                sr.Close();
                file.Close();
            }
            catch (Exception e)
            {
                Console.OpenStandardError();
                Console.Out.WriteLine(e.ToString());
                Console.Out.Close();
            }
        }

        public int WriteChanges(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    FileStream temp = File.Create(filename);
                    temp.Close();
                }
                Dictionary<string, bool>seen = new Dictionary<string, bool>();

                FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);

                string contentToWrite = "";
                fullContent = "";
                string line = sr.ReadLine();
                bool changes = false;
                while (line != null)
                {
                    string[] args = line.Split("=".ToCharArray(),2);
                    if (args.Length == 2 && line[0] != '#')
                    {
                        seen[args[0].Trim()] = true;
                        if (Data.ContainsKey(args[0].Trim()) && Data[args[0].Trim()] != args[1]) //Maybe we need to replace?
                        {
                            contentToWrite += args[0].Trim() + " = " + Data[args[0].Trim()] + "\n";
                            changes = true;
                        }
                        else
                        {
                            Data[args[0].Trim()] = args[1].Trim();
                            contentToWrite += line + "\n";
                        }
                    }
                    else
                        contentToWrite += line + "\n";
                    line = sr.ReadLine();
                }

                sr.Close();
                file.Close();

                //Check for values we never saw in the settings file
                foreach (KeyValuePair<string, string> dataI in Data)
                {
                    if (!seen.ContainsKey(dataI.Key))
                    {
                        contentToWrite += dataI.Key + " = " + dataI.Value + "\n";
                        changes = true;
                    }
                }

                if (changes)
                {
                    file = new FileStream(filename, FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(file);
                    sw.Write(contentToWrite);
                    sw.Close();
                    file.Close();
                    return 2;
                }
                return 1;
            }
            catch (Exception e){
                Console.OpenStandardError();
                Console.Out.WriteLine(e.ToString());
                Console.Out.Close();
            }
            return 0;
        }

    }
}
