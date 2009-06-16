using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using System.Threading;

namespace Infiniminer
{
    class MapSender
    {
        NetConnection client;
        Thread conn;
        Infiniminer.InfiniminerServer infs;
        Infiniminer.InfiniminerNetServer infsN;
        int MAPSIZE = 64;
        bool compression = false;
        //bool finished = false;
        public bool finished
        {
            get
            {
                return !conn.IsAlive;
            }
        }

        public MapSender(NetConnection nClient, Infiniminer.InfiniminerServer nInfs, Infiniminer.InfiniminerNetServer nInfsN, int nMAPSIZE, bool compress)
        {
            client = nClient;
            infs = nInfs;
            infsN = nInfsN;
            MAPSIZE = nMAPSIZE;
            compression = compress;
            //finished = false;
            conn = new Thread(new ThreadStart(this.start));
            conn.Start();
            DateTime started = DateTime.Now;
            TimeSpan diff = DateTime.Now - started;
            while (!conn.IsAlive&&diff.Milliseconds<250) //Hold execution until it starts
            {
                diff = DateTime.Now - started;
            }
        }

        private void start()
        {
            //Debug.Assert(MAPSIZE == 64, "The BlockBulkTransfer message requires a map size of 64.");

            for (byte x = 0; x < MAPSIZE; x++)
                for (byte y = 0; y < MAPSIZE; y += 16)
                {
                    NetBuffer msgBuffer = infsN.CreateBuffer();
                    msgBuffer.Write((byte)Infiniminer.InfiniminerMessage.BlockBulkTransfer);
                    if (!compression)
                    {
                        msgBuffer.Write(x);
                        msgBuffer.Write(y);
                        for (byte dy = 0; dy < 16; dy++)
                            for (byte z = 0; z < MAPSIZE; z++)
                                msgBuffer.Write((byte)(infs.blockList[x, y + dy, z]));
                        if (client.Status == NetConnectionStatus.Connected)
                            infsN.SendMessage(msgBuffer, client, NetChannel.ReliableUnordered);
                    }
                    else
                    {
                        //Compress the data so we don't use as much bandwith - Xeio's work
                        var compressedstream = new System.IO.MemoryStream();
                        var uncompressed = new System.IO.MemoryStream();
                        var compresser = new System.IO.Compression.GZipStream(compressedstream, System.IO.Compression.CompressionMode.Compress);

                        //Send a byte indicating that yes, this is compressed
                        msgBuffer.Write((byte)255);

                        //Write everything we want to compress to the uncompressed stream
                        uncompressed.WriteByte(x);
                        uncompressed.WriteByte(y);

                        for (byte dy = 0; dy < 16; dy++)
                            for (byte z = 0; z < MAPSIZE; z++)
                                uncompressed.WriteByte((byte)(infs.blockList[x, y + dy, z]));

                        //Compress the input
                        compresser.Write(uncompressed.ToArray(), 0, (int)uncompressed.Length);
                        //infs.ConsoleWrite("Sending compressed map block, before: " + uncompressed.Length + ", after: " + compressedstream.Length);
                        compresser.Close();

                        //Send the compressed data
                        msgBuffer.Write(compressedstream.ToArray());
                        if (client.Status == NetConnectionStatus.Connected)
                            infsN.SendMessage(msgBuffer, client, NetChannel.ReliableUnordered);
                    }
                }
            conn.Abort();
        }

        public void stop()
        {
            conn.Abort();
        }
    }
}
