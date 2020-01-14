using System;
using System.IO;
using System.Security.Cryptography;

public class MD5Checker
{
    public delegate void AsyncCheckHeadler(AsyncCheckEventArgs e);
    public event AsyncCheckHeadler AsyncCheckProgress;
    //支持所有哈希算法
    private HashAlgorithm hashAlgorithm;
    //文件读取流
    private Stream inputStream;
    //缓存
    private byte[] asyncBuffer;

    public AsyncCheckState CompleteState { get; private set; }
    public float Progress { get; private set; }
    public string GetMD5 { get; private set; }

    /// <summary>
    /// 返回指定文件的MD5值
    /// </summary>
    /// <param name="path">文件的路径</param>
    /// <returns></returns>
    public static string Check(string path)
    {
        try
        {
            var fs = new FileStream(path, FileMode.Open);
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            byte[] buffer = md5Provider.ComputeHash(fs);
            string resule = BitConverter.ToString(buffer);
            resule = resule.Replace("-", "");
            fs.Close();
            return resule;
        }
        catch (ArgumentException aex)
        {
            throw new ArgumentException(string.Format("<{0}>, 不存在: {1}", path, aex.Message));
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("读取文件 {0} ,MD5失败: {1}", path, ex.Message));
        }
    }

    public static string Check_Stream(string path)
    {
        try
        {
            int bufferSize = 1024 * 256;//自定义缓冲区大小256K
            var buffer = new byte[bufferSize];
            Stream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            HashAlgorithm hashAlgorithm = new MD5CryptoServiceProvider();
            int readLength = 0;//每次读取长度
            var output = new byte[bufferSize];
            while ((readLength = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                //计算MD5
                hashAlgorithm.TransformBlock(buffer, 0, readLength, output, 0);
            }
            //完成最后计算，必须调用(由于上一部循环已经完成所有运算，所以调用此方法时后面的两个参数都为0)
            hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
            string md5 = BitConverter.ToString(hashAlgorithm.Hash);
            hashAlgorithm.Clear();
            inputStream.Close();
            md5 = md5.Replace("-", "");
            return md5;
        }
        catch (ArgumentException aex)
        {
            throw new ArgumentException(string.Format("<{0}>, 不存在: {1}", path, aex.Message));
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("读取文件 {0} ,MD5失败: {1}", path, ex.Message));
        }
    }

    public void AsyncCheck(string path)
    {
        CompleteState = AsyncCheckState.Checking;
        try
        {
            int bufferSize = 1024 * 256;//缓冲区大小，1MB 1048576

            asyncBuffer = new byte[bufferSize];

            //打开文件流
            inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, true);
            hashAlgorithm = new MD5CryptoServiceProvider();

            //异步读取数据到缓冲区
            inputStream.BeginRead(asyncBuffer, 0, asyncBuffer.Length, new AsyncCallback(AsyncComputeHashCallback), null);
        }
        catch (ArgumentException aex)
        {
            throw new ArgumentException(string.Format("<{0}>, 不存在: {1}", path, aex.Message));
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("读取文件{0} ,MD5失败: {1}", path, ex.Message));
        }
    }

    private void AsyncComputeHashCallback(IAsyncResult result)
    {
        int bytesRead = inputStream.EndRead(result);
        //检查是否到达流末尾
        if (inputStream.Position < inputStream.Length)
        {
            //输出进度
            Progress = (float)inputStream.Position / inputStream.Length;
            string pro = string.Format("{0:P0}", Progress);

            if (null != AsyncCheckProgress)
                AsyncCheckProgress(new AsyncCheckEventArgs(AsyncCheckState.Checking, pro));

            var output = new byte[asyncBuffer.Length];
            //分块计算哈希值
            hashAlgorithm.TransformBlock(asyncBuffer, 0, asyncBuffer.Length, output, 0);

            //异步读取下一分块
            inputStream.BeginRead(asyncBuffer, 0, asyncBuffer.Length, new AsyncCallback(AsyncComputeHashCallback), null);
            return;
        }
        else
        {
            //计算最后分块哈希值
            hashAlgorithm.TransformFinalBlock(asyncBuffer, 0, bytesRead);
        }

        Progress = 1;
        string md5 = BitConverter.ToString(hashAlgorithm.Hash).Replace("-", "");
        CompleteState = AsyncCheckState.Completed;
        GetMD5 = md5;
        if (null != AsyncCheckProgress)
            AsyncCheckProgress(new AsyncCheckEventArgs(AsyncCheckState.Completed, GetMD5));

        inputStream.Close();
    }
}

public enum AsyncCheckState
{
    Completed,
    Checking
}

public class AsyncCheckEventArgs : EventArgs
{
    public string Value { get; private set; }

    public AsyncCheckState State { get; private set; }

    public AsyncCheckEventArgs(AsyncCheckState state, string value)
    {
        this.Value = value; this.State = state;
    }
}