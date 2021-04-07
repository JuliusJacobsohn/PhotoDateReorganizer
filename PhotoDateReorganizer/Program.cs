
using CommandLine;
using ExifLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDateReorganizer
{
    class Program
    {
        class Options
        {
            [Option('i', "input", Required = true, HelpText = "Folder to organize", Default = @"F:\Unsorted")]
            public string InputFolder { get; set; }
            [Option('o', "output", Required = true, HelpText = "Folder to write the organized files into", Default = @"F:\Sorted")]
            public string OutputFolder { get; set; }

            [Option('c', "cut", HelpText = "Cut files instead of copying", Default = true)]
            public bool Cut { get; set; }

            static void Main(string[] args)
            {
                bool cut = false;
                string input = @"F:\Unsorted";
                string output = @"F:\Sorted";
                Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o =>
                   {
                       cut = o.Cut;
                       input = o.InputFolder;
                       output = o.OutputFolder;
                   });

                var allFiles = Directory.GetFiles(input, "*.*", SearchOption.AllDirectories);
                Console.WriteLine($"Found {allFiles.Count()} files");
                StringBuilder sb = new StringBuilder();
                string[] forbiddenExtensions = new string[] { ".mp4", ".MP4", ".mov", ".MOV", ".png", ".PNG", ".heic", ".HEIC" };
                foreach (var file in allFiles)
                {
                    DateTime maxDate = new DateTime(2100, 1, 1);
                    DateTime writtenDate = maxDate;
                    try
                    {

                        DateTime writeTime = File.GetLastWriteTime(file);
                        writtenDate = Min(writtenDate, writeTime);

                        DateTime accessTime = File.GetLastAccessTime(file);
                        writtenDate = Min(writtenDate, accessTime);

                        DateTime creationTime = File.GetCreationTime(file);
                        writtenDate = Min(writtenDate, creationTime);

                        if (!forbiddenExtensions.Contains(Path.GetExtension(file)))
                        {
                            //Find minimum date in metadata etc.
                            var imageFile = ImageFile.FromFile(file);
                            DateTime dateTime = (DateTime)(imageFile.Properties.Get(ExifTag.DateTime)?.Value ?? maxDate);
                            writtenDate = Min(writtenDate, dateTime);

                            DateTime dateTimeDigitized = (DateTime)(imageFile.Properties.Get(ExifTag.DateTimeDigitized)?.Value ?? maxDate);
                            writtenDate = Min(writtenDate, dateTimeDigitized);

                            DateTime dateTimeOriginal = (DateTime)(imageFile.Properties.Get(ExifTag.DateTimeOriginal)?.Value ?? maxDate);
                            writtenDate = Min(writtenDate, dateTimeOriginal);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(PrintLog($"Error for {file}: {e}"));
                    }

                    try
                    {


                        string targetYear = Path.Combine(output, writtenDate.Year.ToString());
                        if (!Directory.Exists(targetYear))
                        {
                            sb.AppendLine(PrintLog($"Creating directory {targetYear}"));
                            Directory.CreateDirectory(targetYear);
                        }
                        string targetMonth = Path.Combine(targetYear, writtenDate.Month.ToString());
                        if (!Directory.Exists(targetMonth))
                        {
                            sb.AppendLine(PrintLog($"Creating directory {targetMonth}"));
                            Directory.CreateDirectory(targetMonth);
                        }
                        string fileNameOnly = Path.GetFileNameWithoutExtension(file);
                        string fileExtensionOnly = Path.GetExtension(file);
                        string targetPath = Path.Combine(targetMonth, fileNameOnly + fileExtensionOnly);
                        if (File.Exists(targetPath))
                        {
                            targetPath = Path.Combine(targetMonth, fileNameOnly + "-" + Guid.NewGuid().ToString().Substring(0, 4) + fileExtensionOnly);
                        }

                        sb.AppendLine(PrintLog($"Moving file {file} to {targetMonth}"));
                        File.Move(file, targetPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(PrintLog($"Error for {file}: {e}"));
                    }
                }
                File.AppendAllText("PhotoDateReorganizerLog.txt", sb.ToString());
            }

            public static string PrintLog(string log)
            {
                log = $"{DateTime.Now.ToString()}: {log}";
                Console.WriteLine(log);
                return log;
            }

            public static DateTime Min(DateTime d1, DateTime d2)
            {
                return d1 < d2 ? d1 : d2;
            }
        }
    }

}