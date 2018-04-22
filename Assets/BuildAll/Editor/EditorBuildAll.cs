using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Threading;
using System;
using System.IO;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

public class EditorBuildAll : MonoBehaviour {

    const string BASE_PATH = @"D:\Builds";

    const string WINDOWS_BASE_PATH = BASE_PATH + @"\Windows";
    const string OSX_BASE_PATH = BASE_PATH + @"\OSX";
    const string LINUX_BASE_PATH = BASE_PATH + @"\Linux";
    const string UPLOAD_HELPER = "UnityMediaFireUploadHelper";
    static readonly string HELPER_PATH = Path.Combine(Application.dataPath.Replace("/", "\\"), "Helper~");


    static string currentBuild = "";
    static FastZip zipper;
    static object zipMutex;

    static EditorBuildAllWindow win;

    [MenuItem("Edit/Build All")]
    public static void BuildAll()
    {
        try
        {
            Debug.ClearDeveloperConsole();
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

            //Clear builds dir ( so Unity won't cry c: )
            try
            {
                if (Directory.Exists(BASE_PATH))    Directory.Delete(BASE_PATH, true);
                Directory.CreateDirectory(BASE_PATH);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] Error while clearing {BASE_PATH} : {e.Message}\nPlease restart Unity. If that does not fix the problem, then you're fucked :D");
                throw e;
            }
            zipMutex = new object();

            BuildPlayerOptions opt = new BuildPlayerOptions();
            opt.scenes = SceneManager.GetAllScenes().Select(t => t.path).ToArray();

            zipper = new FastZip();

            string lastErr = "";
            //win = EditorBuildAllWindow.OpenWindow();

            // WINDOWS
            currentBuild = "windows";

            opt.target = BuildTarget.StandaloneWindows;
            opt.locationPathName = WINDOWS_BASE_PATH + @"\player.exe";
            lastErr = BuildPipeline.BuildPlayer(opt);
            if (IsError(lastErr)) return;
            ZipInNewThread(WINDOWS_BASE_PATH + ".zip", WINDOWS_BASE_PATH, true, "", "Win", "/Windows.zip", LogPlatform.Windows).Join();

            // MAC
            currentBuild = "osx";

            opt.target = BuildTarget.StandaloneOSX;
            opt.locationPathName = OSX_BASE_PATH + ".app";
            lastErr = BuildPipeline.BuildPlayer(opt);
            if (IsError(lastErr)) return;
            ZipInNewThread(OSX_BASE_PATH + ".zip", OSX_BASE_PATH + ".app", true, "", "Mac", "/Mac.zip", LogPlatform.Mac);

            // LINUX
            currentBuild = "linux";

            opt.target = BuildTarget.StandaloneLinuxUniversal;
            opt.locationPathName = LINUX_BASE_PATH + @"\player.x86";
            lastErr = BuildPipeline.BuildPlayer(opt);
            if (IsError(lastErr)) return;
            Thread lin = ZipInNewThread(LINUX_BASE_PATH + ".zip", LINUX_BASE_PATH, true, "", "Lin", "/Linux.zip", LogPlatform.Linux);
            UpdateProgress(.5f, false, "Awaiting final zip for linux", "Waiting for completion");

            //Block the method until the last zip operation has completed (so Unity wont FUCK ME OVER!!! (fuk u unity))
            lin.Join();
            UpdateProgress(0, true);

            EditorUtility.DisplayDialog("Build Done!", "All builds have been completed", "OK");
        }
        finally
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        }
    }

    private static Process UploadFile(string fileName, string token, string bld, string dest, LogPlatform platform)
    {
        return SpawnHelper(HELPER_PATH, UPLOAD_HELPER, platform, "-build", bld, "-file", fileName, "-dest", dest);
    }

    private static Process SpawnHelper(string helperPath, string helperName, LogPlatform pl, params object[] args)
    {
        ProcessStartInfo ps = new ProcessStartInfo();
        ps.FileName = Path.Combine(helperPath, helperName + ".exe");
        ps.RedirectStandardOutput = true;
        ps.UseShellExecute = false;
        ps.CreateNoWindow = true;

        ps.Arguments = string.Join(" ", args);

        Process p = new Process();
        p.StartInfo = ps;

        p.OutputDataReceived += (sender, e) => { HelperDataHandler(sender, e, pl); };
        //p.Exited += (sender, e) => { Debug.Log("HELPER EXIT"); foreach (var s in p.StandardOutput.ReadToEnd().Split('\n')) { HelperDataHandler(null, s, pl); } EditorApplication.UnlockReloadAssemblies(); };
        p.Start();

        p.BeginOutputReadLine();
        return null;
    }

    private static void HelperDataHandler(object sender, DataReceivedEventArgs e, LogPlatform p)
    {
        win.LogLine(p, e.Data);
        Debug.Log(e.Data);
    }

    private static void HelperDataHandler(object sender, string e, LogPlatform p)
    {
        win.LogLine(p, e);
        Debug.Log(e);
    }

    private static Thread ZipInNewThread(string fileName, string fileDir, bool recurse, string filter, string build, string uploadFilename, LogPlatform platform)
    {
        return ThreadFactory.CreateAndRun(() => { ZipperThread(fileName, fileDir, recurse, filter, build, uploadFilename, platform); });
    }

    private static void ZipperThread(string fn, string df, bool r, string flt, string bld, string ufn, LogPlatform pl)
    {
        try
        {
            Debug.Log($"[{bld}] Starting zip for build");
            lock (zipMutex)
            {
                zipper.CreateZip(fn, df, r, flt);
                Debug.Log($"[{bld}] Zip finished for build");
            }
            Debug.Log($"[{bld}] Starting upload for build");
            //UploadFile(fn, "", bld, ufn, pl); //TODO: Make this shit work
            Debug.Log($"[{bld}] Upload started for build");
        }
        catch (ThreadAbortException e)
        {
            //Fuck you unity :P
            Debug.LogError($"[{bld}] Unity pls stop cancelling my GOD DAMNED THREADS YOU ASSHOLE : {e.ToString()}");

        }
        catch (Exception e)
        {
            Debug.LogError($"[{bld}] Error zipping file: {e.ToString()}");
        }
    }

    private static bool IsError(string err)
    { 
        if (string.IsNullOrEmpty(err)) return false;

        Debug.ClearDeveloperConsole();
        UpdateProgress(0, true);
        EditorUtility.DisplayDialog("Build Error", "A fatal error occurred while building the " + currentBuild + " build. Please see the console for more details", "OK", null);
        return true;
    }

    private static void UpdateProgress(float v, bool clr = false, string desc = "Compressing files", string title = "Compressing {0} build")
    {
        if (clr)
        {
            EditorUtility.ClearProgressBar();
            return;
        }
        EditorUtility.DisplayProgressBar(string.Format(title, currentBuild), desc, v);
    }
}

internal class ThreadFactory
{
    public static Thread CreateAndRun(ThreadStart thr)
    {
        Thread th = new Thread(thr);
        th.Start();
        return th;
    }
}
