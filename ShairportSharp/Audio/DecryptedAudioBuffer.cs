using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Audio
{
    class DecryptedAudioBuffer : AudioBuffer
    {
        AudioSession session;
        IBufferedCipher cipher;

        public DecryptedAudioBuffer(AudioSession session)
            : base(session.BufferSizeToFrames())
        {
            this.session = session;
            createAESCipher();
        }

        protected override byte[] ProcessNextPacket(byte[] packet)
        {
            return decryptPacket(packet);
        }

        protected override void OnPacketTaken()
        {
            session.UpdateFilter(actualBufferSize);
        }

        protected override void OnBufferRestart()
        {
            session.ResetFilter();
        }

        /// <summary>
        /// Decrypts the ALAC audio data
        /// </summary>
        /// <param name="buffer">encrypted ALAC data</param>
        /// <returns>decrypted ALAC data</returns>
        byte[] decryptPacket(byte[] buffer)
        {
            cipher.Reset();
            byte[] outBuffer = new byte[buffer.Length + AlacDecoder.Consts.EXTRA_BUFFER_SPACE];
            int remainder = buffer.Length % 16;
            int offset = buffer.Length - remainder;
            cipher.DoFinal(buffer, 0, offset, outBuffer, 0);
            Buffer.BlockCopy(buffer, offset, outBuffer, offset, remainder);
            return outBuffer;
        }


        /// <summary>
        /// Create AES cipher
        /// </summary>
        void createAESCipher()
        {
            cipher = CipherUtilities.GetCipher("AES/CBC/NoPadding");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(session.AesKey), session.AesIV));
        }
    }
}
