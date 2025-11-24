// Assets/Editor/CodeCollector.cs
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CodeCollector : EditorWindow
{
    private string customFileName = "ProjectCodeReport.txt";
    private bool useAutoFilename = true; // по умолчанию — автоматическое имя
    private string selectedOutputFolder = "";
    private bool exportAsJson = false;
    private bool includeEmptyFolders = false;
    private bool includeMetaInfo = true;
    private bool includeGitInfo = true;
    private bool includeStats = true;
    private bool includeDependencies = true;

    [MenuItem("Tools/Collect All Source Files into Report")]
    public static void ShowWindow()
    {
        GetWindow<CodeCollector>("Code Collector");
    }

    void OnGUI()
    {
        GUILayout.Label("Code Collector Settings", EditorStyles.boldLabel);

        includeEmptyFolders = EditorGUILayout.Toggle("Include Empty Folders", includeEmptyFolders);
        includeMetaInfo = EditorGUILayout.Toggle("Include Metadata Headers", includeMetaInfo);
        includeGitInfo = EditorGUILayout.Toggle("Include Git Info", includeGitInfo);
        includeStats = EditorGUILayout.Toggle("Include Code Stats", includeStats);
        includeDependencies = EditorGUILayout.Toggle("Extract C# Dependencies", includeDependencies);
        exportAsJson = EditorGUILayout.Toggle("Export as JSON", exportAsJson);

        useAutoFilename = EditorGUILayout.Toggle("Use Auto-generated Filename", useAutoFilename);

        if (!useAutoFilename)
        {
            customFileName = EditorGUILayout.TextField("Custom File Name", customFileName);
        }
        else
        {
            string autoName = GetAutoFileName();
            EditorGUILayout.LabelField("Auto File Name:", autoName);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected Output Folder:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(string.IsNullOrEmpty(selectedOutputFolder) ? "<Not selected>" : selectedOutputFolder);

        if (GUILayout.Button("Select Output Folder"))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Folder to Save Report", "", "");
            if (!string.IsNullOrEmpty(folder))
            {
                selectedOutputFolder = folder;
            }
        }

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedOutputFolder));
        if (GUILayout.Button("Generate Report"))
        {
            try
            {
                GenerateReport();
            }
            catch (Exception e)
            {
                Debug.LogError("CodeCollector error: " + e);
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    string GetAutoFileName()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string ext = exportAsJson ? ".json" : ".txt";
        return $"CodeReport_{timestamp}{ext}";
    }

    string GetFinalFileName()
    {
        return useAutoFilename ? GetAutoFileName() : customFileName;
    }

    void GenerateReport()
    {
        if (string.IsNullOrEmpty(selectedOutputFolder))
        {
            Debug.LogError("Output folder not selected!");
            return;
        }

        string fileName = GetFinalFileName();
        string fullOutputPath = Path.Combine(selectedOutputFolder, fileName);

        // Убедимся, что расширение корректно
        if (!exportAsJson && !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            fullOutputPath += ".txt";
        }
        else if (exportAsJson && !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fullOutputPath += ".json";
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string[] extensions = { ".cs", ".asmdef", ".shader", ".compute", ".hlsl" };

        var allFiles = new List<string>();
        foreach (var ext in extensions)
        {
            allFiles.AddRange(Directory.GetFiles(Application.dataPath, "*" + ext, SearchOption.AllDirectories));
        }

        var folderStructure = BuildFolderStructure(Application.dataPath, allFiles.ToArray());

        GitInfo gitInfo = null;
        if (includeGitInfo)
        {
            gitInfo = GetGitInfo(projectRoot);
        }

        if (exportAsJson)
        {
            Debug.LogWarning("JSON export is not fully implemented with Unity's built-in tools. Falling back to text.");
            ExportAsText(fullOutputPath, projectRoot, allFiles, folderStructure, gitInfo);
        }
        else
        {
            ExportAsText(fullOutputPath, projectRoot, allFiles, folderStructure, gitInfo);
        }

        Debug.Log($"✅ Report saved to: {fullOutputPath}");
        EditorUtility.RevealInFinder(fullOutputPath);
    }

    void ExportAsText(string fullOutputPath, string projectRoot, List<string> allFiles, FolderNode folderStructure, GitInfo gitInfo)
    {
        StringBuilder report = new StringBuilder();

        if (includeMetaInfo)
        {
            report.AppendLine("=== PROJECT SOURCE REPORT ===");
            report.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total files: {allFiles.Count}");
            if (gitInfo != null)
            {
                report.AppendLine($"Git Branch: {gitInfo.Branch}");
                report.AppendLine($"Git Commit: {gitInfo.CommitHash}");
                report.AppendLine($"Git Dirty: {gitInfo.IsDirty}");
            }
            report.AppendLine();
            report.AppendLine("=== FOLDER STRUCTURE ===");
            AppendFolderStructure(report, folderStructure, 0);
            report.AppendLine();
            report.AppendLine("=== FILE CONTENTS ===");
            report.AppendLine();
        }

        foreach (string filePath in allFiles.OrderBy(p => p))
        {
            string relativePath = filePath.Replace(Application.dataPath + Path.DirectorySeparatorChar, "Assets/");
            string fileName = Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath).ToLower();

            string content = File.ReadAllText(filePath);
            string hash = ComputeSha256Hash(content);

            int lineCount = content.Split('\n').Length;
            CodeStats stats = null;
            List<string> usings = null;

            if (extension == ".cs" && includeStats)
            {
                stats = AnalyzeCSharpCode(content);
            }

            if (extension == ".cs" && includeDependencies)
            {
                usings = ExtractUsings(content);
            }

            if (includeMetaInfo)
            {
                report.AppendLine($"// FILE: {fileName}");
                report.AppendLine($"// PATH: {relativePath}");
                report.AppendLine($"// HASH: {hash}");
                if (stats != null)
                {
                    report.AppendLine($"// LINES: {lineCount} | CLASSES: {stats.ClassCount} | METHODS: {stats.MethodCount} | MONOBEHAVIOURS: {stats.MonoBehaviourCount}");
                }
                if (usings != null && usings.Count > 0)
                {
                    report.AppendLine($"// USING: {string.Join(", ", usings)}");
                }
                report.AppendLine("// " + new string('=', 80));
            }

            report.AppendLine(content);
            report.AppendLine();
            report.AppendLine("// " + new string('-', 80));
            report.AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
        File.WriteAllText(fullOutputPath, report.ToString(), Encoding.UTF8);
    }

    // --- Вспомогательные методы (без изменений) ---

    string ComputeSha256Hash(string input)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }

    GitInfo GetGitInfo(string repoPath)
    {
        try
        {
            var branch = RunGitCommand(repoPath, "rev-parse --abbrev-ref HEAD")?.Trim();
            var commit = RunGitCommand(repoPath, "rev-parse HEAD")?.Trim();
            var status = RunGitCommand(repoPath, "status --porcelain")?.Trim();
            bool dirty = !string.IsNullOrEmpty(status);
            return new GitInfo { Branch = branch, CommitHash = commit, IsDirty = dirty };
        }
        catch
        {
            return new GitInfo { Branch = "unknown", CommitHash = "unknown", IsDirty = true };
        }
    }

    string RunGitCommand(string workingDir, string args)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = System.Diagnostics.Process.Start(startInfo))
        {
            process.WaitForExit();
            if (process.ExitCode == 0)
                return process.StandardOutput.ReadToEnd();
            else
                return null;
        }
    }

    CodeStats AnalyzeCSharpCode(string code)
    {
        var classCount = Regex.Matches(code, @"\bclass\s+\w+").Count;
        var methodCount = Regex.Matches(code, @"\b(public|private|protected|internal)?\s*(static)?\s*\w+\s+\w+\s*\(").Count;
        var mbCount = Regex.Matches(code, @"class\s+\w+[^{]*:\s*[^{]*MonoBehaviour").Count;
        return new CodeStats { ClassCount = classCount, MethodCount = methodCount, MonoBehaviourCount = mbCount };
    }

    List<string> ExtractUsings(string code)
    {
        var matches = Regex.Matches(code, @"^\s*using\s+([a-zA-Z0-9_.]+);", RegexOptions.Multiline);
        return matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    // --- Структуры данных ---

    class GitInfo
    {
        public string Branch;
        public string CommitHash;
        public bool IsDirty;
    }

    class CodeStats
    {
        public int ClassCount;
        public int MethodCount;
        public int MonoBehaviourCount;
    }

    class FolderNode
    {
        public string Name;
        public bool HasFile;
        public FolderNode[] Children = new FolderNode[0];
    }

    FolderNode BuildFolderStructure(string rootPath, string[] filePaths)
    {
        var root = new FolderNode { Name = "Assets", HasFile = false };
        var rootDir = new DirectoryInfo(Application.dataPath);

        foreach (var dir in rootDir.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            string relativeDir = dir.FullName.Replace(Application.dataPath + Path.DirectorySeparatorChar, "");
            bool hasFile = filePaths.Any(p => p.StartsWith(dir.FullName));

            if (hasFile || includeEmptyFolders)
            {
                AddNodeToTree(root, relativeDir.Split(Path.DirectorySeparatorChar), hasFile);
            }
        }

        root.HasFile = filePaths.Any(p => Path.GetDirectoryName(p) == Application.dataPath);
        return root;
    }

    void AddNodeToTree(FolderNode parent, string[] pathParts, bool hasFile, int depth = 0)
    {
        if (depth >= pathParts.Length) return;
        string current = pathParts[depth];
        var existing = parent.Children.FirstOrDefault(c => c.Name == current);
        if (existing == null)
        {
            existing = new FolderNode { Name = current, HasFile = false };
            var list = parent.Children.ToList();
            list.Add(existing);
            parent.Children = list.OrderBy(c => c.Name).ToArray();
        }
        if (depth == pathParts.Length - 1)
        {
            existing.HasFile = existing.HasFile || hasFile;
        }
        AddNodeToTree(existing, pathParts, hasFile, depth + 1);
    }

    void AppendFolderStructure(StringBuilder sb, FolderNode node, int indent)
    {
        string prefix = new string(' ', indent * 2);
        string marker = node.HasFile ? "[F] " : "[ ] ";
        sb.AppendLine($"{prefix}{marker}{node.Name}");
        foreach (var child in node.Children)
        {
            AppendFolderStructure(sb, child, indent + 1);
        }
    }
}