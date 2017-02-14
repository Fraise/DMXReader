using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DMXReader
{
	class Program
	{
		public static byte[] buffer = new byte[513];
		public static uint handle;
		public static bool done = false;
		public static bool Connected = false;
		public static int bytesWritten = 0;
		public static FT_STATUS status;

		public const byte BITS_8 = 8;
		public const byte STOP_BITS_2 = 2;
		public const byte PARITY_NONE = 0;
		public const UInt16 FLOW_NONE = 0;
		public const byte PURGE_RX = 1;
		public const byte PURGE_TX = 2;

		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_Open(UInt32 uiPort, ref uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_Close(uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_Read(uint ftHandle, IntPtr lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_Write(uint ftHandle, IntPtr lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_SetDataCharacteristics(uint ftHandle, byte uWordLength, byte uStopBits, byte uParity);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_SetFlowControl(uint ftHandle, char usFlowControl, byte uXon, byte uXoff);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_GetModemStatus(uint ftHandle, ref UInt32 lpdwModemStatus);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_Purge(uint ftHandle, UInt32 dwMask);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_ClrRts(uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_SetBreakOn(uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_SetBreakOff(uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_GetStatus(uint ftHandle, ref UInt32 lpdwAmountInRxQueue, ref UInt32 lpdwAmountInTxQueue, ref UInt32 lpdwEventStatus);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_ResetDevice(uint ftHandle);
		[DllImport("FTD2XX.dll")]
		public static extern FT_STATUS FT_SetDivisor(uint ftHandle, char usDivisor);

		static void Main(string[] args)
		{
			Console.CursorVisible = false;				//Hide console cursor

			try
			{											//find and connect to device (first found if multiple)
				initOpenDMX();
				start();
				if (status == FT_STATUS.FT_DEVICE_NOT_FOUND)       //Update status
					Console.WriteLine("No Enttec USB Device Found");
				else if (status == FT_STATUS.FT_OK)
				{
					Console.WriteLine("Found DMX on USB");
				}
				else
					Console.WriteLine("Error Opening Device");
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
				Console.WriteLine("Error Connecting to Enttec USB Device");

			}

			byte[] buffer = new byte[1024];				//To store data, you need to create an IntPtr, the allocate a space in non managed memory for the array the pointer refer to
			IntPtr ptr = new IntPtr();
			ptr = Marshal.AllocHGlobal((int)buffer.Length);
			Marshal.Copy(buffer, 0, ptr, buffer.Length);

			while (true)
			{
				readData(ptr);
				Thread.Sleep(10);
			}
		}

		public static void start()
		{
			handle = 0;
			status = FT_Open(0, ref handle);
		}

		public static void initOpenDMX()
		{
			status = FT_ResetDevice(handle);
			status = FT_SetDivisor(handle, (char)12);  // set baud rate
			status = FT_SetDataCharacteristics(handle, BITS_8, STOP_BITS_2, PARITY_NONE);
			status = FT_SetFlowControl(handle, (char)FLOW_NONE, 0, 0);
			status = FT_ClrRts(handle);
			status = FT_Purge(handle, PURGE_TX);
			status = FT_Purge(handle, PURGE_RX);
		}

		public static void readData(IntPtr ptr)
		{
			try
			{
				if (status == FT_STATUS.FT_OK)
				{
					UInt32 txq = 0;
					UInt32 rxq = 0;
					UInt32 eventStatus = 0;

					FT_GetStatus(handle, ref rxq, ref txq, ref eventStatus);

					if (rxq != 0)
					{
						Console.SetCursorPosition(0, 1);
						uint bytesread = 0;
						FT_Read(handle, ptr, rxq, ref bytesread);

						if (rxq == 519)			//Frames are 519 bytes long. It mays depends of the DMX controller used? If the enttec usb is found but you still see nothing, check this parameter
						{
							for (int i = 6; i < 518; i++)
								Console.Write(Marshal.ReadByte(ptr, i) + "\t");
						}
					}
				}
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		/// <summary>
		/// Enumaration containing the varios return status for the DLL functions.
		/// </summary>
		public enum FT_STATUS
		{
			FT_OK = 0,
			FT_INVALID_HANDLE,
			FT_DEVICE_NOT_FOUND,
			FT_DEVICE_NOT_OPENED,
			FT_IO_ERROR,
			FT_INSUFFICIENT_RESOURCES,
			FT_INVALID_PARAMETER,
			FT_INVALID_BAUD_RATE,
			FT_DEVICE_NOT_OPENED_FOR_ERASE,
			FT_DEVICE_NOT_OPENED_FOR_WRITE,
			FT_FAILED_TO_WRITE_DEVICE,
			FT_EEPROM_READ_FAILED,
			FT_EEPROM_WRITE_FAILED,
			FT_EEPROM_ERASE_FAILED,
			FT_EEPROM_NOT_PRESENT,
			FT_EEPROM_NOT_PROGRAMMED,
			FT_INVALID_ARGS,
			FT_OTHER_ERROR
		};
	}
}
