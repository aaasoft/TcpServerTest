using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpServerTest.Core.Utils;

namespace TcpServerTest.Core
{
    public class TcpServerContext
    {
        private CancellationTokenSource cts;
        private TcpListener listener;
        private TcpServerContextOptions options;
        private long connectCount = 0;
        private long disConnectCount = 0;
        private long recvCount = 0;
        private byte[] buffer = new byte[1024];

        public TcpServerContext(TcpServerContextOptions options)
        {
            this.options = options;
        }

        public void Start()
        {
            try
            {
                cts = new CancellationTokenSource();
                connectCount = 0;
                disConnectCount = 0;
                recvCount = 0;

                options.ConnectCountHandler.Invoke(connectCount);
                options.DisConnectCountHandler.Invoke(disConnectCount);
                options.RecvCountHandler.Invoke(recvCount);

                listener = new TcpListener(IPAddress.Any, options.Port);
                listener.Start();
                options.StatusHandler.Invoke("开始监听");
                beginAcceptTcpClient(cts.Token);
            }
            catch (Exception ex)
            {
                options.StatusHandler.Invoke("监听出错：" + ex.Message);
            }
        }

        private void beginAcceptTcpClient(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            listener?.AcceptTcpClientAsync().ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                    return;
                Interlocked.Increment(ref connectCount);
                options.ConnectCountHandler.Invoke(connectCount);
                var tcpClient = t.Result;
                if (options.RecvTimeout > 0)
                {
                    tcpClient.ReceiveTimeout = options.RecvTimeout;
                    tcpClient.GetStream().ReadTimeout = options.RecvTimeout;
                }
                beginRead(tcpClient, token);
                beginAcceptTcpClient(token);
            });
        }

        private void onDisconnect(TcpClient tcpClient)
        {
            Interlocked.Increment(ref disConnectCount);
            options.DisConnectCountHandler.Invoke(disConnectCount);
            try
            {
                tcpClient.Close();
                tcpClient.Dispose();
            }
            catch { }
        }

        private void beginRead(TcpClient tcpClient, CancellationToken token)
        {
            var readTask = tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length, token);
            Action<Task<int>> afterReadTaskAction = t =>
             {
                 var readCount = 0;
                 bool isDisconnected = false;
                 if (t.IsCanceled || t.IsFaulted)
                 {
                     isDisconnected = true;
                 }
                 else
                 {
                     readCount = t.Result;
                     if (readCount == 0)
                         isDisconnected = true;
                 }
                 if (isDisconnected)
                 {
                     onDisconnect(tcpClient);
                     return;
                 }
                 Interlocked.Add(ref recvCount, readCount);
                 options.RecvCountHandler.Invoke(recvCount);
                 beginRead(tcpClient, token);
             };


            if (options.RecvTimeout > 0)
            {
                TaskUtils.TaskWait(readTask, options.RecvTimeout).ContinueWith(t =>
                {
                    if (t.IsCanceled || t.IsFaulted)
                    {
                        onDisconnect(tcpClient);
                        return;
                    }
                    t.Result.ContinueWith(afterReadTaskAction);
                });
            }
            else
            {
                readTask.ContinueWith(afterReadTaskAction);
            }
        }

        public void Stop()
        {
            cts.Cancel();
            options.StatusHandler.Invoke("已停止");
            listener?.Stop();
            listener = null;
        }
    }
}
