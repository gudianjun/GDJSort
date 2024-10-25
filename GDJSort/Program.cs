using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

class CsvGenerator
{
    static void Main()
    {
        string fileName = "C:\\GitHub\\SortOutTestCsv.csv";

        //  CrateLageFile(fileName);

        string tempDirectory = "C:\\GitHub\\temp";
        string sortedFileName = "C:\\GitHub\\sorted_large_data.csv";
        Stopwatch stopwatch = new Stopwatch(); // 创建一个 Stopwatch 对象
        stopwatch.Start(); // 开始计时
        // 创建临时目录
        if (!Directory.Exists(tempDirectory))
            Directory.CreateDirectory(tempDirectory);
        if (File.Exists(sortedFileName))
            File.Delete(sortedFileName);
        // 分段读取、排序并生成临时文件
        //List<string> tempFiles = SplitAndSortFile(fileName, tempDirectory).GetAwaiter().GetResult();
        List<MemoryStream> tempFiles = SplitAndSortMemory(fileName, tempDirectory).GetAwaiter().GetResult();
        //List<string> tempFiles = SplitAndSortFile2(fileName, tempDirectory) ;
        stopwatch.Stop(); // 停止计时
        TimeSpan executionTime = stopwatch.Elapsed; // 获取执行时间 
        Console.WriteLine($"SplitAndSortFile：{executionTime.TotalMilliseconds} 毫秒");
        stopwatch.Restart(); // 重新开始计时
        // 归并排序临时文件并生成最终排序结果
        //MergeSortedFiles(tempFiles, sortedFileName);
        MergeSortedMemory(tempFiles, sortedFileName);
        stopwatch.Stop(); // 停止计时
        executionTime = stopwatch.Elapsed; // 获取执行时间 
        Console.WriteLine($"排MergeSortedFiles：{executionTime.TotalMilliseconds} 毫秒");

        // 删除临时目录
        foreach (var tempFile in tempFiles)
        {
            // File.Delete(tempFile);
        }

        // CrateLageFile(fileName);
        // StartReadFile1(fileName);
        // StartReadFile2(fileName);
    }

