using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CreatAssetBundle:EditorWindow  {
    /// <summary>
    /// 构建的方法
    /// </summary>
    /// <param name="target">构建的平台</param>
    /// <returns></returns>
    public static string GetAssetBundlePath(BuildTarget target )
    {
        var path = PathConfig.buildAssetPath + "/" + PathConfig.GetBuildTargetPath(target) + "/";
        //当在硬盘目录结构里不存在该路径时,创建文件夹
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
    [MenuItem("构建AB/Build Windows")]
    public static void CustomBuildAssetBundle_Win()
    {
        BuildBundle(BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }

    [MenuItem("构建AB/Build IOS")]
    public static void CustomBuildAssetBundle_IOS()
    {
        BuildBundle(BuildAssetBundleOptions.None, BuildTarget.iOS);
    }

    [MenuItem("构建AB/Build MAC")]
    public static void CustomBuildAssetBundle_MAC()
    {
        BuildBundle(BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
    }

    [MenuItem("构建AB/Build Android")]
    public static void CustomBuildAssetBundle_Android()
    {
        BuildBundle(BuildAssetBundleOptions.None, BuildTarget.Android);
    }

    [MenuItem("构建AB/Build WebGL")]
    public static void CustomBuildAssetBundle_WebGL()
    {
        BuildBundle(BuildAssetBundleOptions.None, BuildTarget.WebGL);
    }
    private static void BuildBundle(BuildAssetBundleOptions bundleOptions, BuildTarget buildTarget)
    {
        //设置资源读取版本号 

        //包裹存储的路径...
        string outputPath = GetAssetBundlePath(EditorUserBuildSettings.activeBuildTarget);
        Debug.Log(outputPath);
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
        //打包过程..
        BuildPipeline.BuildAssetBundles(outputPath, bundleOptions, buildTarget);
        CreateVersion(outputPath);
        Debug.Log("打包完成!位置: " + outputPath);

    }
    /// <summary>
    /// 创建vision文件
    /// </summary>
    /// <param name="resPath"></param>
    public static void CreateVersion(string resPath)
    {
        // 获取Res文件夹下所有文件的相对路径和MD5值
        string[] files = Directory.GetFiles(resPath, "*", SearchOption.AllDirectories);

        StringBuilder versions = new StringBuilder();
        for (int i = 0, len = files.Length; i < len; i++)
        {
            string filePath = files[i];

            if (filePath.Contains("."))
            {
                string extension = filePath.Substring(files[i].LastIndexOf("."));
                if (extension == ".unity3d")
                {
                    string relativePath = filePath.Replace(resPath, "").Replace("\\", "/");
                    string md5 = PathConfig.MD5File(filePath);
                    versions.Append(relativePath).Append(",").Append(md5).Append("\r\n");
                }
            }
            else
            {
                string test = filePath.Substring(files[i].LastIndexOf("/") + 1);
                if (test == PathConfig.GetBuildTargetPath(EditorUserBuildSettings.activeBuildTarget))
                {
                    string relativePath = filePath.Replace(resPath, "").Replace("\\", "/");
                    string md5 = PathConfig.MD5File(filePath);
                    versions.Append(relativePath).Append(",").Append(md5).Append("\r\n");
                }
            }
        }

        // 生成配置文件
        FileStream stream = new FileStream(resPath + PathConfig.version_file, FileMode.Create);

        byte[] data = Encoding.UTF8.GetBytes(versions.ToString());
        stream.Write(data, 0, data.Length);
        stream.Flush();
        stream.Close();
    } 
}
