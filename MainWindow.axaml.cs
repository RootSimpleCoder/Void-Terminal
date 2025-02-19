using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.IO.Compression;
using NLua;
using System.Diagnostics;
using Avalonia.Layout;

namespace VoidTerminal
{
    public partial class MainWindow : Window
    {
        private string currentDirectory = Directory.GetCurrentDirectory();
        private System.Threading.CancellationTokenSource? pingCancellation;
        private System.Threading.CancellationTokenSource? luaCancellation;
        private List<string> commandHistory = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            inputBox!.KeyDown += InputBox_KeyDown;
            inputBox!.Focus();
        }

        private async void InputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrEmpty(inputBox!.Text))
            {
                string command = inputBox.Text.Trim();
                inputBox.Text = string.Empty;
                
                commandHistory.Add(command);
                
                AddOutput($"[ {command.ToUpper()} ]");
                await ProcessCommand(command);
            }
            else if (e.Key == Key.Escape)
            {
                if (pingCancellation != null)
                {
                    pingCancellation.Cancel();
                    AddOutput("PING OPERATION CANCELLED");
                }
                if (luaCancellation != null)
                {
                    luaCancellation.Cancel();
                    AddOutput("LUA SCRIPT CANCELLED");
                }
            }
        }

        private async Task ProcessCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            switch (cmd)
            {
                case "help":
                    AddOutput("AVAILABLE COMMANDS:", true);
                    AddOutput("  HELP    - SHOW THIS HELP", false);
                    AddOutput("  CLEAR   - CLEAR TERMINAL", false);
                    AddOutput("  STATUS  - SHOW SYSTEM STATUS", false);
                    AddOutput("  EXIT    - TERMINATE SESSION", false);
                    AddOutput("  CD      - CHANGE DIRECTORY", false);
                    AddOutput("  PWD     - PRINT WORKING DIRECTORY", false);
                    AddOutput("  LS      - LIST DIRECTORY CONTENTS", false);
                    AddOutput("  TREE    - DISPLAY DIRECTORY STRUCTURE", false);
                    AddOutput("  MKDIR   - CREATE DIRECTORY", false);
                    AddOutput("  RMDIR   - REMOVE DIRECTORY", false);
                    AddOutput("  RM      - REMOVE FILE", false);
                    AddOutput("  TOUCH   - CREATE EMPTY FILE", false);
                    AddOutput("  CAT     - DISPLAY FILE CONTENTS", false);
                    AddOutput("  PING    - TEST HOST AVAILABILITY", false);
                    AddOutput("  RENAME  - RENAME FILE OR DIRECTORY", false);
                    AddOutput("  ECHO    - DISPLAY TEXT MESSAGE", false);
                    AddOutput("  COPY    - COPY FILE TO DESTINATION", false);
                    AddOutput("  HISTORY - SHOW COMMAND HISTORY", false);
                    AddOutput("  DATE    - SHOW CURRENT DATE AND TIME", false);
                    AddOutput("  ZIP     - CREATE ZIP ARCHIVE", false);
                    AddOutput("  UNZIP   - EXTRACT ZIP ARCHIVE", false);
                    AddOutput("  EDIT    - OPEN TEXT EDITOR", false);
                    AddOutput("  LUA     - EXECUTE LUA SCRIPT", false);
                    AddOutput("  GAME    - PLAY SNAKE GAME", false);
                    AddOutput("  FIND    - SEARCH FOR FILES BY PATTERN", false);
                    AddOutput("  GREP    - SEARCH FOR TEXT IN FILES", false);
                    AddOutput("  CALC    - SIMPLE CALCULATOR", false);
                    AddOutput("  DISK    - SHOW DISK INFORMATION", false);
                    AddOutput("  DIFF    - COMPARE TWO FILES", false);
                    AddOutput("  SYSINFO - SHOW SYSTEM INFORMATION", false);
                    AddOutput("  GENASC  - GENERATE ASCII ART TEXT (MAX 10 CHARS)", false);
                    AddOutput("  PS      - LIST RUNNING PROCESSES", false);
                    AddOutput("  KILL    - TERMINATE PROCESS BY PID", false);
                    AddOutput("  TOP     - MONITOR SYSTEM RESOURCES", false);
                    AddOutput("  INFO    - SHOW VOID TERMINAL INFORMATION", false);
                    break;

                case "cd":
                    if (args.Length == 0)
                    {
                        AddOutput($"CURRENT DIRECTORY: {currentDirectory}");
                        break;
                    }
                    try
                    {
                        string newPath = Path.GetFullPath(Path.Combine(currentDirectory, args[0]));
                        if (Directory.Exists(newPath))
                        {
                            currentDirectory = newPath;
                            AddOutput($"CHANGED DIRECTORY TO: {currentDirectory}");
                        }
                        else
                        {
                            AddOutput("ERROR: DIRECTORY NOT FOUND");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "ls":
                    try
                    {
                        string targetDir = args.Length > 0 ? 
                            Path.GetFullPath(Path.Combine(currentDirectory, args[0])) : 
                            currentDirectory;

                        if (!Directory.Exists(targetDir))
                        {
                            AddOutput("ERROR: DIRECTORY NOT FOUND");
                            break;
                        }

                        AddOutput($"CONTENTS OF: {targetDir}", true);
                        var dirs = Directory.GetDirectories(targetDir);
                        var files = Directory.GetFiles(targetDir);

                        foreach (var dir in dirs)
                        {
                            AddOutput($"[DIR]  {Path.GetFileName(dir)}");
                        }
                        foreach (var file in files)
                        {
                            AddOutput($"[FILE] {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "tree":
                    try
                    {
                        string targetDir = args.Length > 0 ? 
                            Path.GetFullPath(Path.Combine(currentDirectory, args[0])) : 
                            currentDirectory;

                        if (!Directory.Exists(targetDir))
                        {
                            AddOutput("ERROR: DIRECTORY NOT FOUND");
                            break;
                        }

                        AddOutput($"DIRECTORY STRUCTURE OF: {targetDir}", true);
                        PrintDirectoryTree(targetDir, "");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "mkdir":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: DIRECTORY NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string newDir = Path.Combine(currentDirectory, args[0]);
                        Directory.CreateDirectory(newDir);
                        AddOutput($"CREATED DIRECTORY: {newDir}");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "rmdir":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: DIRECTORY NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string dirToRemove = Path.Combine(currentDirectory, args[0]);
                        if (Directory.Exists(dirToRemove))
                        {
                            Directory.Delete(dirToRemove, recursive: true);
                            AddOutput($"REMOVED DIRECTORY: {dirToRemove}");
                        }
                        else
                        {
                            AddOutput("ERROR: DIRECTORY NOT FOUND");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "rm":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: FILE NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string fileToRemove = Path.Combine(currentDirectory, args[0]);
                        if (File.Exists(fileToRemove))
                        {
                            File.Delete(fileToRemove);
                            AddOutput($"REMOVED FILE: {fileToRemove}");
                        }
                        else
                        {
                            AddOutput("ERROR: FILE NOT FOUND");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "touch":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: FILE NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string newFile = Path.Combine(currentDirectory, args[0]);
                        File.Create(newFile).Dispose();
                        AddOutput($"CREATED FILE: {newFile}");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "ping":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: HOST NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        AddOutput($"PINGING {args[0]}...");
                        AddOutput("PRESS ESC TO CANCEL");
                        
                        pingCancellation?.Dispose();
                        pingCancellation = new System.Threading.CancellationTokenSource();
                        
                        var ping = new System.Net.NetworkInformation.Ping();
                        const int attempts = 4;
                        const int timeout = 5000; 

                        var successCount = 0;
                        var totalTime = 0.0;
                        var minTime = double.MaxValue;
                        var maxTime = 0.0;
                        var times = new double[attempts];
                        var startTime = DateTime.Now;

                        for (int i = 0; i < attempts; i++)
                        {
                            if (pingCancellation.Token.IsCancellationRequested)
                            {
                                break;
                            }

                            try 
                            {
                                var reply = await ping.SendPingAsync(args[0], timeout);
                                
                                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    successCount++;
                                    var time = reply.RoundtripTime;
                                    times[i] = time;
                                    totalTime += time;
                                    minTime = Math.Min(minTime, time);
                                    maxTime = Math.Max(maxTime, time);
                                    AddOutput($"REPLY FROM {reply.Address}: TIME={time}MS");
                                }
                                else
                                {
                                    times[i] = -1;
                                    AddOutput($"ATTEMPT {i + 1}: {reply.Status}");
                                }
                            }
                            catch (Exception)
                            {
                                times[i] = -1;
                                AddOutput($"ATTEMPT {i + 1}: FAILED");
                            }
                            
                            if (i < attempts - 1 && !pingCancellation.Token.IsCancellationRequested)
                            {
                                await Task.Delay(1000, pingCancellation.Token).ContinueWith(t => { });
                            }
                        }

                        if (!pingCancellation.Token.IsCancellationRequested)
                        {
                            var elapsedTime = (DateTime.Now - startTime).TotalMilliseconds;
                            var packetLoss = ((attempts - successCount) * 100.0) / attempts;
                            
                            var avg = successCount > 0 ? totalTime / successCount : 0;
                            var sumSquares = 0.0;
                            var validTimes = times.Where(t => t >= 0);
                            foreach (var time in validTimes)
                            {
                                sumSquares += Math.Pow(time - avg, 2);
                            }
                            var mdev = successCount > 0 ? Math.Sqrt(sumSquares / successCount) : 0;

                            AddOutput(string.Empty);
                            AddOutput($"--- {args[0]} PING STATISTICS ---");
                            AddOutput($"{attempts} PACKETS TRANSMITTED, {successCount} RECEIVED, {packetLoss:F0}% PACKET LOSS, TIME {elapsedTime:F0}MS");
                            
                            if (successCount > 0)
                            {
                                AddOutput($"RTT MIN/AVG/MAX/MDEV = {minTime:F3}/{avg:F3}/{maxTime:F3}/{mdev:F3} MS");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    finally
                    {
                        pingCancellation?.Dispose();
                        pingCancellation = null;
                    }
                    break;

                case "clear":
                    outputPanel!.Children.Clear();
                    break;

                case "status":
                    AddOutput("SYSTEM STATUS:", true);
                    AddOutput("ALL SYSTEMS OPERATIONAL", false);
                    AddOutput("VOID CONNECTION: STABLE", false);
                    break;

                case "exit":
                    AddOutput("TERMINATING SESSION...");
                    Close();
                    break;

                case "pwd":
                    AddOutput($"CURRENT DIRECTORY: {currentDirectory}");
                    break;

                case "cat":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: FILE NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string filePath = Path.Combine(currentDirectory, args[0]);
                        if (File.Exists(filePath))
                        {
                            string content = File.ReadAllText(filePath);
                            AddOutput($"CONTENTS OF {args[0]}:", true);
                            foreach (string line in content.Split('\n'))
                            {
                                AddOutput(line.TrimEnd('\r'));
                            }
                        }
                        else
                        {
                            AddOutput("ERROR: FILE NOT FOUND");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "rename":
                    if (args.Length != 2)
                    {
                        AddOutput("ERROR: USAGE: RENAME <OLD_NAME> <NEW_NAME>");
                        break;
                    }
                    try
                    {
                        string oldPath = Path.Combine(currentDirectory, args[0]);
                        string newPath = Path.Combine(currentDirectory, args[1]);

                        if (!File.Exists(oldPath) && !Directory.Exists(oldPath))
                        {
                            AddOutput("ERROR: SOURCE FILE OR DIRECTORY NOT FOUND");
                            break;
                        }

                        if (File.Exists(newPath) || Directory.Exists(newPath))
                        {
                            AddOutput("ERROR: DESTINATION ALREADY EXISTS");
                            break;
                        }

                        if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, newPath);
                            AddOutput($"RENAMED FILE: {args[0]} -> {args[1]}");
                        }
                        else
                        {
                            Directory.Move(oldPath, newPath);
                            AddOutput($"RENAMED DIRECTORY: {args[0]} -> {args[1]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "echo":
                    if (args.Length == 0)
                    {
                        AddOutput(string.Empty);
                        break;
                    }
                    string message = string.Join(" ", args);
                    AddOutput(message);
                    break;

                case "copy":
                    if (args.Length != 2)
                    {
                        AddOutput("ERROR: USAGE: COPY <SOURCE> <DESTINATION>");
                        break;
                    }
                    try
                    {
                        string sourcePath = Path.Combine(currentDirectory, args[0]);
                        string destPath = Path.Combine(currentDirectory, args[1]);

                        if (!File.Exists(sourcePath))
                        {
                            AddOutput("ERROR: SOURCE FILE NOT FOUND");
                            break;
                        }

                        File.Copy(sourcePath, destPath, false);
                        AddOutput($"COPIED: {args[0]} -> {args[1]}");
                    }
                    catch (IOException ex) when ((ex.HResult & 0x0000FFFF) == 0x0B)
                    {
                        AddOutput("ERROR: DESTINATION FILE ALREADY EXISTS");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "history":
                    if (commandHistory.Count == 0)
                    {
                        AddOutput("NO COMMAND HISTORY");
                        break;
                    }
                    
                    AddOutput("COMMAND HISTORY:", true);
                    for (int i = 0; i < commandHistory.Count; i++)
                    {
                        AddOutput($"{i + 1,4}  {commandHistory[i]}");
                    }
                    break;

                case "date":
                    var now = DateTime.Now;
                    AddOutput($"CURRENT DATE: {now.ToLongDateString().ToUpper()}");
                    AddOutput($"CURRENT TIME: {now.ToLongTimeString().ToUpper()}");
                    break;

                case "zip":
                    if (args.Length < 2)
                    {
                        AddOutput("ERROR: USAGE: ZIP <ARCHIVE_NAME> <FILE_OR_DIR> [FILES...]");
                        break;
                    }
                    try
                    {
                        string zipPath = Path.Combine(currentDirectory, args[0]);
                        if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            zipPath += ".zip";
                        }

                        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                        {
                            for (int i = 1; i < args.Length; i++)
                            {
                                string sourcePath = Path.Combine(currentDirectory, args[i]);
                                if (File.Exists(sourcePath))
                                {
                                    archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath));
                                    AddOutput($"ADDED FILE: {args[i]}");
                                }
                                else if (Directory.Exists(sourcePath))
                                {
                                    string dirName = Path.GetFileName(sourcePath);
                                    archive.CreateEntry(dirName + "/");
                                    AddOutput($"ADDED DIRECTORY: {dirName}");

                                    foreach (string dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                                    {
                                        string relativePath = Path.GetRelativePath(sourcePath, dir);
                                        archive.CreateEntry(Path.Combine(dirName, relativePath) + "/");
                                        AddOutput($"ADDED DIRECTORY: {relativePath}");
                                    }

                                    foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                                    {
                                        string relativePath = Path.GetRelativePath(sourcePath, file);
                                        archive.CreateEntryFromFile(file, Path.Combine(dirName, relativePath));
                                        AddOutput($"ADDED FILE: {relativePath}");
                                    }
                                }
                                else
                                {
                                    AddOutput($"WARNING: {args[i]} NOT FOUND");
                                }
                            }
                        }
                        AddOutput($"CREATED ARCHIVE: {Path.GetFileName(zipPath)}");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "unzip":
                    if (args.Length < 1)
                    {
                        AddOutput("ERROR: USAGE: UNZIP <ARCHIVE_NAME> [DESTINATION]");
                        break;
                    }
                    try
                    {
                        string zipPath = Path.Combine(currentDirectory, args[0]);
                        string extractPath = args.Length > 1 
                            ? Path.Combine(currentDirectory, args[1])
                            : Path.Combine(currentDirectory, Path.GetFileNameWithoutExtension(args[0]));

                        if (!File.Exists(zipPath))
                        {
                            AddOutput("ERROR: ARCHIVE NOT FOUND");
                            break;
                        }

                        Directory.CreateDirectory(extractPath);
                        using (var archive = ZipFile.OpenRead(zipPath))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(extractPath, entry.FullName);
                                string? destinationDir = Path.GetDirectoryName(destinationPath);
                                
                                if (destinationDir != null)
                                {
                                    Directory.CreateDirectory(destinationDir);
                                }
                                
                                if (!string.IsNullOrEmpty(entry.Name))
                                {
                                    entry.ExtractToFile(destinationPath, overwrite: true);
                                    AddOutput($"EXTRACTED: {entry.FullName}");
                                }
                            }
                        }
                        AddOutput($"EXTRACTED TO: {extractPath}");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "edit":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: FILE NAME REQUIRED");
                        break;
                    }
                    try
                    {
                        string filePath = Path.Combine(currentDirectory, args[0]);
                        var editorWindow = new TextEditorWindow();
                        editorWindow.LoadFile(filePath);
                        editorWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "lua":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: LUA SCRIPT FILE REQUIRED");
                        break;
                    }
                    try
                    {
                        string scriptPath = Path.Combine(currentDirectory, args[0]);
                        if (!File.Exists(scriptPath))
                        {
                            AddOutput("ERROR: SCRIPT FILE NOT FOUND");
                            break;
                        }

                        string script = File.ReadAllText(scriptPath);
                        
                        luaCancellation?.Dispose();
                        luaCancellation = new System.Threading.CancellationTokenSource();
                        var token = luaCancellation.Token;
                        
                        AddOutput("PRESS ESC TO CANCEL SCRIPT EXECUTION");
                        
                        using var lua = new Lua();
                        
                        lua["print"] = new Action<object>(message => 
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            
                            try
                            {
                                Dispatcher.UIThread.Post(() => 
                                {
                                    if (!token.IsCancellationRequested)
                                    {
                                        AddOutput(message?.ToString()?.ToUpper() ?? "NIL");
                                    }
                                }, DispatcherPriority.Normal);
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.UIThread.Post(() => 
                                    AddOutput($"WARNING: ERROR IN PRINT FUNCTION: {ex.Message.ToUpper()}"));
                            }
                        });

                        lua["sleep"] = new Action<double>(seconds => 
                        {
                            if (seconds <= 0) return;
                            
                            try
                            {
                                int totalMs = (int)(seconds * 1000);
                                int interval = 50; 
                                
                                for (int i = 0; i < totalMs; i += interval)
                                {
                                    if (token.IsCancellationRequested)
                                        throw new Exception("SCRIPT EXECUTION CANCELLED");
                                        
                                    Thread.Sleep(Math.Min(interval, totalMs - i));
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("CANCELLED"))
                                    throw;
                                throw new Exception($"Error in sleep function: {ex.Message}");
                            }
                        });

                        AddOutput($"EXECUTING LUA SCRIPT: {args[0]}", true);
                        
                        await Task.Run(() => 
                        {
                            try 
                            {
                                lua.DoString(script);
                                
                                if (!token.IsCancellationRequested)
                                {
                                    Dispatcher.UIThread.Post(() => AddOutput("SCRIPT EXECUTION COMPLETED"));
                                }
                            }
                            catch (Exception ex)
                            {
                                var error = ex.Message;
                                if (error.Contains("CANCELLED"))
                                {
                                    Dispatcher.UIThread.Post(() => AddOutput("SCRIPT EXECUTION STOPPED"));
                                }
                                else
                                {
                                    Dispatcher.UIThread.Post(() => 
                                    {
                                        AddOutput($"LUA ERROR: {error.ToUpper()}");
                                        if (ex.InnerException != null)
                                        {
                                            AddOutput($"DETAILS: {ex.InnerException.Message.ToUpper()}");
                                        }
                                    });
                                }
                            }
                        }, token);
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"LUA ERROR: {ex.Message.ToUpper()}");
                    }
                    finally
                    {
                        if (luaCancellation != null)
                        {
                            luaCancellation.Dispose();
                            luaCancellation = null;
                        }
                    }
                    break;

                case "game":
                    try
                    {
                        var gameWindow = new GameWindow();
                        AddOutput("STARTING SNAKE GAME...");
                        AddOutput("USE ARROW KEYS TO CONTROL THE SNAKE");
                        AddOutput("COLLECT RED FOOD TO GROW AND SCORE POINTS");
                        AddOutput("PRESS ESC TO EXIT GAME");
                        await gameWindow.ShowDialog(this);
                        AddOutput("GAME SESSION ENDED");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "find":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: USAGE: FIND <PATTERN>");
                        break;
                    }
                    try
                    {
                        var pattern = args[0];
                        AddOutput($"SEARCHING FOR: {pattern}", true);
                        AddOutput("");

                        var files = Directory.GetFiles(currentDirectory, pattern, SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            AddOutput(Path.GetRelativePath(currentDirectory, file));
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "calc":
                    if (args.Length < 3)
                    {
                        AddOutput("ERROR: USAGE: CALC <NUMBER1> <OPERATOR> <NUMBER2>");
                        AddOutput("OPERATORS: + - * / %");
                        break;
                    }
                    try
                    {
                        AddOutput("CALCULATOR", true);
                        AddOutput("");

                        if (double.TryParse(args[0], out double num1) && double.TryParse(args[2], out double num2))
                        {
                            double calcResult = args[1] switch
                            {
                                "+" => num1 + num2,
                                "-" => num1 - num2,
                                "*" => num1 * num2,
                                "/" => num2 != 0 ? num1 / num2 : throw new DivideByZeroException(),
                                "%" => num2 != 0 ? num1 % num2 : throw new DivideByZeroException(),
                                _ => throw new ArgumentException("INVALID OPERATOR")
                            };

                            AddOutput($"{num1} {args[1]} {num2} = {calcResult}");
                        }
                        else
                        {
                            AddOutput("ERROR: INVALID NUMBERS");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "disk":
                    try
                    {
                        var info = new System.Text.StringBuilder();
                        info.AppendLine("DISK INFORMATION");
                        info.AppendLine();

                        var drives = DriveInfo.GetDrives();
                        foreach (var drive in drives)
                        {
                            try
                            {
                                info.AppendLine($"DRIVE: {drive.Name}");
                                if (drive.IsReady)
                                {
                                    info.AppendLine($"  LABEL: {drive.VolumeLabel}");
                                    info.AppendLine($"  FORMAT: {drive.DriveFormat}");
                                    info.AppendLine($"  TYPE: {drive.DriveType}");
                                    info.AppendLine($"  TOTAL SIZE: {FormatSize(drive.TotalSize)}");
                                    info.AppendLine($"  FREE SPACE: {FormatSize(drive.AvailableFreeSpace)}");
                                    info.AppendLine($"  USED SPACE: {FormatSize(drive.TotalSize - drive.AvailableFreeSpace)}");
                                }
                                else
                                {
                                    info.AppendLine("  [DRIVE NOT READY]");
                                }
                                info.AppendLine();
                            }
                            catch
                            {
                                info.AppendLine("  [ERROR ACCESSING DRIVE]");
                                info.AppendLine();
                            }
                        }

                        var outputWindow = new OutputWindow();
                        outputWindow.SetContent("Disk Information", info.ToString());
                        await outputWindow.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "grep":
                    if (args.Length < 2)
                    {
                        AddOutput("ERROR: USAGE: GREP <PATTERN> <FILE>");
                        break;
                    }
                    try
                    {
                        var pattern = args[0];
                        var filePath = Path.Combine(currentDirectory, args[1]);
                        var results = new System.Text.StringBuilder();
                        results.AppendLine($"SEARCHING FOR: {pattern}");
                        results.AppendLine($"IN FILE: {args[1]}");
                        results.AppendLine();

                        if (File.Exists(filePath))
                        {
                            var lines = File.ReadAllLines(filePath);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                                {
                                    results.AppendLine($"LINE {i + 1}: {lines[i]}");
                                }
                            }
                        }
                        else
                        {
                            results.AppendLine("ERROR: FILE NOT FOUND");
                        }

                        var outputWindow = new OutputWindow();
                        outputWindow.SetContent($"Grep Results - {pattern}", results.ToString());
                        await outputWindow.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "diff":
                    if (args.Length < 2)
                    {
                        AddOutput("ERROR: USAGE: DIFF <FILE1> <FILE2>");
                        break;
                    }
                    try
                    {
                        var file1Path = Path.Combine(currentDirectory, args[0]);
                        var file2Path = Path.Combine(currentDirectory, args[1]);
                        
                        AddOutput($"COMPARING FILES:", true);
                        AddOutput($"FILE 1: {args[0]}");
                        AddOutput($"FILE 2: {args[1]}");
                        AddOutput("");

                        if (!File.Exists(file1Path))
                        {
                            AddOutput($"ERROR: FILE NOT FOUND: {args[0]}");
                        }
                        else if (!File.Exists(file2Path))
                        {
                            AddOutput($"ERROR: FILE NOT FOUND: {args[1]}");
                        }
                        else
                        {
                            var lines1 = File.ReadAllLines(file1Path);
                            var lines2 = File.ReadAllLines(file2Path);
                            
                            if (lines1.SequenceEqual(lines2))
                            {
                                AddOutput("FILES ARE IDENTICAL");
                            }
                            else
                            {
                                AddOutput("FILES ARE DIFFERENT");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "sysinfo":
                    try
                    {
                        AddOutput($"    ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄    ", true);
                        AddOutput($"    █ SYSTEM INFORMATION █    ", true);
                        AddOutput($"    ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀    ", true);
                        AddOutput("");
                        
                        AddOutput($"OS VERSION: {Environment.OSVersion.VersionString}");
                        AddOutput($"PLATFORM: {Environment.OSVersion.Platform}");
                        AddOutput($"ARCHITECTURE: {(Environment.Is64BitOperatingSystem ? "64-BIT" : "32-BIT")}");
                        AddOutput("");
                        
                        AddOutput($"USER NAME: {Environment.UserName}");
                        AddOutput($"CURRENT DIRECTORY: {currentDirectory}");
                        AddOutput("");
                        
                        AddOutput($"PROCESSOR COUNT: {Environment.ProcessorCount}");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "genasc":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: TEXT REQUIRED");
                        AddOutput("USAGE: GENASC <TEXT>");
                        AddOutput("NOTE: MAXIMUM 10 CHARACTERS");
                        break;
                    }

                    string text = string.Join(" ", args).ToUpper();
                    if (text.Length > 10)
                    {
                        AddOutput("ERROR: TEXT TOO LONG (MAX 10 CHARACTERS)");
                        break;
                    }

                    AddOutput("GENERATING ASCII ART:", true);
                    AddOutput("");

                    var asciiPatterns = new Dictionary<char, string[]>
                    {
                        {'A', new[] {
                            "  █  ",
                            " ███ ",
                            "█   █",
                            "█████",
                            "█   █"
                        }},
                        {'B', new[] {
                            "████ ",
                            "█   █",
                            "████ ",
                            "█   █",
                            "████ "
                        }},
                        {'C', new[] {
                            " ████",
                            "█    ",
                            "█    ",
                            "█    ",
                            " ████"
                        }},
                        {'D', new[] {
                            "████ ",
                            "█   █",
                            "█   █",
                            "█   █",
                            "████ "
                        }},
                        {'E', new[] {
                            "█████",
                            "█    ",
                            "████ ",
                            "█    ",
                            "█████"
                        }},
                        {'F', new[] {
                            "█████",
                            "█    ",
                            "████ ",
                            "█    ",
                            "█    "
                        }},
                        {'G', new[] {
                            " ████",
                            "█    ",
                            "█  ██",
                            "█   █",
                            " ████"
                        }},
                        {'H', new[] {
                            "█   █",
                            "█   █",
                            "█████",
                            "█   █",
                            "█   █"
                        }},
                        {'I', new[] {
                            "█████",
                            "  █  ",
                            "  █  ",
                            "  █  ",
                            "█████"
                        }},
                        {'J', new[] {
                            "█████",
                            "   █ ",
                            "   █ ",
                            "█  █ ",
                            " ██  "
                        }},
                        {'K', new[] {
                            "█   █",
                            "█  █ ",
                            "███  ",
                            "█  █ ",
                            "█   █"
                        }},
                        {'L', new[] {
                            "█    ",
                            "█    ",
                            "█    ",
                            "█    ",
                            "█████"
                        }},
                        {'M', new[] {
                            "█   █",
                            "██ ██",
                            "█ █ █",
                            "█   █",
                            "█   █"
                        }},
                        {'N', new[] {
                            "█   █",
                            "██  █",
                            "█ █ █",
                            "█  ██",
                            "█   █"
                        }},
                        {'O', new[] {
                            " ███ ",
                            "█   █",
                            "█   █",
                            "█   █",
                            " ███ "
                        }},
                        {'P', new[] {
                            "████ ",
                            "█   █",
                            "████ ",
                            "█    ",
                            "█    "
                        }},
                        {'Q', new[] {
                            " ███ ",
                            "█   █",
                            "█   █",
                            "█  █ ",
                            " ██ █"
                        }},
                        {'R', new[] {
                            "████ ",
                            "█   █",
                            "████ ",
                            "█  █ ",
                            "█   █"
                        }},
                        {'S', new[] {
                            " ████",
                            "█    ",
                            " ███ ",
                            "    █",
                            "████ "
                        }},
                        {'T', new[] {
                            "█████",
                            "  █  ",
                            "  █  ",
                            "  █  ",
                            "  █  "
                        }},
                        {'U', new[] {
                            "█   █",
                            "█   █",
                            "█   █",
                            "█   █",
                            " ███ "
                        }},
                        {'V', new[] {
                            "█   █",
                            "█   █",
                            "█   █",
                            " █ █ ",
                            "  █  "
                        }},
                        {'W', new[] {
                            "█   █",
                            "█   █",
                            "█ █ █",
                            "██ ██",
                            "█   █"
                        }},
                        {'X', new[] {
                            "█   █",
                            " █ █ ",
                            "  █  ",
                            " █ █ ",
                            "█   █"
                        }},
                        {'Y', new[] {
                            "█   █",
                            " █ █ ",
                            "  █  ",
                            "  █  ",
                            "  █  "
                        }},
                        {'Z', new[] {
                            "█████",
                            "   █ ",
                            "  █  ",
                            " █   ",
                            "█████"
                        }},
                        {'0', new[] {
                            " ███ ",
                            "█   █",
                            "█   █",
                            "█   █",
                            " ███ "
                        }},
                        {'1', new[] {
                            "  █  ",
                            " ██  ",
                            "  █  ",
                            "  █  ",
                            " ███ "
                        }},
                        {'2', new[] {
                            " ███ ",
                            "    █",
                            " ███ ",
                            "█    ",
                            "█████"
                        }},
                        {'3', new[] {
                            "████ ",
                            "    █",
                            " ███ ",
                            "    █",
                            "████ "
                        }},
                        {'4', new[] {
                            "█   █",
                            "█   █",
                            "█████",
                            "    █",
                            "    █"
                        }},
                        {'5', new[] {
                            "█████",
                            "█    ",
                            "████ ",
                            "    █",
                            "████ "
                        }},
                        {'6', new[] {
                            " ███ ",
                            "█    ",
                            "████ ",
                            "█   █",
                            " ███ "
                        }},
                        {'7', new[] {
                            "█████",
                            "   █ ",
                            "  █  ",
                            " █   ",
                            "█    "
                        }},
                        {'8', new[] {
                            " ███ ",
                            "█   █",
                            " ███ ",
                            "█   █",
                            " ███ "
                        }},
                        {'9', new[] {
                            " ███ ",
                            "█   █",
                            " ████",
                            "    █",
                            " ███ "
                        }},
                        {' ', new[] {
                            "     ",
                            "     ",
                            "     ",
                            "     ",
                            "     "
                        }}
                    };

                    var result = new string[5];
                    foreach (char c in text)
                    {
                        if (asciiPatterns.ContainsKey(c))
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                result[i] += asciiPatterns[c][i] + " ";
                            }
                        }
                    }

                    foreach (string line in result)
                    {
                        AddOutput(line);
                    }
                    AddOutput("");
                    break;

                case "ps":
                    try 
                    {
                        var processes = Process.GetProcesses();
                        AddOutput("RUNNING PROCESSES:", true);
                        AddOutput($"{"PID",-8} {"Memory (MB)",-12} {"Name",-30} {"Responding",-10}");
                        AddOutput(new string('-', 60));
                        
                        foreach (var process in processes.OrderBy(p => p.Id))
                        {
                            try 
                            {
                                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                                AddOutput($"{process.Id,-8} {memoryMB,-12} {process.ProcessName,-30} {process.Responding,-10}");
                            }
                            catch 
                            {
                                // what?
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "kill":
                    if (args.Length == 0)
                    {
                        AddOutput("ERROR: PROCESS ID REQUIRED");
                        AddOutput("USAGE: KILL <PID>");
                        break;
                    }
                    try 
                    {
                        if (int.TryParse(args[0], out int pid))
                        {
                            var process = Process.GetProcessById(pid);
                            process.Kill();
                            AddOutput($"PROCESS {pid} TERMINATED");
                        }
                        else
                        {
                            AddOutput("ERROR: INVALID PROCESS ID");
                        }
                    }
                    catch (ArgumentException)
                    {
                        AddOutput($"ERROR: PROCESS WITH ID {args[0]} NOT FOUND");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "top":
                    try 
                    {
                        AddOutput("STARTING SYSTEM MONITOR...");
                        
                        var topWindow = new Window
                        {
                            Title = "System Monitor",
                            Width = 600,
                            Height = 400,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };

                        var outputText = new TextBlock
                        {
                            FontFamily = new FontFamily("Courier New"),
                            Foreground = Brushes.White
                        };

                        var scrollViewer = new ScrollViewer
                        {
                            Content = outputText,
                            Margin = new Thickness(10)
                        };

                        topWindow.Content = scrollViewer;
                        topWindow.Background = new SolidColorBrush(Color.Parse("#000000"));

                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };

                        timer.Tick += (s, e) =>
                        {
                            var processes = Process.GetProcesses()
                                .OrderByDescending(p => p.WorkingSet64)
                                .Take(15);

                            var output = new System.Text.StringBuilder();
                            output.AppendLine($"TOP PROCESSES BY MEMORY USAGE - {DateTime.Now:HH:mm:ss}");
                            output.AppendLine();
                            output.AppendLine($"{"PID",-8} {"Memory (MB)",-12} {"CPU Time",-15} {"Name",-30}");
                            output.AppendLine(new string('-', 65));

                            foreach (var process in processes)
                            {
                                try 
                                {
                                    var memoryMB = process.WorkingSet64 / 1024 / 1024;
                                    output.AppendLine($"{process.Id,-8} {memoryMB,-12} {process.TotalProcessorTime.ToString(@"hh\:mm\:ss"),-15} {process.ProcessName,-30}");
                                }
                                catch 
                                {
                                    // what?
                                }
                            }

                            outputText.Text = output.ToString();
                        };

                        timer.Start();

                        topWindow.Closed += (s, e) => 
                        {
                            timer.Stop();
                            AddOutput("SYSTEM MONITOR TERMINATED");
                        };
                        
                        await topWindow.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                case "info":
                    try 
                    {
                        AddOutput($"    ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄    ", true);
                        AddOutput($"    █ VOID TERMINAL INFO █    ", true);
                        AddOutput($"    ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀    ", true);
                        AddOutput("");
                        
                        AddOutput("DESCRIPTION:", true);
                        AddOutput("VOID TERMINAL IS A POWERFUL TERMINAL EMULATOR THAT COMBINES");
                        AddOutput("CLASSIC COMMAND-LINE FUNCTIONALITY WITH A MODERN ARCHITECTURE.");
                        AddOutput("IT PROVIDES A STYLISH RETRO INTERFACE WITH A DARK THEME,");
                        AddOutput("OFFERING EXTENSIVE FILE SYSTEM MANAGEMENT, NETWORK TOOLS,");
                        AddOutput("AND LUA SCRIPT EXECUTION CAPABILITIES.");
                        AddOutput("");
                        
                        AddOutput("VERSION:", true);
                        AddOutput("1.1");
                        AddOutput("");
                        
                        AddOutput("AUTHOR:", true);
                        AddOutput("ROOTSIMPLECODER");
                        AddOutput("");
                        
                        AddOutput("LINKS:", true);
                        AddHyperlink("GITHUB: HTTPS://GITHUB.COM/ROOTSIMPLECODER/VOID-TERMINAL", 
                                     "https://github.com/RootSimpleCoder/Void-Terminal");
                        AddHyperlink("TELEGRAM: HTTPS://T.ME/VOID_TERMINAL",
                                     "https://t.me/Void_Terminal");
                        AddOutput("");
                        
                        AddOutput("FEATURES:", true);
                        AddOutput("• COMPLETE FILE SYSTEM OPERATIONS");
                        AddOutput("• NETWORK TOOLS");
                        AddOutput("• LUA SCRIPT EXECUTION");
                        AddOutput("• TEXT EDITOR");
                        AddOutput("• SNAKE GAME");
                        AddOutput("• SYSTEM MONITORING");
                        AddOutput("• PROCESS MANAGEMENT");
                        AddOutput("");
                        
                        AddOutput("TYPE 'HELP' TO SEE ALL AVAILABLE COMMANDS");
                    }
                    catch (Exception ex)
                    {
                        AddOutput($"ERROR: {ex.Message.ToUpper()}");
                    }
                    break;

                default:
                    AddOutput("ERROR: UNKNOWN COMMAND", true);
                    break;
            }
            AddOutput(string.Empty);
        }

        private void PrintDirectoryTree(string dir, string indent)
        {
            try
            {
                AddOutput($"{indent}[+] {Path.GetFileName(dir) ?? dir}");
                indent += "   ";

                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    PrintDirectoryTree(subDir, indent);
                }

                foreach (string file in Directory.GetFiles(dir))
                {
                    AddOutput($"{indent}|- {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                AddOutput($"ERROR ACCESSING {dir}: {ex.Message.ToUpper()}");
            }
        }

        private void AddOutput(string text, bool isBold = false)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.Parse("#FFFFFF")),
                FontFamily = new FontFamily("Courier New"),
                FontSize = 18,
                FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = outputPanel!.Bounds.Width > 0 ? 
                    outputPanel.Bounds.Width - 40 : 
                    760 
            };
            outputPanel!.Children.Add(textBlock);

            if (outputPanel.Parent is ScrollViewer scrollViewer)
            {
                Task.Delay(50).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        scrollViewer.ScrollToEnd();
                    });
                });
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void AddHyperlink(string text, string url)
        {
            var button = new Button
            {
                Content = text,
                Foreground = Brushes.Cyan,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            button.Click += (s, e) =>
            {
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            Arguments = url,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        Process.Start(startInfo);
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Process.Start("open", url);
                    }
                }
                catch (Exception ex)
                {
                    AddOutput($"ERROR OPENING URL: {ex.Message.ToUpper()}");
                }
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5),
                Children = { button }
            };

            outputPanel!.Children.Add(panel);
        }
    }
}