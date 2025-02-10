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
    }
}