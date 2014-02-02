using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RenameByRegex
{
    class Program
    {
        static void Main(string[] args)
        {
            //arg 0 is the file to open. Arg 1 is the directory it is to be run against. 
            // /t = testmode
            
            if (args.Length < 2)
            {
                 Console.WriteLine("Usage: RenameByRegex.exe <Regex filename> <directory> [/t]");
                 Console.WriteLine("/t (optional): test mode - do not actually rename anything ");
                 Console.WriteLine("File should contain a line seperated list of regexs, in three line pairs. Blank lines allowed between groups. First line should be a name for the regex, second line should be regex to extract into match groups, third line is how those groups are written out. Match groups referenced with a $ sign. E.g:");
                 Console.WriteLine("BLECH");
                 Console.WriteLine("(.*'stuff')");
                 Console.WriteLine("BLECH-$stuff");


            }

            //open file containing regex pairs. 
            string filename = args[0];
            string dirname = args[1];
            bool testmode = false;
            //todo - change if more args
            if (args.Length > 2 && args[2] == "\\t")
            {
                testmode = true;
            }

            List<regexPair> regexes = new List<regexPair>();
            try
            {
                StreamReader sr = new StreamReader(filename);
                string mode = "N"; //N = new regex, R = Regexmatch, O = output;

                string regName = "";
                string regMatch = "";
                string regOut = "";

                

                //parse the lines into a set of regex objects
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line != "")
                    {
                        if (mode == "N")
                        {
                            regName = line;
                            mode = "R";
                        }
                        else if (mode == "R")
                        {
                            regMatch = line;
                            mode = "O";
                        }
                        else if (mode == "O")
                        {
                            regOut = line;
                            mode = "N";

                            regexes.Add(new regexPair(regName, regMatch, regOut));

                            regName = "";
                            regMatch = "";
                            regOut = "";
                        }


                    }
                }
                sr.Close();

            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File Not Found");
            }
            //todo more exceptions


            //try open the directory
            DirectoryInfo di = new DirectoryInfo(dirname);
            if (di.Exists)
            {
                //loop through all the files in the directory, and all the regexes. Only rename each file once. 
                foreach (FileInfo fi in di.GetFiles())
                {
                    bool renamed = false;
                    foreach (regexPair reg in regexes)
                    {
                        if (!renamed)
                        {
                            Match m = reg.regex.Match(fi.Name);
                            if (m.Success)
                            {
                                string newVal = "";
                                string newFileName = reg.output;
                                foreach (string s in reg.replacements)
                                {
                                    string groupName = s.Substring(1,s.Length - 2);
                                    if (m.Groups[groupName] != null)
                                    {
                                        newVal = m.Groups[groupName].Value;
                                        //Console.WriteLine("Found " + groupName + " which is " + newVal);
                                    }
                                    newFileName = newFileName.Replace(s, newVal);
                                    //Console.WriteLine("Replacing " + s + " with " + newVal);
                                    
                                }

                                renamed = true;
                                Console.WriteLine("Renaming " + fi.Name + " to " + newFileName);
                                if (!testmode)
                                {
                                    fi.MoveTo(fi.DirectoryName + "\\" + newFileName);
                                }

                            }
                        }
                    }

                }


            }
            else
            {
                 Console.WriteLine("Directory Not Found");
            }

            Console.WriteLine("Finished. Press Return");
            Console.Read();

        }

        private class regexPair
        {
            public string name;
            public string regexMatch;
            public string output;
            public Regex regex;
            public List<String> replacements = new List<string>(); //list of strings in the output that need to be replaced.

            public regexPair(string name, string regexMatch, string output)
            {
                this.name = name;
                this.regexMatch = regexMatch;
                this.output = output;
                regex = new Regex(regexMatch);
                Regex outputReg = new Regex("\\$(\\w*)\\$");

                int startpos = 0;
                bool noMatches = false;
                while (!noMatches)
                {
                    Match m = outputReg.Match(output,startpos);
                    if (m.Success && m.Captures.Count > 0)
                    {
                        foreach (Capture c in m.Captures)
                        {
                            replacements.Add(c.Value);
                            startpos = c.Index + c.Length;
                        }
                    }
                    else
                    {
                        noMatches = true;
                    }
                }
                Console.WriteLine("Added group " + name + "with output " + output + " containing " + replacements.Count.ToString() + " replacements");
            }



        }
    }
}
