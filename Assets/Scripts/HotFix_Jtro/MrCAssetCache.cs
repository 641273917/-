using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MrCAssetFramework
{
    internal sealed class MrCAssetCache                      //缓存管理
    {
        #region 包裹缓存机制
        //创建缓存字典
        private static Dictionary<string, MrCAssetBundle> assetBundleCache;
        //缓存字典属性
        internal static Dictionary<string, MrCAssetBundle> AssetBundleCache
        {
            get
            {
                if (assetBundleCache == null)
                {
                    assetBundleCache = new Dictionary<string, MrCAssetBundle>();
                }
                return assetBundleCache;
            }
        }

        //创建缓存WWW对象
        private static Dictionary<string, AssetBundleCreateRequest> wwwCache;
        //创建缓存WWW对象属性
        private static Dictionary<string, AssetBundleCreateRequest> WwwCache
        {
            get
            {
                if (wwwCache == null)
                {
                    wwwCache = new Dictionary<string, AssetBundleCreateRequest>();
                }
                return wwwCache;
            }
        }

        //创建依赖缓存对象
        private static Dictionary<string, string[]> dependCache;
        //创建依赖缓存属性
        private static Dictionary<string, string[]> DependCache
        {
            get
            {
                if (dependCache == null)
                {
                    dependCache = new Dictionary<string, string[]>();
                }
                return dependCache;
            }
        }


        /// <summary>
        /// Instantiate the Cache
        /// </summary>
        /// <param name="assetbundleName"></param>
        /// <returns></returns>
        internal static bool InCache(string assetbundleName)
        {
            return AssetBundleCache.ContainsKey(assetbundleName);
        }
        #endregion



        #region 卸载系列函数

        /// <summary>
        /// 卸载资源包和依赖包
        /// </summary>
        /// <param name="assetBundleName"></param>
        public static void UnloadAssetBundle(string assetBundleName)
        {
            UnloadAssetBundleInternal(assetBundleName);
            UnloadDependencies(assetBundleName);
        }

        internal static void UnloadDependencies(string assetBundleName)
        {
            string[] depends = null;

            //获取所有的依赖包名称
            if (!DependCache.TryGetValue(assetBundleName, out depends))
                return;

            //卸载依赖包
            foreach (var dependency in depends)
            {
                UnloadAssetBundleInternal(dependency);
            }
            //删除依赖缓存策略
            DependCache.Remove(assetBundleName);
        }

        internal static void UnloadAssetBundleInternal(string assetBundleName)
        {
            MrCAssetBundle bundle;
            AssetBundleCache.TryGetValue(assetBundleName, out bundle);

            if (bundle == null)
                return;
            bundle.Release();
        }
        #endregion

        #region GetFunction
        internal static AssetBundleCreateRequest GetWWWCache(string key)
        {
            if (WwwCache.ContainsKey(key))
            {
                return WwwCache[key];
            }
            return null;
        }
        internal static MrCAssetBundle SetWWWCache(string key, AssetBundleCreateRequest value)
        {
            if (!WwwCache.ContainsKey(key))
                WwwCache.Add(key, value);
            else
                WwwCache[key] = value;
            var bundleObject = new MrCAssetBundle(key);
            SetBundleCache(key, bundleObject);
            return bundleObject;
        }

        internal static MrCAssetBundle GetBundleCache(string key)
        {
            MrCAssetBundle ab;
            AssetBundleCache.TryGetValue(key, out ab);
            return ab;
        }

        internal static void SetBundleCache(string key, MrCAssetBundle value)
        {
            if (!AssetBundleCache.ContainsKey(key))
            {
                AssetBundleCache.Add(key, value);
            }
            else
            {
                AssetBundleCache[key] = value;
            }
        }

        internal static string[] GetDependCache(string key)
        {
            string[] depends;
            DependCache.TryGetValue(key, out depends);
            return depends;
        }
        internal static void SetDependCache(string key, string[] value)
        {
            if (!DependCache.ContainsKey(key))
            {
                DependCache.Add(key, value);
            }
        }
        #endregion

        internal static void FreeBundle(string key)
        {
            if (AssetBundleCache.ContainsKey(key))
                AssetBundleCache.Remove(key);
        }

        #region Update
        private static List<string> keysToRemove = new List<string>();

        internal static void Update()
        {
            foreach (var keyValue in WwwCache)
            {
                var download = keyValue.Value;
                string bundleName = keyValue.Key;
                var bundleObject = GetBundleCache(bundleName);
                if (bundleObject == null)
                {
                    bundleObject = new MrCAssetBundle(bundleName);
                    SetBundleCache(bundleName, bundleObject);
                }
                bundleObject.Progress = download.progress;

                //下载成功
                if (download.isDone)
                {
                    bundleObject.AssetBundle = download.assetBundle;
                    bundleObject.IsDone = true;
                    bundleObject.Progress = 1.1f;
                    keysToRemove.Add(bundleName);
                }
            }
            //删除下载成功的WWW对象
            foreach (var key in keysToRemove)
            {
                if (wwwCache.ContainsKey(key))
                {
                    var download = WwwCache[key];
                    WwwCache.Remove(key);
                }
            }
            keysToRemove.Clear();
        }
        #endregion
    }
} 