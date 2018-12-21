using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Trnsprt.TCP
{
    internal sealed class TCPFactory
    {
        private static readonly Lazy<TCPFactory> instance = new Lazy<TCPFactory>(() => new TCPFactory());

        public static TCPFactory Instance => instance.Value;

        private TCPFactory() { }

        public TCPBase CreateTCP(string mode,IPAddress ip,int port,EventHandler<ReceiveEventArgs> onReceiveData)
        {
            Type type = Type.GetType(typeof(TCPBase).FullName.Replace("Base",mode),true);
            return Activator.CreateInstance(type,new object[] { ip,port,onReceiveData }) as TCPBase;
        }
    }

    internal class ReceiveEventArgs:EventArgs
    {
        public byte[] Receive { get; }

        public ReceiveEventArgs(byte[] receive) => Receive=receive;
    }

    internal class TCPBase
    {
        private const int BUFFER_SIZE = 1024*1024;
        protected Socket handle;
        protected bool connecting;
        protected byte[] buffer;

        public bool Connected
        {
            get
            {
                if(null==handle||!handle.Connected)
                {
                    return false;
                }
                try
                {
                    return !(handle.Poll(1000,SelectMode.SelectRead)&&0==handle.Available);
                }
                catch
                {
                    return false;
                }
            }
        }

        protected event EventHandler<ReceiveEventArgs> ReceiveData;

        public TCPBase(IPAddress ip,int port,EventHandler<ReceiveEventArgs> onReceiveData)
        {
            buffer=new byte[BUFFER_SIZE];
            ReceiveData+=new EventHandler<ReceiveEventArgs>(onReceiveData);
        }

        protected void ReceiveCallback(IAsyncResult ar)
        {
            if(!Connected)
            {
                return;
            }

            int receivelen;
            try
            {
                receivelen=handle.EndReceive(ar);
            }
            catch(ArgumentException)
            {
                return;
            }

            if(0<receivelen)
            {
                byte[] receive = new byte[receivelen];
                Buffer.BlockCopy(buffer,0,receive,0,receivelen);
                Interlocked.CompareExchange(ref ReceiveData,null,null)?.Invoke(this,new ReceiveEventArgs(receive));
            }
            handle.BeginReceive(buffer,0,buffer.Length,SocketFlags.None,new AsyncCallback(ReceiveCallback),handle);
        }

        public virtual bool Connect() => true;

        public bool Send(byte[] data)
        {
            try
            {
                handle.Send(data);
                return true;
            }
            catch(SocketException)
            {
                return false;
            }
        }

        public void DisConnect()
        {
            if(Connected)
            {
                handle.Close();
            }
        }
    }

    internal class TCPServer:TCPBase
    {
        private readonly int localPort;

        public TCPServer(IPAddress ip,int port,EventHandler<ReceiveEventArgs> onReceiveData) : base(ip,port,onReceiveData) => localPort=port;

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = ar.AsyncState as Socket;
            handle=listener.EndAccept(ar);
            listener.Close();
            connecting=false;
            handle.BeginReceive(buffer,0,buffer.Length,SocketFlags.None,new AsyncCallback(ReceiveCallback),handle);
        }

        public override bool Connect()
        {
            if(connecting)
            {
                return false;
            }

            Socket listen = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            listen.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);
            listen.Bind(new IPEndPoint(IPAddress.Any,localPort) as EndPoint);
            listen.Listen(0);
            connecting=true;
            listen.BeginAccept(new AsyncCallback(AcceptCallback),listen);
            return Connected;
        }
    }

    internal class TCPClient:TCPBase
    {
        private readonly IPEndPoint endPoint;

        public TCPClient(IPAddress ip,int port,EventHandler<ReceiveEventArgs> onReceiveData) : base(ip,port,onReceiveData) => endPoint=new IPEndPoint(ip,port);

        private void ConnectCallback(IAsyncResult ar)
        {
            handle=ar.AsyncState as Socket;
            try
            {
                handle.EndConnect(ar);
            }
            catch(SocketException)
            {
                return;
            }
            finally
            {
                connecting=false;
            }
            handle.BeginReceive(buffer,0,buffer.Length,SocketFlags.None,new AsyncCallback(ReceiveCallback),handle);
        }

        public override bool Connect()
        {
            if(connecting)
            {
                return false;
            }

            handle=new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            connecting=true;
            handle.BeginConnect(endPoint,new AsyncCallback(ConnectCallback),handle);
            return Connected;
        }
    }
}