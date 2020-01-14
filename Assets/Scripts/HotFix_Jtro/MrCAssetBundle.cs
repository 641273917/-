using UnityEngine;
namespace MrCAssetFramework
{
    public sealed class MrCAssetBundle
    {
        internal string m_AssetBundleName;

        public AssetBundle AssetBundle { get; set; }

        public float Progress { get; set; }

        public bool IsDone { get; set; }

        internal MrCAssetBundle(string assetBundleName)
        {
            this.m_AssetBundleName = assetBundleName;
            this.m_ReferencedCount = 1;
        }
        private int m_ReferencedCount;
        //保留调用次数
        public void RetainCall()
        {
            this.m_ReferencedCount++;
        }
        //卸载资源
        public void Release()
        {
            //this.m_ReferencedCount--;
            ////当引用计数为0时，卸载资源
            //if (this.m_ReferencedCount == 0)
            //{
            //    if(AssetBundle!=null)
            //        this.AssetBundle.Unload(true);
            //    MrCAssetCache.FreeBundle(this.m_AssetBundleName);
            //}
            if (AssetBundle != null)
                this.AssetBundle.Unload(true);
            MrCAssetCache.FreeBundle(this.m_AssetBundleName);
            Debug.Log("卸载资源: " + m_AssetBundleName);
        }
        //如果是新创建的，不首先消毁
        public int RetainCount()
        {
            var newbundle = MrCAssetManager.Instance.NewAssetBundle;
            if (newbundle != null && newbundle == this)
            {
                return 65535;
            }
            return m_ReferencedCount;
        }
    }
} 