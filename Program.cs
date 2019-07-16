
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Processing2IntelliJ
{
    class Program
    {
        static string root = @"D:\Users\Koeqatel\ProcessingProjects\project-ready-set-go\Dirty_Paws\";
        static string fileContents = "";
        static string contentToPush = "";

        static void Main(string[] args)
        {
            start();

        }

        static void start()
        {
            Console.WriteLine("Would you like to convert to IntelliJ IDEA (1) or to Processing (2)?");

            char result = Console.ReadKey().KeyChar;
            Console.Clear();

            //Get MainClass content
            string MainClass = readFile(@"src/MainClass.java");

            switch (result)
            {
                case '1':
                    //Get the names of all files in the project
                    List<string> fileNames = getFileNames();

                    foreach (string fileName in fileNames)
                    {
                        WriteLine("Found file: " + fileName);

                        //Get the contents of that file
                        string fileContent = readFile(fileName);

                        //Prepare the content string to be pushed to the server
                        addToContentString(fileName, fileContent);
                    }

                    bool ignoreLines = false;

                    //Put the contents of MainClass and the pde files together
                    using (StringReader reader = new StringReader(MainClass))
                    {
                        string line;
                        //Read the content of MainClass line by line
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Replace(" ", "").Replace("\t", "").Equals("//<content>"))
                            {
                                ignoreLines = true;
                            }
                            else if (line.Replace(" ", "").Replace("\t", "").Equals("//</content>"))
                            {
                                //And add the content of the other files
                                contentToPush += @"//<content>" + Environment.NewLine;
                                contentToPush += fileContents + Environment.NewLine;
                                contentToPush = contentToPush.Replace(@"setup() {", @"setup() {" + Environment.NewLine + "processing = this;" + Environment.NewLine);
                                contentToPush += @"//</content>" + Environment.NewLine;


                                ignoreLines = false;
                            }
                            else if (!ignoreLines)
                            {
                                //And add it to the content to push
                                contentToPush += line + Environment.NewLine;
                            }
                        }
                    }

                    //Save the content to the MainClass file
                    writeToFile("IDEA", null, null);

                    break;
                case '2':
                    Dictionary<string, string> fileList = breakupMainClass(MainClass);

                    contentToPush = MainClass;

                    foreach (var file in fileList)
                    {
                        string fileName = file.Key.Replace("<", "").Replace(">", "");
                        string fileContent = file.Value;
                        fileContent = fileContent.Replace(@"processing = this;", "");

                        writeToFile("Processing", fileName, fileContent);
                    }

                    writeToFile("IDEA", null, null);
                    break;
                default:
                    break;
            }


            fileContents = "";
            contentToPush = "";
        }

        static List<string> getFileNames()
        {
            List<string> fileNames = new List<string>();

            //Get all files in path and put the in the returned list
            string[] files = Directory.GetFiles(root);
            foreach (string file in files)
            {
                if (Path.GetFileName(file).Split('.')[1].Equals("pde"))
                    fileNames.Add(Path.GetFileName(file));
            }

            //Return all filenames
            return fileNames;
        }

        static string readFile(string fileName)
        {
            //Get the file content and return it

            string fileContent = System.IO.File.ReadAllText(root + fileName);

            return fileContent;
        }

        static void addToContentString(string fileName, string content)
        {
            fileContents += @"//<" + fileName + ">" + Environment.NewLine;
            fileContents += content + Environment.NewLine;
            fileContents += @"//</" + fileName + ">" + Environment.NewLine;
        }

        static void writeToFile(string targetIde, string file, string content)
        {
            switch (targetIde)
            {
                case "IDEA":
                    System.IO.File.WriteAllText(root + "src/MainClass.java", contentToPush);
                    CopyFilesRecursively(root + "/data", root + "/src");
                    break;
                case "Processing":
                    System.IO.File.WriteAllText(root + file, content);
                    break;
            }
        }

        static Dictionary<string, string> breakupMainClass(string MainClass)
        {
            Dictionary<string, string> fileList = new Dictionary<string, string>();

            //Set the regex
            Regex rx = new Regex(@"\/\/\<(.*?)\>(.*?)\/\/\<\/(.*?)\>", RegexOptions.Singleline);

            //Run the regex
            MatchCollection matches = rx.Matches(MainClass);

            foreach (Match match in matches)
            {
                //Split regex result up
                GroupCollection groups = match.Groups;
                string fileName = groups[1].Value;
                string fileContent = groups[2].Value;

                if (!fileName.Equals("content"))
                    fileList.Add(fileName, fileContent);
            }

            return fileList;
        }

        public static void CopyFilesRecursively(string SourcePath, string DestinationPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
        }

        public static void WriteLine(string text)
        {
            string date = string.Format("{0:HH:mm:ss.fff}", DateTime.Now);

            Console.WriteLine(date + ": " + text);
        }
    }
}
