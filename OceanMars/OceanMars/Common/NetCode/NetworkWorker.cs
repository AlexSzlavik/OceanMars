using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OceanMars.Common.NetCode
{
    class NetworkWorker : UdpClient
    {
        private Thread rcvThread, sendThread;
        private bool go = true;

        private Queue<Packet> buffer = new Queue<Packet>();
        private Queue<Packet> readBuffer = new Queue<Packet>();

        private static int id = 0;
        private int myID;

        private Semaphore sendSem = new Semaphore(0, 1000);
        private Semaphore nextSem = new Semaphore(0, 1000);

        private static int TIMEOUT = 5000;

        //This is the server side
        public NetworkWorker(int port = 0)
            : base(port)
        {
            //Console.WriteLine("Started NW-Server on port: " + this.Client.LocalEndPoint);
            this.rcvThread = new Thread(thread_do_recv);
            this.sendThread = new Thread(thread_do_send);
            rcvThread.Name = "Server Receive Thread ID: " + id;
            sendThread.Name = "Sever Send Thread ID: " + id;
            this.rcvThread.Start();
            this.sendThread.Start();
            this.myID = id;
            NetworkWorker.id++;
        }

        //This init's so we only communicate with one
        //It's the Client init (so port is any)
        public NetworkWorker(IPEndPoint endpt)
            : base(0)
        {
            //Console.WriteLine("Started NW-Client on port: " + this.Client.LocalEndPoint);
            this.rcvThread = new Thread(thread_do_recv);
            this.sendThread = new Thread(thread_do_send);
            rcvThread.Name = "Client Receive Thread ID: " + id;
            sendThread.Name = "Client Send Thread ID: " + id;
            this.rcvThread.Start();
            this.sendThread.Start();
            this.myID = id;
            NetworkWorker.id++;
        }

        public void commitPacket(Packet p)
        {
            lock (this.buffer)
            {
                this.buffer.Enqueue(p);
                //Console.WriteLine("Backlog Commit: {0} "+Thread.CurrentThread.Name, buffer.Count);
            }
            this.sendSem.Release();
        }

        public Packet getNext()
        {
            if (this.nextSem.WaitOne(TIMEOUT))
            {
                Packet ret;
                lock (readBuffer)
                {
                    ret = readBuffer.Dequeue();
                }

                return ret;
            }
            else
            {
                return null;
            }
        }

        private void thread_do_recv()
        {
            Thread.CurrentThread.IsBackground = true; // Set the thread to run invisibly in the background
            IPEndPoint serverAddress = new IPEndPoint(IPAddress.Any, 0);
            while (this.go)
            {
                using (MemoryStream incomingPacketStream = new MemoryStream(this.Receive(ref serverAddress)))
                {   
                    Packet receivePacket = new Packet((Packet.PacketType)incomingPacketStream.ReadByte(), serverAddress);
                    incomingPacketStream.Read(receivePacket.DataArray, 0, (int)incomingPacketStream.Length - 1);
                    lock (readBuffer)
                    {
                        readBuffer.Enqueue(receivePacket);
                        //if (readBuffer.Count > 50)
                        //{
                        //    Console.WriteLine("ReadBuffer falling behind {0}:" + Thread.CurrentThread.Name, readBuffer.Count);
                        //}
                    }
                    this.nextSem.Release();
                }
            }
            return;
        }

        private void thread_do_send()
        {
            Thread.CurrentThread.IsBackground = true;
            while (this.go)
            {
                this.sendSem.WaitOne(); //Get Semaphore
                lock (this.buffer)
                {
                    //Console.WriteLine("NW-" + myID + " Send");
                    Packet pkt = this.buffer.Dequeue();
                    //Console.WriteLine("--> {0}", (byte)pkt.data[0]);
                    int i = this.Send(pkt.DataArray, pkt.DataArray.Length, pkt.Destination);
                    //Console.WriteLine(i);
                    //if (buffer.Count > 50)
                    //{
                    //    Console.WriteLine("Buffer falling behind {0}:" + Thread.CurrentThread.Name, buffer.Count);
                    //}
                }
            }
        }
    }

}
