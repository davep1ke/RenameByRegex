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
                write("Usage: RenameByRegex.exe <Regex filename> <directory> [/t] [/e] (<tag1> <replace1>)");
                write("\\t (optional): test mode - do not actually rename anything ");
                write("\\e (optional): exhaustive mode - Keep replacing until no regexes match");
                write("\\p (optional): pause at end- pause at the end of the run");
                write("\\pf (optional): pause after file - pause before renaming each file");
                write("File should contain a line seperated list of regexs, in three line pairs. Blank lines allowed between groups. First line should be a name for the regex, second line should be regex to extract into match groups, third line is how those groups are written out. Match groups referenced with a $ sign. Comment with a #, -- or // E.g:");
                write("BLECH");
                write("--This looks for something");
                write("(?<stuff>.*)");
                write("BLECH-$stuff$");
                
            }

            //open file containing regex pairs. 
            string filename = args[0];
            string dirname = args[1];

            List<KeyValuePair<string, string>> replacements = new List<KeyValuePair<string, string>>();

            bool testmode = false;
            bool pauseAtEnd = false;
            bool pauseFile = false;
            bool exhaustive = false;

            //todo - change if more args
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "\\t" || args[i] == "-t" || args[i] == "/t") { testmode = true; }
                else if (args[i] == "\\p" || args[i] == "-p" || args[i] == "/p") { pauseAtEnd = true; }
                else if (args[i] == "\\e" || args[i] == "-e" || args[i] == "/e") { exhaustive = true; }
                else if (args[i] == "\\pf" || args[i] == "-pf" || args[i] == "/pf") { pauseFile = true; }
                //check if we can add a replacement
                else if (args.Length > i + 1)
                {

                    replacements.Add(new KeyValuePair<string, string>(args[i], args[i + 1]));
                    write("Added replacement - \"" + args[i] +  "\" replaced with \"" + args[i + 1] + "\"", 1, ConsoleColor.Yellow);
                    i++;
                }
                
            }

            if (replacements.Count > 0 ) { write("There are " + replacements.Count +  " replacements", 1, ConsoleColor.Green); }

            if (testmode)
            {
                write("Working in test mode - files won't acutally be renamed.", 1, ConsoleColor.Red);
            }

            if (exhaustive)
            {
                write("Exhaustive mode - regexes will keep being applied until none match.", 1, ConsoleColor.Green);
            }

            if (pauseFile)
            {
                write("Pausing after each file.", 1, ConsoleColor.Green);
            }
            if (pauseAtEnd)
            { 
                write("Pausing at end", 1, ConsoleColor.Green);
            }
    
            List<regexPair> regexes = new List<regexPair>();
            try
            {
                write("Loading Regex file ", 0, ConsoleColor.Green, true);
                write("Loading " + filename, 1, ConsoleColor.Green);
                StreamReader sr = new StreamReader(filename);
                string mode = "N"; //N = new regex, R = Regexmatch, O = output;

                string regName = "";
                string regMatch = "";
                string regOut = "";


                //parse the lines into a set of regex objects
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line != "" && !line.StartsWith("#") && !line.StartsWith("--") && !line.StartsWith("//"))
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
                write("Finished processing file", 1, ConsoleColor.Green);

            }
            catch (DirectoryNotFoundException)
            {
                write("Directory containing regex file Not Found", 0, ConsoleColor.Red);
                return;
            }
            catch (FileNotFoundException )
            {
                write("Regex file not found!", 0, ConsoleColor.Red);
                return;
            }
            //todo more exceptions


            //Apply our replacements to each regex
            write("Replacing text in regexes", 0, ConsoleColor.Green, true);

            foreach (regexPair rp in regexes)
            {
                write(rp.name, 2, ConsoleColor.Gray);
                foreach(KeyValuePair<string, string> r in replacements)
                {
                    rp.applyReplacement(r);
                }

            }

            
            //try open the directory
            DirectoryInfo di = new DirectoryInfo(dirname);

            write("Processing Files", 0, ConsoleColor.Green, true);

            if (di.Exists)
            {
                //loop through all the files in the directory, and all the regexes. Only rename each file once. 
                foreach (FileInfo fi in di.GetFiles())
                {
                    
                    bool exitLoop = false;
                    int regexesApplied = 0;

                    string currentFileName = fi.Name;
                    write("Processing file " + fi.Name, 2, ConsoleColor.Green);

                    /*go through until we get a hit, then stop processing. Retry from the top each time. Exit the loop if :
                            we only want to hit a single regex
                            we havent hit any regexes
                            we have hit like 100 and want to abort
    
                    */

                    while (!exitLoop)
                    {
                        bool renamed = false;
                        foreach (regexPair reg in regexes)
                        {
                            if (!renamed)
                            {
                                Match m = reg.regex.Match(currentFileName);
                                if (m.Success)
                                {
                                    write("Regex Matched! " + reg.name, 4, ConsoleColor.Yellow );
                                    string newVal = "";
                                    currentFileName = reg.output;
                                    foreach (string s in reg.replacements)
                                    {
                                        string groupName = s.Substring(1, s.Length - 2);
                                        if (m.Groups[groupName] != null)
                                        {
                                            newVal = m.Groups[groupName].Value;
                                            //Console.WriteLine("::::::Found " + groupName + " which is " + newVal);
                                        }
                                        currentFileName = currentFileName.Replace(s, newVal);
                                        //Console.WriteLine("::::::Replacing " + s + " with " + newVal);

                                    }
                                    write("Regex " + reg.name + " applied - name now ", 5, ConsoleColor.DarkYellow);
                                    write(currentFileName,5, ConsoleColor.DarkYellow);
                                    renamed = true;
                                    regexesApplied++;
                                }
                            }
                        }

                        //do we want to have another pass at this? 
                        if (renamed && exhaustive && regexesApplied < 100)
                        {
                            exitLoop = false;
                        }
                        else
                        {
                            exitLoop = true;
                        }
                        
                    }

                    //do we want to actually rename the file? 
                    if (fi.Name != currentFileName)
                    {
                        write("Renaming from / to", 3, ConsoleColor.White);
                        write(fi.Name, 4, ConsoleColor.Gray);
                        write(currentFileName, 4, ConsoleColor.Gray);
                        if (pauseFile)
                        {
                            if (pauseFile) { Console.ReadKey(); }
                        }
                        if (!testmode)
                        {
                            fi.MoveTo(fi.DirectoryName + "\\" + currentFileName);
                        }
                        else
                        {
                            write("Rename Skipped - test mode", 2, ConsoleColor.Gray);
                        }



                    }
                }


            }
            else
            {
                write("Directory Not Found",1, ConsoleColor.Red);
            }

            write("Finished. Press Return",1,ConsoleColor.Green);
            if (pauseAtEnd) { Console.ReadKey(); }

        }


        public static void write(string text, int level = 1, ConsoleColor colour = ConsoleColor.White, bool banner = false)
        {
            Console.ForegroundColor = colour;
            text = new string(' ', level) + text;
            if (banner) { Console.WriteLine(new string('=', text.Length)); }
            Console.WriteLine(text);
            if (banner) { Console.WriteLine(new string('=', text.Length)); }
            if (colour == ConsoleColor.Red) { Console.ReadKey();  }
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
                write("Added group " + name + " with output " + output + " containing " + replacements.Count.ToString() + " replacements", 2, ConsoleColor.Green);
            }

            /// <summary>
            /// Apply a replacement to the regex
            /// </summary>
            /// <param name="replacement"></param>
            public void applyReplacement(KeyValuePair<string, string> replacement)
            {
                string newRegex = regexMatch.Replace(replacement.Key, replacement.Value);
                if (newRegex != regexMatch)
                {
                    write("Updated regex " + name + " regex from / to ", 4, ConsoleColor.Yellow);
                    write(regexMatch, 5, ConsoleColor.Gray);
                    write(newRegex , 5, ConsoleColor.Gray);
                    regexMatch = newRegex;
                    regex = new Regex(regexMatch);
                }

                string newOutput = output.Replace(replacement.Key, replacement.Value);
                if (newOutput != output)
                {
                    write("Updated regex " + name + " ouptut from / to ", 4, ConsoleColor.Yellow);
                    write(output, 5, ConsoleColor.Gray);
                    write(newOutput, 5, ConsoleColor.Gray);
                    output = newOutput;
                }

            }

        }
                    
    }
}
