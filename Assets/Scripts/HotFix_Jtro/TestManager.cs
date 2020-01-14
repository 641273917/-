using MrCAssetFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestManager : MonoBehaviour
{

    private static TestManager _instance;

    public UpdateAssets updateAssets;



    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public static TestManager Instance
    {
        get { return _instance; }
    }

    public bool IsBundleSuccess { get; set; }
    /// <summary>
    /// 加载场景
    /// </summary>
    private void Start()
    {
        StartCoroutine(InitUpdate(LoadScene));
    }

    //登陆时首先需要进行资源比对，看是否需要重新下载
    private IEnumerator InitUpdate(System.Action action)
    {
        if (updateAssets)
        {
            yield return updateAssets.OnStart();
        }
        action?.Invoke();

    }

    public void LoadScene()
    {
        //MainScene   测试场景的bundle名称，可替换成你所需加载的场景名

        StartCoroutine(LoadSceneCoroutine("MainScene", null));
    }

    /// <summary>
    /// 异步解压bundle，加载场景
    /// </summary>
    /// <param name="sceneName">要加载登陆的场景打包的名字</param>
    /// <param name="action"></param>
    /// <returns></returns>
    private IEnumerator LoadSceneCoroutine(string sceneName, System.Action action)
    {
        print("开始加载场景.............");
        // yield return MrCAssetManager.Instance.LoadManifestBundle();
        yield return MrCAssetManager.Instance.LoadLevelAsync(sceneName + ".unity3d", sceneName);
        if (IsBundleSuccess)
        {
            action?.Invoke(); 
            LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("场景加载出现了问题!");
        }
    }

    private void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAsync(sceneName));
    }
    //异步对象  
    private AsyncOperation _asyncOperation;
    /// <summary>
    /// 携程进行异步加载场景
    /// </summary>
    /// <param name="sceneName">需要加载的场景名</param>
    /// <returns></returns>
    IEnumerator LoadAsync(string sceneName)
    {
        //当前进度
        float currentProgress = 0;
        //目标进度
        float targetProgress = 0;
        float speed = 0.02f;
        _asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        if (_asyncOperation == null)
        {
            Debug.LogError("没有找到场景: " + sceneName);
            yield break;
        }
        //unity 加载90%
        _asyncOperation.allowSceneActivation = false;
        while (_asyncOperation.progress < 0.9f)
        {
            targetProgress = _asyncOperation.progress;
            //平滑过渡
            while (currentProgress < targetProgress)
            {
                //progressText.text = string.Format("{0:f0}{1}", currentProgress * 100, "%");
                //sliderImage.rectTransform.sizeDelta = new Vector2(704f * currentProgress, 12);
                yield return null;
                currentProgress += speed;
            }
            yield return null;
        }
        //自行加载剩余的10%
        targetProgress = 1f;
        while (currentProgress < targetProgress)
        {
            //progressText.text = string.Format("{0:f0}{1}", currentProgress * 100, "%");
            //sliderImage.GetComponent<RectTransform>().sizeDelta = new Vector2(704f * currentProgress, 12);
            yield return null;
            currentProgress += speed;
        }
        _asyncOperation.allowSceneActivation = true;
        currentProgress = 1;
        //progressText.text = string.Format("{0:f0}{1}", currentProgress * 100, "%");
        //sliderImage.GetComponent<RectTransform>().sizeDelta = new Vector2(704f * currentProgress, 12);
        //等待一会等场景中资源加载一会儿再显示(设置0.5f切换到登录界面会有显示问题)
        yield return new WaitForSeconds(1f); //(0.5f);//(1f);

    }
}
