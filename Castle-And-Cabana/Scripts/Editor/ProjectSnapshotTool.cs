using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility that mirrors the Unity project's <c>ProjectSettings\</c> folder
/// and the two <c>Packages\</c> manifest files into
/// <c>Assets\_Project\ProjectSnapshot~\</c>, so a from-scratch rebuild has everything
/// outside <c>_Project</c> that isn't an Asset Store/Package Manager download.
/// <para>
/// This is the manual counterpart to the git pre-commit hook at
/// <c>Assets\_Project\Tools~\git-hooks\pre-commit</c>, which performs the same mirror
/// automatically on every commit. This menu item does not touch git at all - run it
/// any time you want to eyeball the snapshot contents, or as a fallback if the hook
/// isn't configured (see PACKAGES.md's rebuild steps for enabling it).
/// </para>
/// </summary>
public static class ProjectSnapshotTool
{
    #region // Constants

    private const string MenuPath = "Tools/Castle and Cabana/Snapshot Project Settings";
    private const string SnapshotFolderName = "ProjectSnapshot~";
    private const string ProjectSettingsFolderName = "ProjectSettings";
    private const string PackagesFolderName = "Packages";

    /// <summary>The only files pulled out of <c>Packages\</c> - the manifest, not the installed packages themselves.</summary>
    private static readonly string[] PackagesFilesToSnapshot = { "manifest.json", "packages-lock.json" };

    #endregion

    #region // Menu Command

    /// <summary>
    /// Saves any pending in-memory asset/settings changes to disk, then mirrors
    /// <c>ProjectSettings\</c> and the <c>Packages\</c> manifest files into
    /// <c>Assets\_Project\ProjectSnapshot~\</c>. Logs a one-line summary of how many
    /// files were copied and removed.
    /// </summary>
    [MenuItem(MenuPath)]
    public static void SnapshotProjectSettings()
    {
        // Flush in-memory changes (e.g. a Project Settings window still showing as dirty)
        // to disk first, so the files we're about to copy are current.
        AssetDatabase.SaveAssets();

        string projectRoot = GetProjectRoot();
        string settingsSrc = Path.Combine(projectRoot, ProjectSettingsFolderName);
        string packagesSrc = Path.Combine(projectRoot, PackagesFolderName);

        if (!Directory.Exists(settingsSrc))
        {
            Debug.LogWarning($"[ProjectSnapshotTool] '{settingsSrc}' not found - nothing to snapshot.");
            return;
        }

        string snapshotRoot = Path.Combine(Application.dataPath, "_Project", SnapshotFolderName);
        string settingsDst = Path.Combine(snapshotRoot, ProjectSettingsFolderName);
        string packagesDst = Path.Combine(snapshotRoot, PackagesFolderName);

        int copied = 0;
        int removed = 0;

        MirrorDirectory(settingsSrc, settingsDst, ref copied, ref removed);
        MirrorPackagesManifests(packagesSrc, packagesDst, ref copied, ref removed);

        Debug.Log($"[ProjectSnapshotTool] Snapshot refreshed: {copied} file(s) copied, {removed} file(s) removed.");
    }

    #endregion

    #region // Path Resolution

    /// <summary>Returns the Unity project root (the folder containing <c>Assets\</c>), derived from <see cref="Application.dataPath"/>.</summary>
    private static string GetProjectRoot()
    {
        // Application.dataPath is ".../CastleAndCabana/Assets"
        return Directory.GetParent(Application.dataPath).FullName;
    }

    #endregion

    #region // Mirror Copy Logic

    /// <summary>
    /// Recursively mirrors every file from <paramref name="srcDir"/> into
    /// <paramref name="dstDir"/> (copying new/changed files, and deleting any file in
    /// the destination that no longer exists in the source), preserving raw bytes with
    /// no line-ending translation.
    /// </summary>
    private static void MirrorDirectory(string srcDir, string dstDir, ref int copied, ref int removed)
    {
        Directory.CreateDirectory(dstDir);

        var srcFiles = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
        var srcRelativePaths = new HashSet<string>();

        foreach (string srcFile in srcFiles)
        {
            string relativePath = GetRelativePath(srcDir, srcFile);
            srcRelativePaths.Add(relativePath);

            string dstFile = Path.Combine(dstDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile));

            if (!FilesAreIdentical(srcFile, dstFile))
            {
                File.Copy(srcFile, dstFile, overwrite: true);
                copied++;
            }
        }

        if (!Directory.Exists(dstDir)) return;

        // Mirror deletions: remove snapshot files whose source no longer exists.
        var dstFiles = Directory.GetFiles(dstDir, "*", SearchOption.AllDirectories);
        foreach (string dstFile in dstFiles)
        {
            string relativePath = GetRelativePath(dstDir, dstFile);
            if (!srcRelativePaths.Contains(relativePath))
            {
                File.Delete(dstFile);
                removed++;
            }
        }

        RemoveEmptyDirectories(dstDir);
    }

    /// <summary>
    /// Mirrors only the two Package Manager manifest files (not the full
    /// <c>Packages\</c> folder, which also contains every installed package's local
    /// cache) into the snapshot's <c>Packages\</c> subfolder.
    /// </summary>
    private static void MirrorPackagesManifests(string packagesSrc, string packagesDst, ref int copied, ref int removed)
    {
        Directory.CreateDirectory(packagesDst);

        foreach (string fileName in PackagesFilesToSnapshot)
        {
            string srcFile = Path.Combine(packagesSrc, fileName);
            string dstFile = Path.Combine(packagesDst, fileName);

            if (File.Exists(srcFile))
            {
                if (!FilesAreIdentical(srcFile, dstFile))
                {
                    File.Copy(srcFile, dstFile, overwrite: true);
                    copied++;
                }
            }
            else if (File.Exists(dstFile))
            {
                File.Delete(dstFile);
                removed++;
            }
        }
    }

    #endregion

    #region // Helpers

    /// <summary>Returns <paramref name="fullPath"/> relative to <paramref name="baseDir"/>, using forward slashes.</summary>
    private static string GetRelativePath(string baseDir, string fullPath)
    {
        string relative = Path.GetRelativePath(baseDir, fullPath);
        return relative.Replace('\\', '/');
    }

    /// <summary>Byte-for-byte comparison used to skip no-op copies and keep the summary count meaningful.</summary>
    private static bool FilesAreIdentical(string a, string b)
    {
        if (!File.Exists(b)) return false;

        byte[] bytesA = File.ReadAllBytes(a);
        byte[] bytesB = File.ReadAllBytes(b);
        return bytesA.SequenceEqual(bytesB);
    }

    /// <summary>Recursively deletes any now-empty subdirectories left behind after mirrored deletions.</summary>
    private static void RemoveEmptyDirectories(string dir)
    {
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            RemoveEmptyDirectories(subDir);
            if (Directory.GetFileSystemEntries(subDir).Length == 0)
            {
                Directory.Delete(subDir);
            }
        }
    }

    #endregion
}