    static List<string> SplitAndSortFile2(string fileName, string tempDirectory)
    {
        const int chunkSize = 100 * 1024 * 1024; // 50MB
        ConcurrentBag<string> tempFiles = new ConcurrentBag<string>();
        int fileIndex = 0;

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BufferedStream bs = new BufferedStream(fs))
        using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
        {
            char[] buffer = new char[chunkSize];
            int bytesRead;
            StringBuilder leftover = new StringBuilder();

            while ((bytesRead = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = leftover.Append(buffer, 0, bytesRead).ToString();
                int lastNewLineIndex = chunk.LastIndexOf('\n');

                if (lastNewLineIndex == -1)
                {
                    // 如果没有找到换行符，说明整个块都是不完整的行
                    continue;
                }

                // 分割完整的行和不完整的行
                string completeChunk = chunk.Substring(0, lastNewLineIndex + 1);
                leftover.Clear();
                leftover.Append(chunk.Substring(lastNewLineIndex + 1));

                string[] lines = completeChunk.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // 对块进行排序
                Array.Sort(lines, new CsvLineComparer());

                // 写入临时文件
                string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                File.WriteAllLines(tempFileName, lines, Encoding.UTF8);
                tempFiles.Add(tempFileName);

            }

            // 处理最后剩余的不完整行
            if (leftover.Length > 0)
            {

                string[] lines = leftover.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Array.Sort(lines, new CsvLineComparer());
                string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                File.WriteAllLines(tempFileName, lines, Encoding.UTF8);
                tempFiles.Add(tempFileName);

            }
        }


        return tempFiles.ToList();
    }
    static List<string> SplitAndSortFileTongbu(string fileName, string tempDirectory)
    {
        const int chunkSize = 50 * 1024 * 1024; // 50MB
        ConcurrentBag<string> tempFiles = new ConcurrentBag<string>();
        int fileIndex = 0;
        List<Task> tasks = new List<Task>();

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BufferedStream bs = new BufferedStream(fs))
        using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
        {
            char[] buffer = new char[chunkSize];
            int bytesRead;
            StringBuilder leftover = new StringBuilder();

            while ((bytesRead = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = leftover.Append(buffer, 0, bytesRead).ToString();
                int lastNewLineIndex = chunk.LastIndexOf('\n');

                if (lastNewLineIndex == -1)
                {
                    // 如果没有找到换行符，说明整个块都是不完整的行
                    continue;
                }

                // 分割完整的行和不完整的行
                string completeChunk = chunk.Substring(0, lastNewLineIndex + 1);
                leftover.Clear();
                leftover.Append(chunk.Substring(lastNewLineIndex + 1));

                string[] lines = completeChunk.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);


                // 对块进行排序
                // Array.Sort(lines, new CsvLineComparer());
                var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                Array.Sort(csvLines, (x, y) =>
                {
                    int result = string.Compare(x.Fields[2], y.Fields[2]);
                    if (result == 0)
                    {
                        result = string.Compare(x.Fields[3], y.Fields[3]);
                        if (result == 0)
                        {
                            result = string.Compare(x.Fields[4], y.Fields[4]);
                        }
                    }
                    return result;
                });
                // 写入临时文件
                string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                File.WriteAllLines(tempFileName, csvLines.Select(cl => cl.OriginalLine), Encoding.UTF8);
                tempFiles.Add(tempFileName);

            }

            // 处理最后剩余的不完整行
            if (leftover.Length > 0)
            {

                string[] lines = leftover.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // Array.Sort(lines, new CsvLineComparer());
                // 预处理数据
                var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                Array.Sort(csvLines, (x, y) =>
                {
                    int result = string.Compare(x.Fields[2], y.Fields[2]);
                    if (result == 0)
                    {
                        result = string.Compare(x.Fields[3], y.Fields[3]);
                        if (result == 0)
                        {
                            result = string.Compare(x.Fields[4], y.Fields[4]);
                        }
                    }
                    return result;
                });
                string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                File.WriteAllLines(tempFileName, csvLines.Select(cl => cl.OriginalLine), Encoding.UTF8);
                tempFiles.Add(tempFileName);

            }
        }


