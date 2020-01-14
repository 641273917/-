using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MrCAssetFramework
{
    //AssetBundle管理
    public class MrCAssetManager : MonoBehaviour
    {
        #region Singleton
        private static MrCAssetManager _instance = null;

        public static MrCAssetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("LoadAssetManager");
                    _instance = obj.AddComponent<MrCAssetManager>();
                }
                return _instance;
            }
        }

        #endregion

        //最新生成的bundle
        public MrCAssetBundle NewAssetBundle { get; set; }

        public void Init()
        {
            StartCoroutine(LoadManifestBundle());
        }
        // 异步加载场景
        public IEnumerator LoadLevelAsync(string assetBundleName, string assetName)
        {
            var bundleObject = MrCAssetCache.GetBundleCache(assetBundleName);
            //缓存中已经存在请求bundle，中止..
            if (bundleObject == null)
            {
                //通过网络下载AssetBundle
                bundleObject = LoadAssetBundleAtInternal(assetBundleName);
                yield return LoadAssetBundleProgress(bundleObject);
                //yield return GetAssetAsyc(assetBundleName, assetName);
                //StartCoroutine(LoadDependencies(assetBundleName));
            }
            else
            {
                bundleObject.RetainCall();
            }
        }

        private void Update()
        {
            MrCAssetCache.Update();
        }

        public IEnumerator LoadManifestBundle()
        {
            var manifestName = PathConfig.GetManifestFileName();

            var bundleObject = MrCAssetCache.GetBundleCache(manifestName);
            //缓存中已经存在请求bundle，中止..
            if (bundleObject == null)
            {
                //通过网络下载AssetBundle
                bundleObject = LoadAssetBundleAtInternal(manifestName);
                yield return LoadAssetBundleProgress(bundleObject);
                //StartCoroutine(GetAssetAsyc<AssetBundleManifest>(manifestName, "AssetBundleManifest"));
            }
            else
            {
                bundleObject.RetainCall();
            }
        }

        //private IEnumerator GetAssetAsyc(string assetbundleName, string assetName)
        //{
        //    MrCAssetBundle lab = MrCAssetCache.GetBundleCache(assetbundleName);
        //    if (lab != null)
        //    {
        //        var ab = lab.AssetBundle.LoadAssetAsync(assetName);
        //        ab.allowSceneActivation = false;
        //        //当前进度
        //        int currentProgress = 0;
        //        //目标进度
        //        int targetProgress = 1000;
        //        while (true)
        //        {
        //            if (ab.isDone || currentProgress++ > targetProgress)
        //            {
        //                print("加载场景");
        //                break;
        //            }
        //            DownLoadProgress.Instance.Progress = ab.progress;
        //            yield return null;
        //        }
        //        ab.allowSceneActivation = true;
        //    }
        //    else
        //    {
        //        print("资源: " + assetbundleName + " 未加载或正在加载!");
        //        yield break;
        //    }
        //}

        #region 加载包裹系列函数
        ///检查是否已经从网络下载
        protected MrCAssetBundle LoadAssetBundleAtInternal(string assetBundleName)
        {
            var bundleObject = MrCAssetCache.GetBundleCache(assetBundleName);
            //如果WWW缓存策略中包含有对应的关键字，则返回true
            if (bundleObject == null)
            {
                MustBundleHandle();
                var url = PathConfig.localUrl + "/"
                    + PathConfig.GetManifestFileName() + "/"
                    + assetBundleName;
                //创建下载链接
                var request = AssetBundle.LoadFromFileAsync(url);
                //WWW www = new WWW(url); 
                //+ assetBundleName
                //按版本号，按需要通过网络下载AssetBundle，一般在正式游戏版本中，不使用上面的，因为它会每次打开游戏重新下载
                //WWW www = WWW.LoadFromCacheOrDownload(LOAssetManager.URI + assetBundleName, nowVersion);
                //加入缓存策略
                NewAssetBundle = MrCAssetCache.SetWWWCache(assetBundleName, request);
                return NewAssetBundle;
            }
            return bundleObject;
        }

        //超过最大bunld数处理
        private void MustBundleHandle()
        {
            var bundles = MrCAssetCache.AssetBundleCache;
            if (bundles != null && bundles.Count >= PathConfig.MAXBUNDLECOUNT)
            {
                int min = int.MaxValue;
                string findKey = string.Empty;
                foreach (var item in bundles.Values)
                {
                    if (item.RetainCount() < min)
                    {
                        min = item.RetainCount();
                        findKey = item.m_AssetBundleName;
                    }
                }
                var bundle = MrCAssetCache.GetBundleCache(findKey);
                if (bundle != null) bundle.Release();
            }
        }

        private IEnumerator LoadDependencies(string assetBundleName)
        {
            var manifestName = PathConfig.GetManifestFileName();
            var manifest = this.GetAsset<AssetBundleManifest>(manifestName, "AssetBundleManifest");
            if (manifest == null)
            {
                if (MrCAssetCache.InCache(manifestName))
                {
                    manifest = this.GetAsset<AssetBundleManifest>(manifestName, "AssetBundleManifest");
                }
                else
                    yield return null;
            }
            //获取依赖包裹
            string[] depends = manifest.GetAllDependencies(assetBundleName);

            if (depends.Length == 0)
            {
                yield return null;
            }

            //记录并且加载所有的依赖包裹
            MrCAssetCache.SetDependCache(assetBundleName, depends);

            for (int i = 0; i < depends.Length; i++)
            {
                yield return LoadAssetBundleAtInternal(depends[i]);
            }
        }

        // To Get The Asset...
        private T GetAsset<T>(string assetbundleName, string assetName) where T : UnityEngine.Object
        {
            MrCAssetBundle lab = MrCAssetCache.GetBundleCache(assetbundleName);
            if (lab != null)
            {
                return lab.AssetBundle.LoadAsset<T>(assetName);
            }
            else
            {
                print("资源: " + assetbundleName + " 未加载或正在加载!");
                return null;
            }
        }
        #endregion

        #region  Loading阶段
        private IEnumerator LoadAssetBundleProgress(MrCAssetBundle _bundleObject)
        {
            TestManager.Instance.IsBundleSuccess = true;
            if (_bundleObject == null)
            {
                TestManager.Instance.IsBundleSuccess = false;
                yield break;
            }
            //当前进度
            //int initNum = 0;
            //目标进度
            //int maxNum = 2000;
            while (!_bundleObject.IsDone)
            {
                //if (initNum++ > maxNum)
                //    break;
                //var progress = _bundleObject.Progress;
                //sliderImage.rectTransform.sizeDelta = new Vector2(700f * progress, 12);
                //progressText.text = string.Format("{0:f1}{1}", progress * 100, '%');
                yield return new WaitForEndOfFrame();
            }
            TestManager.Instance.IsBundleSuccess = _bundleObject.IsDone;
        }
        #endregion
    }
} 