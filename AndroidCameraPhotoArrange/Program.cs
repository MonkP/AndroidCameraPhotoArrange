using System;
using System.IO;
using System.Globalization;

namespace FileOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("请将工作目录路径作为命令行参数传入");
                Console.WriteLine("按回车退出。");
                Console.ReadLine();
                return;
            }

            string workingDirectory = args[0];
            if (!Directory.Exists(workingDirectory))
            {
                Console.WriteLine($"指定的目录不存在: {workingDirectory}");
                Console.WriteLine("按回车退出。");
                Console.ReadLine();
                return;
            }

            try
            {
                Console.WriteLine($"开始整理目录: {workingDirectory}");
                int processedFiles = 0;
                int skippedFiles = 0;

                foreach (string filePath in Directory.GetFiles(workingDirectory))
                {
                    string fileName = Path.GetFileName(filePath);
                    string extension = Path.GetExtension(filePath).ToLower();

                    // 只处理jpg和mp4文件
                    if (extension != ".jpg" && extension != ".mp4")
                    {
                        skippedFiles++;
                        continue;
                    }

                    DateTime? fileDate = null;

                    // 尝试解析下划线分隔的文件名
                    fileDate = ParseUnderscoreFileName(fileName);

                    // 如果下划线解析失败，尝试解析时间戳文件名
                    if (fileDate == null)
                    {
                        fileDate = ParseTimestampFileName(fileName);
                    }

                    if (fileDate != null)
                    {
                        MoveFileToDateFolder(filePath, workingDirectory, fileDate.Value);
                        processedFiles++;
                    }
                    else
                    {
                        Console.WriteLine($"无法解析文件日期: {fileName}");
                        skippedFiles++;
                    }
                }

                Console.WriteLine($"整理完成！已处理: {processedFiles} 个文件，跳过: {skippedFiles} 个文件");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }
            Console.WriteLine("按回车退出。");
            Console.ReadLine();
        }

        static DateTime? ParseUnderscoreFileName(string fileName)
        {
            string[] parts = fileName.Split('_');
            if (parts.Length >= 3)
            {
                string datePart = parts[1]; // 第二段是日期
                string timePart = parts[2].Split('.')[0]; // 第三段是时间（去掉扩展名）

                if (datePart.Length == 8 && timePart.Length >= 4)
                {
                    try
                    {
                        // 解析日期部分：20190209 -> 2019-02-09
                        int year = int.Parse(datePart.Substring(0, 4));
                        int month = int.Parse(datePart.Substring(4, 2));
                        int day = int.Parse(datePart.Substring(6, 2));

                        // 解析时间部分：205622 -> 20:56:22
                        int hour = int.Parse(timePart.Substring(0, 2));
                        int minute = int.Parse(timePart.Substring(2, 2));
                        int second = timePart.Length > 4 ? int.Parse(timePart.Substring(4, 2)) : 0;

                        return new DateTime(year, month, day, hour, minute, second);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        static DateTime? ParseTimestampFileName(string fileName)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (long.TryParse(nameWithoutExtension, out long timestamp))
            {
                try
                {
                    // JavaScript时间戳是毫秒，需要转换为秒
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    return dateTimeOffset.LocalDateTime;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        static void MoveFileToDateFolder(string filePath, string workingDirectory, DateTime fileDate)
        {
            string fileName = Path.GetFileName(filePath);
            string year = fileDate.Year.ToString();
            string month = fileDate.Month.ToString("00");

            // 创建目标文件夹结构：工作目录/年/月
            string targetDirectory = Path.Combine(workingDirectory, year, month);
            Directory.CreateDirectory(targetDirectory);

            string targetPath = Path.Combine(targetDirectory, fileName);

            // 如果目标文件已存在，添加序号避免冲突
            int counter = 1;
            while (File.Exists(targetPath))
            {
                string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{counter}{Path.GetExtension(fileName)}";
                targetPath = Path.Combine(targetDirectory, newFileName);
                counter++;
            }

            File.Move(filePath, targetPath);
            Console.WriteLine($"已移动: {fileName} -> {Path.Combine(year, month, Path.GetFileName(targetPath))}");
        }
    }
}