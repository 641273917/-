using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class UpdateAssets : MonoBehaviour
{
    //保存本地的assetbundle名和对应的MD5值
    private Dictionary<string, string> LocalResVersion;
    private Dictionary<string, string> ServerResVersion;

    //保存需要更新的AssetBundle名
    private List<string> NeedDownFiles;

    private bool NeedUpdateLocalVersionFile = false;

    private string _localUrl;
    private string _serverUrl;

    public IEnumerator OnStart()
    {
        yield return Init();
    }

    private IEnumerator Init()
    {
        LocalResVersion = new Dictionary<string, string>();
        ServerResVersion = new Dictionary<string, string>();
        NeedDownFiles = new List<string>();

        //加载本地version配置
        _localUrl = PathConfig.localUrl + "/" + PathConfig.GetManifestFileName() + "/";
        yield return DownLoad(PathConfig.GetFileHeader + _localUrl, PathConfig.version_file, LocalVersionCallBack);


        //加载服务端version配置
        var serverUrl = PathConfig.serverUrl;
        _serverUrl = serverUrl + PathConfig.GetManifestFileName() + "/";
        Debug.Log("下载文件服务器的地址: " + _serverUrl);
        yield return DownLoad(_serverUrl, PathConfig.version_file, ServerVersionCallBack);
    }
    private IEnumerator LocalVersionCallBack(UnityWebRequest request, string param = "")
    {
        //保存本地的version
        var context = request.downloadHandler.text;
        ParseVersionFile(context, LocalResVersion);

        yield return ClearIncompleteFile();
    }

    private IEnumerator ServerVersionCallBack(UnityWebRequest request, string param = "")
    {
        //保存服务端version
        var context = request.downloadHandler.text;
        ParseVersionFile(context, ServerResVersion);

        //根据用户名过滤下载资源
        //FilterServerDownLoadFile();

        //计算出需要重新加载的资源
        CompareVersion();
        if (NeedUpdateLocalVersionFile)
        {
            //DownLoadProgress.Instance.ShowDownLoad(NeedDownFiles.Count);
            Debug.Log("需要更新的资源个数为：" + NeedDownFiles.Count);
        }
        //加载需要更新的资源
        yield return DownLoadRes();
    }

    //对比本地配置，清除缺失文件
    private IEnumerator ClearIncompleteFile()
    {
        if (LocalResVersion != null)
        {
            List<string> removeKey = new List<string>();
            foreach (var local in LocalResVersion)
            {
                string filePath = _localUrl + local.Key;
                if (!Directory.Exists(_localUrl))
                {
                    Directory.CreateDirectory(_localUrl);
                }
                if (!File.Exists(filePath))
                {
                    removeKey.Add(local.Key);
                }
                else
                {
                    //异步
                    yield return MD5FileAsync(filePath, delegate (string md5)
                    {
                        if (md5 != local.Value)
                        {
                            File.Delete(filePath);
                            removeKey.Add(local.Key);
                        }
                    });

                }
            }
            foreach (var key in removeKey)
            {
                if (LocalResVersion.ContainsKey(key))
                    LocalResVersion.Remove(key);
            }
        }
    }

    //过滤服务器下载文件，根据用户可访问项目
    //private void FilterServerDownLoadFile()
    //{
    //    var tabdatas = DataManager.Instance.projectTabDatas;
    //    if (tabdatas == null)
    //        return;
    //    var localSet = ProjectSetting.Instance.projectSetData;
    //    if (localSet != null && localSet.subProjectInfo != null)
    //    {
    //        //获取所选项目所有Id
    //        var allProjectIds = tabdatas
    //            .Where(t => t.project_list != null && t.project_list.Count > 0)
    //            .SelectMany(t => t.project_list)
    //            .Where(t => t != null)
    //            .Select(t => t.Id.ToString())
    //            .ToList();
    //        //获取所有需要加载场景的项目Id
    //        var localProjectIds = localSet.subProjectInfo.SelectMany(t => t.sceneDatas.Select(f => f.Key));
    //        //根据所选项目，推出所有需要下载的Id (Intersect交集)
    //        var needDownloadIds = allProjectIds.Intersect(localProjectIds);
    //        var needDownloadScenes = localSet.subProjectInfo
    //            .SelectMany(t => t.sceneDatas
    //                .Where(f => needDownloadIds.Contains(f.Key))
    //                .Select(s => (s.Value + PathConfig.bundleSuffix))
    //                .ToList());
    //        //从而得出需要过滤的Id(Except获取不在list2中所有list1中元素)
    //        var filterIds = ServerResVersion.Select(t => t.Key).Except(needDownloadScenes).ToList();
    //        for (int i = 0; i < filterIds.Count; i++)
    //        {
    //            if (ServerResVersion.ContainsKey(filterIds[i]))
    //            {
    //                ServerResVersion.Remove(filterIds[i]);
    //            }
    //        }
    //        //测试-----------------------
    //        //string message = string.Empty;
    //        //foreach (var item in allProjectIds)
    //        //{
    //        //    message += item + " ,";
    //        //}
    //        //print("用户所有Ids: " + message);

    //        //message = string.Empty;
    //        //foreach (var item in localProjectIds)
    //        //{
    //        //    message += item + " ,";
    //        //}
    //        //print("本地所有需要加载Ids: " + message);

    //        //message = string.Empty;
    //        //foreach (var item in needDownloadIds)
    //        //{
    //        //    message += item + " ,";
    //        //}
    //        //print("需要加载Ids: " + message);

    //        //message = string.Empty;
    //        //foreach (var item in needDownloadScenes)
    //        //{
    //        //    message += item + " ,";
    //        //}
    //        //print("需要加载Scenes: " + message);

    //        //message = string.Empty;
    //        //foreach (var item in filterIds)
    //        //{
    //        //    message += item + " ,";
    //        //}
    //        //print("过滤Ids: " + message);
    //    }
    //}

    //依次加载需要更新的资源

    private IEnumerator DownLoadRes()
    {
        if (NeedDownFiles.Count == 0)
        {
            UpdateLocalVersionFile();
            yield break;
        }

        string file = NeedDownFiles[0];

        NeedDownFiles.RemoveAt(0);
        yield return DownLoad(_serverUrl, file, DownLoadCallBack);
    }

    private IEnumerator DownLoadCallBack(UnityWebRequest request, string param = "")
    {
        //将下载的资源替换本地就的资源
        var download = request.downloadHandler;
        if (!request.isNetworkError && !request.isHttpError)
        {
            ReplaceLocalRes(param, download.data);
            if (ServerResVersion.ContainsKey(param))
            {
                if (LocalResVersion.ContainsKey(param))
                    LocalResVersion[param] = ServerResVersion[param];
                else
                    LocalResVersion.Add(param, ServerResVersion[param]);
            }
        }
        yield return DownLoadRes();
    }


    private void ReplaceLocalRes(string fileName, byte[] data)
    {
        string filePath = _localUrl + fileName;

        if (!Directory.Exists(_localUrl))
        {
            Directory.CreateDirectory(_localUrl);
        }
        FileStream stream = new FileStream(filePath, FileMode.Create);
        stream.Write(data, 0, data.Length);
        stream.Flush();
        stream.Close();
    }


    //更新本地的version配置

    private void UpdateLocalVersionFile()
    {
        if (NeedUpdateLocalVersionFile)
        {
            StringBuilder versions = new StringBuilder();
            foreach (var item in LocalResVersion)
            {
                versions.Append(item.Key).Append(",").Append(item.Value).Append("\r\n");
            }
            if (!Directory.Exists(_localUrl))
            {
                Directory.CreateDirectory(_localUrl);
            }
            FileStream stream = new FileStream(_localUrl + PathConfig.version_file, FileMode.Create);
            byte[] data = Encoding.UTF8.GetBytes(versions.ToString());
            stream.Write(data, 0, data.Length);
            stream.Flush();
            stream.Close();
        }

        //加载显示对象
        //StartCoroutine(Show());
    }


    private void CompareVersion()
    {
        foreach (var version in ServerResVersion)
        {
            string fileName = version.Key;                 //assetbundleName
            string serverMd5 = version.Value;              // asset MD5值

            //新增的资源
            if (!LocalResVersion.ContainsKey(fileName))
            {
                NeedDownFiles.Add(fileName);
            }
            else
            {
                //需要替换的资源
                string localMd5;

                LocalResVersion.TryGetValue(fileName, out localMd5);
                if (!serverMd5.Equals(localMd5))
                {
                    NeedDownFiles.Add(fileName);
                }
            }
        }

        if (NeedDownFiles.Count > 0)
        {
            for (int i = 0; i < NeedDownFiles.Count; i++)
            {
                Debug.Log("需要更新的资源：" + NeedDownFiles[i]);
            }
        }
        //本次有更新，同时更新本地的version.txt
        NeedUpdateLocalVersionFile = NeedDownFiles.Count > 0;
    }


    private void ParseVersionFile(string content, Dictionary<string, string> dict)
    {
        if (content == null || content.Length == 0)
        {
            return;
        }

        string[] items = content.Split('\n');
        foreach (string item in items)
        {
            string str = item.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            string[] info = str.Split(',');
            if (info != null && info.Length == 2)
            {
                dict.Add(info[0], info[1]);
            }
        }
    }

    private IEnumerator DownLoad(string url, string fileName, HandleFinishDownload finishFun)
    {
        url = PathConfig.CheckUrl(url);
        var request = UnityWebRequest.Get(url + fileName);
        if (NeedUpdateLocalVersionFile)
        {
            yield return LoadRegularRequest(request);
        }
        else
        {
            yield return request.SendWebRequest();
        }

        if (finishFun != null && request.isDone)
        {
            yield return finishFun(request, fileName);
        }
        if (request.isNetworkError)
        {
            Debug.LogError("更新资源出错: " + url + " error: " + request.error);
        }
        request.Dispose();
    }

    public delegate IEnumerator HandleFinishDownload(UnityWebRequest request, string param = "");


    //异步生成MD5值
    private IEnumerator MD5FileAsync(string file, Action<string> action)
    {
        var asyncChecker = new MD5Checker();
        asyncChecker.AsyncCheck(file);
        var endframe = new WaitForEndOfFrame();
        while (asyncChecker.CompleteState == AsyncCheckState.Checking)
        {
            //SeerLogger.Log("load...{0:P0}" + asyncChecker.Progress);
            yield return endframe;
        }
        action(asyncChecker.GetMD5);
    }

    //整齐的下载资源
    public IEnumerator LoadRegularRequest(UnityEngine.Networking.UnityWebRequest request)
    {
        var ao = request.SendWebRequest();
        bool downError = false;
        //ao.allowSceneActivation = false;
        while (true)
        {
            if (downError) break;

            if (ao.webRequest.isNetworkError || ao.webRequest.isHttpError)
            {
                downError = true;
            }
            else if (ao.isDone)
                break;
        }
        yield return new WaitForEndOfFrame();
    }
    //ao.allowSceneActivation = true;

}