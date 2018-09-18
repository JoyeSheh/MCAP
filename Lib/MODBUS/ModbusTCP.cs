using System;
using System.Collections.Generic;

namespace Applctn.Modbus
{
    internal class RequestTCP : RequestBase
    {
        private const int LenOfMBAP = 6;
        private static byte[] MBAPHead;

        public RequestTCP()
        {
            LenOfHead = LenOfMBAP + 3;
            LenOfRear = 0;
        }

        public override byte[] CreateRequestArray(byte device, MiniFrame miniFrame)
        {
            byte[] result = new byte[LenOfMBAP + LenOfRequest];
            byte[] request = base.CreateRequestArray(device, miniFrame);
            MBAPHead = new byte[] { 0, 1, 0, 0, 0, LenOfRequest };
            Buffer.BlockCopy(MBAPHead, 0, result, 0, LenOfMBAP);
            Buffer.BlockCopy(request, 0, result, LenOfMBAP, LenOfRequest);
            return result;
        }
    }

    internal class ResponseTCP : ResponseBase
    {
        private const int LenOfMBAP = 6;

        public ResponseTCP(Dictionary<int, RegisterType> dictype) : base(dictype)
        {
            IndexOfFunCode = LenOfMBAP + 1;
            IndexOfLength = LenOfMBAP + 2;
            LenOfHead = LenOfMBAP + 3;
            LenOfRear = 0;
        }
    }
}
