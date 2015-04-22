using Arm7;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShairportSharp.Sap
{
    public class SapHandler
    {
        const int INIT_SAP = 0x435B4;
        const int FP_INFO = 0x123;
        const int FP_CHALLENGE = 0xEB00C;
        const int FP_DECRYPT_KEY = 0xEB964;
        const int P_UNKNOWN = 0xabc;

        Loader loader;
        long sapInfo;

        public SapHandler()
        {
            loader = new Loader();
        }

        public void Init()
        {
            loader.Init(getAirtunesdStream());
            long pSapInfo = loader.Malloc(4);
            loader.Call(INIT_SAP, pSapInfo, FP_INFO);
            sapInfo = loader.LoadWord(pSapInfo);
        }

        public byte[] Challenge(byte[] challenge, int stage)
        {
            long pData = loader.Malloc(challenge.Length);
            loader.CopyIn(pData, challenge);

            long pOutData = loader.Malloc(4);
            long pOutLength = loader.Malloc(4);

            long pStage = loader.Malloc(4);
            loader.StoreWord(pStage, stage);

            byte sapType = challenge[4];
            loader.Call(FP_CHALLENGE, sapType, FP_INFO, sapInfo, pData, P_UNKNOWN, pOutData, pOutLength, pStage);

            long outData = loader.LoadWord(pOutData);
            long outLength = loader.LoadWord(pOutLength);
            //long outStage = loader.ld_word(pStage);

            byte[] response = loader.CopyOut(outData, outLength);
            return response;
        }

        public byte[] DecryptKey(byte[] encryptedKey)
        {
            long pKey = loader.Malloc(encryptedKey.Length);
            loader.CopyIn(pKey, encryptedKey);

            long pOutData = loader.Malloc(4);
            long pOutLength = loader.Malloc(4);

            loader.Call(FP_DECRYPT_KEY, sapInfo, pKey, encryptedKey.Length, pOutData, pOutLength);

            long outData = loader.LoadWord(pOutData);
            long outLength = loader.LoadWord(pOutLength);

            byte[] key = loader.CopyOut(outData, outLength);
            return key;
        }

        Stream getAirtunesdStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("ShairportSharp.Sap.airtunesd");
        }
    }
}
