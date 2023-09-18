using System;
using System.IO;
using System.Diagnostics;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("请选择要执行的功能：\nPlease select the function to execute:\n");
                Console.WriteLine("  \u001b[31mA. 完整流程执行\n     Executing the entire procedure\u001b[0m\n");
                Console.WriteLine("  1. 复制文件到新文件夹\n     Copy the files to a new folder\n");
                Console.WriteLine("  2. MOL2/PDB/PDBQ转化为PDBQT\n     Convert MOL2/PDB/PDBQ to PDBQT\n");
                Console.WriteLine("  3. 执行批量对接\n     Batch docking\n");
                Console.WriteLine("  4. 将文件夹名改为对接评分结果\n     Change the folder name to docking scoring results\n");
                Console.Write("请输入字符并按Enter键：\nPlease enter character and press Enter:\n");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        CopyFilesToNewFolder();
                        break;
                    case "2":
                        MOL2toPDBQT();
                        break;
                    case "3":
                        batch_docking();
                        break;
                    case "4":
                        rename();
                        break;
                    case "A":
                        CopyFilesToNewFolder();
                        MOL2toPDBQT();
                        batch_docking();
                        rename();
                        Console.WriteLine("\n按下任意键继续\nPress any key to continue...\n");
                        Console.ReadKey();
                        break;
                    default:
                        Console.WriteLine("输入无效，请重新输入\nInvalid input. Please enter a valid input\n");
                        break;
                }
            }
        }
        // 复制文件到新文件夹
        static void CopyFilesToNewFolder()
        {
            Console.WriteLine("\n\u001b[32m复制文件到新文件夹\nCopy the file(s) to a new folder\u001b[0m\n");

            Console.Write("\n请选择输入目录：(配体分子所在的目录)\nPlease select input directory: (directory where ligand molecules are located)\n");
            string inputPath = GetDirectoryPath();
            Console.Write("\n请选择输出目录：（Docking目录）\nPlease select output directory: (directory for docking)\n");
            string outputPath = GetDirectoryPath();

            int count = 1;
            foreach (string filePath in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
            {
                string newFolderName = count.ToString();
                string newFolderPath = Path.Combine(outputPath, newFolderName);
                Directory.CreateDirectory(newFolderPath);

                string newFilePath = Path.Combine(newFolderPath, Path.GetFileName(filePath));
                File.Copy(filePath, newFilePath);

                count++;
            }

            Console.WriteLine("\n已完成所有复制任务\nAll copying tasks have been completed\n");
        }
        static string GetDirectoryPath()
        {
            while (true)
            {
                string path = Console.ReadLine().Trim();
                if (Directory.Exists(path))
                {
                    return path;
                }
                else
                {
                    Console.WriteLine("\n目录不存在，请重新输入：\nDirectory does not exist, please enter again:\n");
                }
            }
        }
        // mol2转化为pdbqt
        static void MOL2toPDBQT()
        {
            Console.WriteLine("\n\u001b[32mMOL2/PDB/PDBQ转化为pdbqt\nConvert MOL2/PDB/PDBQ to PDBQT\n\u001b[0m\n");

            Console.WriteLine("\n输入配体所在的文件路径（Docking目录）：\nPlease enter the file path of the ligand (Docking directory):");
            string mol2Dir = Console.ReadLine().Trim();

            Console.WriteLine("\n输入prepare_ligand4.py\u001b[31m所在的文件夹\u001b[0m的路径(不能有引号)\n通常是C:\\Program Files (x86)\\MGLTools-1.5.7\\Lib\\site-packages\\AutoDockTools\\Utilities24\n\nPlease enter the directory containing the prepare_ligand4.py script(Cannot have \"\"):\nPlease enter the file path of the ligand located in the docking directory. Usually it is \"C:\\Program Files (x86)\\MGLTools-1.5.7\\Lib\\site-packages\\AutoDockTools\\Utilities24\"\n");
            string prepareScriptDir = Console.ReadLine().Trim();

            Console.WriteLine("\n将配体文件转化为pdbqt文件...\nConverting ligand files to PDBQT format...");

            int totalFileCount = 0;
            List<string> mol2Paths = CollectMOL2FilePaths(mol2Dir, ref totalFileCount);

            int finishedFileCount = 0;
            foreach (string mol2Path in mol2Paths)
            {
                string pdbqtPath = Path.ChangeExtension(mol2Path, ".pdbqt");
                string mol2directoryPath = Path.GetDirectoryName(mol2Path);

                string command = $"python_mgl \"{prepareScriptDir}\\prepare_ligand4.py\" -l \"{mol2Path}\" -o \"{pdbqtPath}\" -v";

                Console.WriteLine(command);

                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WorkingDirectory = mol2directoryPath;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                if (File.Exists(pdbqtPath))
                {
                    finishedFileCount++;
                }

                Console.WriteLine($"\n剩余文件：\nRemaining files:\n {totalFileCount - finishedFileCount}/{totalFileCount}");
                Console.WriteLine($"\n输出：\nOutput:\n {output}");
            }

        }
        static List<string> CollectMOL2FilePaths(string directory, ref int count)
        {
            List<string> results = new List<string>();

            foreach (string file in Directory.GetFiles(directory, "*.mol2").Concat(Directory.GetFiles(directory, "*.pdb")).Concat(Directory.GetFiles(directory, "*.pdbq")))
            {
                results.Add(file);
                count++;
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                results.AddRange(CollectMOL2FilePaths(subDir, ref count));
            }

            return results;
        }

        static void batch_docking()
        {
            Console.WriteLine("\n\u001b[32m执行批量对接\nBatch docking\n\u001b[0m\n");
            // 提示用户输入配体路径，遍历路径以及其子文件夹的.pdbqt文件
            Console.Write("请输入配体所在目录的路径：（Docking目录）\nPlease enter the file path of the directory where the ligand is located (Docking directory):\n");
            string ligandPath = Console.ReadLine();
            // 获取所有.pdbqt文件路径
            string[] ligandFiles = Directory.GetFiles(ligandPath, "*.pdbqt", SearchOption.AllDirectories);

            // 将字符串数组转换为FileInfo数组，并按Length属性进行排序
            FileInfo[] sortedFiles = ligandFiles.Select(f => new FileInfo(f))
                                                .OrderBy(fi => fi.Length)
                                                .ToArray();

            // 将排序后的文件路径存储回字符串数组中
            ligandFiles = sortedFiles.Select(fi => fi.FullName).ToArray();

            // 提示用户输入受体文件，受体文件只有一个
            Console.Write("\n请输入受体文件的路径：\nPlease enter the path of the receptor file:\n");
            string receptorPath = Console.ReadLine();

            // 提示用户输入配置文件
            Console.Write("\n请输入配置文件的路径：\nPlease enter the path of the configuration file:\n");
            string configPath = Console.ReadLine();

            // 创建一个计时器对象
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); // 开始计时器
            Console.WriteLine("程序已经开始运行\nThe program has started running\n");

            // 生成命令并逐个运行
            int numProcessed = 0;
            foreach (string ligandFile in ligandFiles)
            {
                //显示程序运行时间
                TimeSpan runTime = stopwatch.Elapsed;
                Console.WriteLine($"程序运行的时间(The running time of the program):\u001b[31m{runTime.ToString(@"hh\:mm\:ss")}\u001b[0m");

                //显示配体文件的大小
                FileInfo fileInfo = new FileInfo(ligandFile); // 创建一个 FileInfo 对象
                double fileSizeKB = fileInfo.Length / 1024.0; // 将字节数转换为 KB
                Console.WriteLine($"{ligandFile}-\u001b[31m{fileSizeKB:F2} KB\u001b[0m"); // 显示文件大小，保留两位小数

                //配体目录
                string directoryPath1 = Path.GetDirectoryName(ligandFile);

                // 生成命令
                string command = $"vina --receptor \"{receptorPath}\" --ligand \"{ligandFile}\" --config \"{configPath}\" --out \"{directoryPath1}\\output.pdbqt\" > \"{directoryPath1}\\result.txt\"";

                // 显示命令
                Console.WriteLine($"正在处理的配体文件(LigandFile)：\u001b[31m{ligandFile}\u001b[0m");
                Console.WriteLine($"命令(Command)：\n{command}");

                // 运行命令
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C {command}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                // 显示进度
                numProcessed++;
                Console.WriteLine($"已处理(progress):\u001b[31m{numProcessed}/{ligandFiles.Length}\u001b[0m");

                // 等待进程结束
                process.WaitForExit();
                Console.WriteLine("\n\n");
            }
        }

        static void rename()
        {
            Console.WriteLine("\u001b[32m将文件夹名改为对接评分结果\nChange the folder name to docking scoring results\u001b[0m\n");
            Console.WriteLine("\n请输入工作路径：\nPlease enter the working directory:\n");
            string workingDirectory = Console.ReadLine();

            // 遍历所有子文件夹
            foreach (var folder in Directory.GetDirectories(workingDirectory, "*", SearchOption.AllDirectories))
            {
                string resultFilePath = Path.Combine(folder, "result.txt");

                if (File.Exists(resultFilePath))
                {
                    try
                    {
                        // 读取result.txt文件，并查找目标行
                        string targetLine = null;
                        using (var streamReader = new StreamReader(resultFilePath))
                        {
                            while (!streamReader.EndOfStream)
                            {
                                string line = streamReader.ReadLine();
                                if (line.Trim().StartsWith("1"))
                                {
                                    targetLine = line.Trim();
                                    break;
                                }
                            }
                        }

                        // 如果找到目标行，则提取出分数并重命名文件夹
                        if (!string.IsNullOrEmpty(targetLine))
                        {
                            string[] fields = targetLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            string scoreString = fields[1];
                            double score = double.Parse(scoreString);

                            // 使用分数重命名文件夹
                            string newFolderName = $"F{score.ToString("0.000").Replace('.', '-')}_{Path.GetFileName(folder)}";
                            string newFolderPath = Path.Combine(Path.GetDirectoryName(folder), newFolderName);

                            Console.WriteLine($"正在将 {folder} 重命名为 {newFolderPath}");
                            Console.WriteLine($"Renaming {folder} to {newFolderPath}");
                            Directory.Move(folder, newFolderPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"重命名 {folder} 失败：{ex.Message}");
                        Console.WriteLine($"Renaming {folder} Failed:{ex.Message}");
                    }
                }
            }

            Console.WriteLine("已完成!");
            Console.WriteLine("Finished!");
        }

    }
}


