﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MiFare.Classic;
using MiFare.Win32;

namespace MiFare.Devices
{
    public sealed class SmartCardConnection : IDisposable
    {
        private IntPtr hCard;
        private int hProtocol;

        internal SmartCardConnection(IntPtr hCard, int hProtocol)
        {
            this.hCard = hCard;
            this.hProtocol = hProtocol;
        }

        public byte[] Tranceive(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var sioreq = new SCARD_IO_REQUEST
            {
                dwProtocol = 0x2,
                cbPciLength = 8
            };
            var rioreq = new SCARD_IO_REQUEST
            {
                cbPciLength = 8,
                dwProtocol = 0x2
            };

            var receiveBuffer = new byte[256];
            var rlen = receiveBuffer.Length;

            var retVal = SafeNativeMethods.SCardTransmit(hCard, ref sioreq, buffer, buffer.Length, ref rioreq, receiveBuffer, ref rlen);
            Helpers.CheckError(retVal);


            var retBuf = new byte[rlen];
            Array.Copy(receiveBuffer, retBuf, rlen);

            return retBuf;
        }

        private void Disconnect()
        {
            if (hCard != IntPtr.Zero)
            {
                var retVal = SafeNativeMethods.SCardDisconnect(hCard, Constants.SCARD_UNPOWER_CARD);

                hCard = IntPtr.Zero;
                Helpers.CheckError(retVal);
            }
        }

        private void Dispose(bool disposing)
        {
            Disconnect();
        }

        public void Dispose()
        {
            Debug.WriteLine("Dispose: " + nameof(SmartCardConnection));
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SmartCardConnection()
        {
            Dispose(false);
        }
    }
}
