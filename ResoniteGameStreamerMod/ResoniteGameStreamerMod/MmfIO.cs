using System.IO;
using System.IO.MemoryMappedFiles;

namespace ResoniteGameStreamerMod
{
    internal static class MmfIO
    {
        internal static void EnsureAck()
        {
            if (RuntimeState.MmfAck == null)
            {
                RuntimeState.MmfAck = MemoryMappedFile.CreateOrOpen(RuntimeState.ClientAckMMFName, RuntimeState.ClientAckMMFSize);
                ResoniteGameStreamerMod.Msg("[Ack] Created/Opened client ack MMF");
            }
        }

        internal static void ReadFrameIfAvailable()
        {
            try
            {
                if (RuntimeState.MmfPixel == null)
                {
                    ResoniteGameStreamerMod.Error("[MMF] Pixel MMF not initialized.");
                    RuntimeState.PxDataLen = -1;
                    return;
                }

                RuntimeState.MmfView.Seek(0, SeekOrigin.Begin);

                short status = RuntimeState.Reader.ReadInt16();
                if (status == 0) { RuntimeState.PxDataLen = -1; return; }

                int tick = RuntimeState.Reader.ReadInt32();
                if (tick == RuntimeState.LatestFrameTick) { RuntimeState.PxDataLen = -1; return; }
                RuntimeState.LatestFrameTick = tick;

                RuntimeState.RowPairsLen = RuntimeState.Reader.ReadInt32();
                for (int i = 0; i < RuntimeState.RowPairsLen; i++)
                    RuntimeState.RowPairs[i] = RuntimeState.Reader.ReadInt16();

                RuntimeState.PxDataLen = RuntimeState.Reader.ReadInt32();
                for (int i = 0; i < RuntimeState.PxDataLen; i++)
                    RuntimeState.PxData[i] = RuntimeState.Reader.ReadInt32();
            }
            catch (System.Exception ex)
            {
                ResoniteGameStreamerMod.Error($"[MMF] Read error: {ex.Message}");
                RuntimeState.PxDataLen = -1;
            }
        }

        internal static void AckTick()
        {
            EnsureAck();
            using var s = RuntimeState.MmfAck.CreateViewStream();
            using var w = new BinaryWriter(s);
            w.Write(RuntimeState.LatestFrameTick);
        }
    }
}