        return tempFiles.ToList();
    }
    static async Task<List<string>> SplitAndSortFile(string fileName, string tempDirectory)
    {
        const int chunkSize = 100 * 1024 * 1024; // 50MB
        ConcurrentBag<string> tempFiles = new ConcurrentBag<string>();
        int fileIndex = 0;
        List<Task> tasks = new List<Task>();

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BufferedStream bs = new BufferedStream(fs))
        using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
        {
            char[] buffer = new char[chunkSize];
            int bytesRead;
            StringBuilder leftover = new StringBuilder();

            while ((bytesRead = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = leftover.Append(buffer, 0, bytesRead).ToString();
                int lastNewLineIndex = chunk.LastIndexOf('\n');

                if (lastNewLineIndex == -1)
                {
                    // 如果没有找到换行符，说明整个块都是不完整的行
                    continue;
                }

                // 分割完整的行和不完整的行
                string completeChunk = chunk.Substring(0, lastNewLineIndex + 1);
                leftover.Clear();
                leftover.Append(chunk.Substring(lastNewLineIndex + 1));

                string[] lines = completeChunk.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var task = Task.Run(() =>
                {
                    // 对块进行排序
                    // Array.Sort(lines, new CsvLineComparer());
                    var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                    //Array.Sort(csvLines, (x, y) =>
                    //{
                    //    int result = string.Compare(x.Fields[2], y.Fields[2]);
                    //    if (result == 0)
                    //    {
                    //        result = string.Compare(x.Fields[3], y.Fields[3]);
                    //        if (result == 0)
                    //        {
                    //            result = string.Compare(x.Fields[4], y.Fields[4]);
                    //        }
                    //    }
                    //    return result;
                    //});
                    // 写入临时文件
                    string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                    File.WriteAllLines(tempFileName, csvLines.Select(cl => cl.OriginalLine), Encoding.UTF8);
                    tempFiles.Add(tempFileName);
                });
                tasks.Add(task);
            }

            // 处理最后剩余的不完整行
            if (leftover.Length > 0)
            {
                var task = Task.Run(() =>
                {
                    string[] lines = leftover.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    // Array.Sort(lines, new CsvLineComparer());
                    // 预处理数据
                    var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                    Array.Sort(csvLines, (x, y) =>
                    {
                        int result = string.Compare(x.Fields[2], y.Fields[2]);
                        if (result == 0)
                        {
                            result = string.Compare(x.Fields[3], y.Fields[3]);
                            if (result == 0)
                            {
                                result = string.Compare(x.Fields[4], y.Fields[4]);
                            }
                        }
                        return result;
                    });
                    string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                    File.WriteAllLines(tempFileName, csvLines.Select(cl => cl.OriginalLine), Encoding.UTF8);
                    tempFiles.Add(tempFileName);
                });
                tasks.Add(task);
            }
        }

        await Task.WhenAll(tasks);
        return tempFiles.ToList();
    }

    static async Task<List<MemoryStream>> SplitAndSortMemory(string fileName, string tempDirectory)
    {
        const int chunkSize = 100 * 1024 * 1024; // 50MB
        ConcurrentBag<MemoryStream> tempFiles = new ConcurrentBag<MemoryStream>();
        int fileIndex = 0;
        List<Task> tasks = new List<Task>();

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BufferedStream bs = new BufferedStream(fs))
        using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
        {
            char[] buffer = new char[chunkSize];
            int bytesRead;
            StringBuilder leftover = new StringBuilder();

            while ((bytesRead = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = leftover.Append(buffer, 0, bytesRead).ToString();
                int lastNewLineIndex = chunk.LastIndexOf('\n');

                if (lastNewLineIndex == -1)
                {
                    // 如果没有找到换行符，说明整个块都是不完整的行
                    continue;
                }

                // 分割完整的行和不完整的行
                string completeChunk = chunk.Substring(0, lastNewLineIndex + 1);
                leftover.Clear();
                leftover.Append(chunk.Substring(lastNewLineIndex + 1));

                string[] lines = completeChunk.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var task = Task.Run(() =>
                {
                    // 对块进行排序
                    // Array.Sort(lines, new CsvLineComparer());
                    var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                    //Array.Sort(csvLines, (x, y) =>
                    //{
                    //    int result = string.Compare(x.Fields[2], y.Fields[2]);
                    //    if (result == 0)
                    //    {
                    //        result = string.Compare(x.Fields[3], y.Fields[3]);
                    //        if (result == 0)
                    //        {
                    //            result = string.Compare(x.Fields[4], y.Fields[4]);
                    //        }
                    //    }
                    //    return result;
                    //});
                    // 写入临时文件
                    string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                    MemoryStream ms = new MemoryStream();

                    using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                    {
                         
                        foreach (var csvLine in csvLines)
                        {
                            sw.WriteLine(csvLine.OriginalLine);
                        }
                    }

                    tempFiles.Add(ms);
                });
                tasks.Add(task);
            }

            // 处理最后剩余的不完整行
            if (leftover.Length > 0)
            {
                var task = Task.Run(() =>
                {
                    string[] lines = leftover.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    // Array.Sort(lines, new CsvLineComparer());
                    // 预处理数据
                    var csvLines = lines.Select(line => new CsvLine(line)).ToArray();
                    Array.Sort(csvLines, (x, y) =>
                    {
                        int result = string.Compare(x.Fields[2], y.Fields[2]);
                        if (result == 0)
                        {
                            result = string.Compare(x.Fields[3], y.Fields[3]);
                            if (result == 0)
                            {
                                result = string.Compare(x.Fields[4], y.Fields[4]);
                            }
                        }
                        return result;
                    });
                    string tempFileName = Path.Combine(tempDirectory, $"temp_{Interlocked.Increment(ref fileIndex)}.csv");
                    MemoryStream ms = new MemoryStream();

                    using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                    {
                        foreach (var csvLine in csvLines)
                        {
                            sw.WriteLine(csvLine.OriginalLine);
                        }
                    }

                    tempFiles.Add(ms);
                });
                tasks.Add(task);
            }
        }

        await Task.WhenAll(tasks);
        return tempFiles.ToList();
    }
    static void MergeSortedFiles2(List<string> sortedFiles, string outputFileName)
    {
        var readers = sortedFiles.Select(file => new StreamReader(file, Encoding.UTF8)).ToList();
        var priorityQueue = new ConcurrentDictionary<string, int>();

        using (var writer = new StreamWriter(outputFileName, false, Encoding.UTF8))
        {
            Parallel.For(0, readers.Count, i =>
            {
                if (!readers[i].EndOfStream)
                {
                    string? line = readers[i].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.TryAdd(line, i);
                    }
                }
            });

            while (priorityQueue.Count > 0)
            {
                var kvp = priorityQueue.OrderBy(k => k.Key).First();
                writer.WriteLine(kvp.Key);

                int readerIndex = kvp.Value;
                priorityQueue.TryRemove(kvp.Key, out _);

                if (!readers[readerIndex].EndOfStream)
                {
                    string? line = readers[readerIndex].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.TryAdd(line, readerIndex);
                    }
                }
            }
        }

        foreach (var reader in readers)
        {
            reader.Close();
        }
    }
    static void MergeSortedFiles(List<string> sortedFiles, string outputFileName)
    {
        var readers = sortedFiles.Select(file => new StreamReader(file, Encoding.UTF8)).ToList();

        var priorityQueue = new SortedDictionary<string, int>();

        using (var writer = new StreamWriter(outputFileName, false, Encoding.UTF8))
        {
            // 初始化优先队列
            for (int i = 0; i < readers.Count; i++)
            {
                if (!readers[i].EndOfStream)
                {
                    string? line = readers[i].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.Add(line, i);
                    }
                }
            }

            // 归并排序
            while (priorityQueue.Count > 0)
            {
                var kvp = priorityQueue.First();
                writer.WriteLine(kvp.Key);

                int readerIndex = kvp.Value;
                priorityQueue.Remove(kvp.Key);

                if (!readers[readerIndex].EndOfStream)
                {
                    string? line = readers[readerIndex].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.Add(line, readerIndex);
                    }
                }
            }
        }

        // 关闭所有 StreamReader
        foreach (var reader in readers)
        {
            reader.Close();
        }
    }

    static void MergeSortedMemory(List<MemoryStream> sortedFiles, string outputFileName)
    {
        var readers = sortedFiles.Select(file => new StreamReader(file, Encoding.UTF8)).ToList();

        var priorityQueue = new SortedDictionary<string, int>();

        using (var writer = new StreamWriter(outputFileName, false, Encoding.UTF8))
        {
            // 初始化优先队列
            for (int i = 0; i < readers.Count; i++)
            {
                if (!readers[i].EndOfStream)
                {
                    string? line = readers[i].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.Add(line, i);
                    }
                }
            }

            // 归并排序
            while (priorityQueue.Count > 0)
            {
                var kvp = priorityQueue.First();
                writer.WriteLine(kvp.Key);

                int readerIndex = kvp.Value;
                priorityQueue.Remove(kvp.Key);

                if (!readers[readerIndex].EndOfStream)
                {
                    string? line = readers[readerIndex].ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        priorityQueue.Add(line, readerIndex);
                    }
                }
            }
        }

        // 关闭所有 StreamReader
        foreach (var reader in readers)
        {
            reader.Close();
        }
    }
    static void StartReadFile1(string fileName)
    {
        Stopwatch stopwatch = new Stopwatch(); // 创建一个 Stopwatch 对象
        stopwatch.Start(); // 开始计时
        // 用第一列排序
        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BufferedStream bs = new BufferedStream(fs))
        using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
        {
            string? line = null;
            while ((line = sr.ReadLine()) != null)
            {
                // 处理每一行
                //Console.WriteLine(line);
            }
        }
        stopwatch.Stop(); // 停止计时
        TimeSpan executionTime = stopwatch.Elapsed; // 获取执行时间


        Console.WriteLine($"排序完毕1！执行时长：{executionTime.TotalMilliseconds} 毫秒");
    }
    static void StartReadFile2(string fileName)
    {
        Stopwatch stopwatch = new Stopwatch(); // 创建一个 Stopwatch 对象
        stopwatch.Start(); // 开始计时
        using (StreamReader sr = new StreamReader(fileName, Encoding.UTF8))
        {
            string? line = null;
            while ((line = sr.ReadLine()) != null)
            {
                // 处理每一行
                //Console.WriteLine(line);
            }
        }
        stopwatch.Stop(); // 停止计时
        TimeSpan executionTime = stopwatch.Elapsed; // 获取执行时间


        Console.WriteLine($"排序完毕2！执行时长：{executionTime.TotalMilliseconds} 毫秒");
    }
    static void CrateLageFile(string filePath)
    {
        long targetSize = 1L * 100 * 1024 * 1024; // 1GB
        var columnTypes = new Func<Random, string>[30];

        Random random = new Random();

        // 定义每列的数据类型
        for (int i = 0; i < 30; i++)
        {
            int columnTypeIndex = i % 4;
            columnTypes[i] = columnTypeIndex switch
            {
                0 => (rand) => rand.Next(1, 10000).ToString(), // 数字列
                1 => (rand) => RandomString(rand, 8),          // 英文字母和数字列
                2 => (rand) => DateTime.Now.AddDays(-rand.Next(0, 1000)).ToString("yyyy-MM-dd"), // 日期列
                3 => (rand) => RandomJapaneseString(rand, 15),  // 日文字符列
                _ => (rand) => rand.Next(1, 10000).ToString()  // 默认数字
            };
        }

        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            // 写入标题行
            var header = new StringBuilder();
            for (int i = 1; i <= 30; i++)
            {
                header.Append($"Column{i}");
                if (i < 30) header.Append(",");
            }
            writer.WriteLine(header.ToString());

            long fileSize = new FileInfo(filePath).Length;

            while (fileSize < targetSize)
            {
                var line = new StringBuilder();
                for (int i = 0; i < 30; i++)
                {
                    line.Append(columnTypes[i](random)); // 根据定义的类型生成数据
                    if (i < 29) line.Append(",");
                }
                writer.WriteLine(line.ToString());

                // 更新文件大小
                fileSize = new FileInfo(filePath).Length;
            }
        }

        Console.WriteLine("CSV 文件生成完毕！");
    }
    static string RandomString(Random random, int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }

    static string RandomJapaneseString(Random random, int length)
    {
        const string japaneseChars = "あいうえおかきくけこさしすせそたちつてとなにぬねの";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = japaneseChars[random.Next(japaneseChars.Length)];
        }
        return new string(result);
    }
}
class CsvLineComparer : IComparer<string>
{
    // 2,3,4
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
            return 0;

        var xFields = x.Split(',');
        var yFields = y.Split(',');

        // 假设我们要根据第一个字段和第二个字段进行排序
        int result = string.Compare(xFields[2], yFields[2]);
        if (result == 0)
        {
            result = string.Compare(xFields[3], yFields[3]);
            if (result == 0)
            {
                result = string.Compare(xFields[4], yFields[4]);
            }
        }

        return result;
    }
}
class CsvLine
{
    public string OriginalLine { get; }
    public string[] Fields { get; }

    public CsvLine(string line)
    {
        OriginalLine = line;
        Fields = line.Split(',');
    }
}