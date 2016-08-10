using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FluentPlaneAverager
{
    class Program
    {

        public static string getPath(string file)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
        }

        public static string getDir()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static int numberFromString(string str)
        {
            MatchCollection mc = Regex.Matches(str, @"\d+");

            string str2 = "";

            for (int i = 0; i < mc.Count; i++)
            {
                str2 += (mc[i].Value);
            }

            int number = 0;
            bool arg1 = int.TryParse(str2, out number);

            return number;
        }

        static void Main(string[] args)
        {
            localprocess();
            
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        
        static void localprocess()
        {
            Console.WriteLine("Type exact common name of files to loop:");
            string dataFileName = Console.ReadLine();

            Console.WriteLine("Type number of data columns to exclude:");
            string skipString = Console.ReadLine();
            int skipCols = numberFromString(skipString);

            Console.WriteLine("Specify data delimiter:");
            string delimiter = Console.ReadLine();

            string[] headerFields = new string[2];
            int dataRows = 0;
            int dataCols = 0;
            int[] nodeColumn = new int[dataRows];
            double[][] staticDataColumns = new double[dataCols][];
            double[][] meanDataColumns = new double[dataCols][];
            decimal[][] squaredDataColumns = new decimal[dataCols][];

            // Identify number of files to process and initiate variables
            string[] filePaths = Directory.GetFiles(getDir());
            Console.WriteLine("Directory consists of " + filePaths.Length + " files.");
            int filesCount = 0;
            foreach (string myfile in filePaths)
            {
                if (myfile.Contains(dataFileName))
                {
                    if (filesCount == 0)
                    {
                        Console.WriteLine("Initiating from file: \n" + myfile);
                        var lines = File.ReadAllLines(myfile);

                        // prepare header & initiate fields
                        string temp = lines[0].TrimStart(' ').TrimEnd(' ');
                        temp = Regex.Replace(temp, @"\s+", " "); // Clean out spaces and prepare delimiter
                        headerFields = Regex.Split(temp,delimiter);

                        dataRows = lines.Length - 1;
                        dataCols = headerFields.Length - 1 - skipCols;

                        nodeColumn = new int[dataRows];
                        staticDataColumns = new double[skipCols][];
                        meanDataColumns = new double[dataCols][];
                        squaredDataColumns = new decimal[dataCols][];

                        for (int c = 0; c < skipCols; c++)
                        {
                            staticDataColumns[c] = new double[dataRows];
                        }
                        for (int c = 0; c < dataCols; c++)
                        {
                            meanDataColumns[c] = new double[dataRows];
                            squaredDataColumns[c] = new decimal[dataRows];
                        }

                        // fill the unprocessed columns (node + skip)
                        for (int r = 0; r < dataRows; r++)
                        {
                            string temp1 = lines[r + 1].TrimStart(' ').TrimEnd(' ');
                            temp1 = Regex.Replace(temp1, @"\s+", " "); // Clean out spaces and prepare delimiter
                            string[] fields = Regex.Split(temp1, delimiter);

                            nodeColumn[r] = int.Parse(fields[0]);
                            for (int c = 0; c < skipCols; c++)
                            {
                                staticDataColumns[c][r] = double.Parse(fields[c + 1], NumberStyles.Float, CultureInfo.InvariantCulture);
                            }
                        }
                    }
                    filesCount++;
                }
            }
            Console.WriteLine("Initiated & identified " + filesCount + " data files to process.");

            Console.WriteLine("Working...");
            foreach (string myfile in filePaths)
            {
                if (myfile.Contains(dataFileName))
                {
                    string[] linez = File.ReadAllLines(myfile);
                    Console.WriteLine("Processing file: " + myfile.Replace(getDir(),""));

                    // fill the data fields
                    for (int r = 0; r < dataRows; r++)
                    {
                        string temp2 = linez[r + 1].TrimStart(' ').TrimEnd(' ');
                        temp2 = Regex.Replace(temp2, @"\s+", " "); // Clean out spaces and prepare delimiter
                        string[] fields = Regex.Split(temp2, delimiter);

                        for (int c = 0; c < dataCols; c++)
                        {
                            meanDataColumns[c][r] += double.Parse(fields[c + 1 + skipCols], NumberStyles.Float, CultureInfo.InvariantCulture) / filesCount;
                            squaredDataColumns[c][r] += (decimal)(Math.Pow(double.Parse(fields[c + 1 + skipCols], NumberStyles.Float, CultureInfo.InvariantCulture), 2)/filesCount);
                        }
                    }
                }
            }

            // process afterwards
            double[][] stdevDataColumns = new double[dataCols][];
            for (int j = 0; j < dataCols; j++)
            {
                stdevDataColumns[j] = new double[dataRows];
            }
            
            for (int r = 0; r < dataRows; r++)
            {
                for (int c = 0; c < dataCols; c++)
                {
                    stdevDataColumns[c][r] = Math.Sqrt((double)(squaredDataColumns[c][r] - (decimal)Math.Pow(meanDataColumns[c][r],2)));
                }
            }

            // output files
            writeDataFile(dataFileName, "mean", headerFields, dataRows, skipCols, dataCols, nodeColumn, staticDataColumns, meanDataColumns);
            writeDataFile(dataFileName, "stdev", headerFields, dataRows, skipCols, dataCols, nodeColumn, staticDataColumns, stdevDataColumns);

            Console.WriteLine("Results files were generated!");
        }

        private static void writeDataFile(string dataFileName, string prop, string[] headerFields, int dataRows, int skipCols, int dataCols, int[] nodeColumn, double[][] staticDataColumns, double[][] dataColumns)
        {
            // output results onto file
            StreamWriter file = new StreamWriter(Program.getPath("results " + prop + " " + dataFileName + ".txt"), false);
            // put header line
            string header = "";
            for (int j = 0; j < headerFields.Length; j++)
            {
                header += headerFields[j];
                if (j != headerFields.Length - 1)
                    header += ", ";
            }
            file.WriteLine(header);
            // put data
            for (int i = 0; i < dataRows; i++)
            {
                string dataLine = nodeColumn[i].ToString();

                for (int k = 0; k < skipCols; k++)
                {
                    dataLine += ", " + staticDataColumns[k][i].ToString().Replace(",", ".");
                }
                for (int k = 0; k < dataCols; k++)
                {
                    dataLine += ", " + dataColumns[k][i].ToString().Replace(",", ".");
                }
                
                file.WriteLine(dataLine);
            }
            file.Flush();
            file.Close();
        }
    }
}
