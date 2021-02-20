using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;



public class WaveConverter
{
    public WaveConverter()
    {

    }

    public void ConvertToWave(string inputFileOgg, string outputFileWav)
    {

        using (FileStream fileIn = new FileStream(inputFileOgg, FileMode.Open))
        using (MemoryStream pcmStream = new MemoryStream())
        {
            OpusDecoder decoder = OpusDecoder.Create(48000, 1);

            OpusOggReadStream oggIn = new OpusOggReadStream(decoder, fileIn);
            while (oggIn.HasNextPacket)
            {
                short[] packet = oggIn.DecodeNextPacket();
                if (packet != null)
                {
                    for (int i = 0; i < packet.Length; i++)
                    {
                        var bytes = BitConverter.GetBytes(packet[i]);
                        pcmStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            pcmStream.Position = 0;
            var wavStream = new RawSourceWaveStream(pcmStream, new WaveFormat(48000, 1));
            var sampleProvider = wavStream.ToSampleProvider();
            WaveFileWriter.CreateWaveFile16(outputFileWav, sampleProvider);
        }
    }
}

