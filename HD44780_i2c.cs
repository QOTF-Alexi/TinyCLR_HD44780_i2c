using System;
using System.Threading;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Display.Provider;

namespace QOTF_Alexi.TinyCLR.Display.HD44780_i2c
{
	public class HD44780Controller
	{
		I2cDevice i2c;
		int LCD_WIDTH;
		int LCD_HEIGHT;
		byte LCD_BACKLIGHT;

		int LCD_CHR = 1; // Mode - Sending data
		int LCD_CMD = 0; // Mode - Sending command

		// Hardcoded for now.
		byte LCD_LINE_1 = 0x80; // LCD RAM addr for line one
		byte LCD_LINE_2 = 0xC0; // LCD RAM addr for line two

		// Timing constants
		int E_PULSE = 1;
		int E_DELAY = 1;

		int ENABLE = 0b00000100; // Enable bit

		public static I2cConnectionSettings GetConnectionSettings() => new I2cConnectionSettings(0x27)
		{
			AddressFormat = I2cAddressFormat.SevenBit,
			BusSpeed = 400000,
		};

		public HD44780Controller(I2cDevice i2c, int width, int height, bool backlight)
		{
			this.i2c = i2c;
			LCD_WIDTH = width;
			LCD_HEIGHT = height;

			if (backlight)
			{
				LCD_BACKLIGHT = 0x08;
				Debug.WriteLine("Backlight HIGH");
			}
			else
			{
				LCD_BACKLIGHT = 0x00;
				Debug.WriteLine("Backlight LOW");
			}

			this.Initialize();
		}

		private void Initialize()
		{
			Thread.Sleep(50); // waiting for voltages to stabilise.
			SendData(0x33, LCD_CMD); // 110011 Initialise
			SendData(0x32, LCD_CMD); // 110010 Initialise
			SendData(0x06, LCD_CMD); // 000110 Cursor move direction
			SendData(0x0C, LCD_CMD); // 001100 Display On,Cursor Off, Blink Off
			SendData(0x28, LCD_CMD); // 101000 Data length, number of lines, font size
			SendData(0x01, LCD_CMD); // 000001 Clear display
			Debug.WriteLine("Initialised");
		}

		private void SendData(int bits, int mode)
		{
			// Send byte to data pins
			// bits = data
			// mode = 1 for data, 0 for command

			int originalHigh = mode | (bits & 0xF0) | LCD_BACKLIGHT;
			int originalLow = mode | ((bits << 4) & 0xF0) | LCD_BACKLIGHT;

            var bits_high = BitConverter.GetBytes(originalHigh);
			var bits_low = BitConverter.GetBytes(originalLow);

			// High bits
			this.i2c.Write(bits_high);
			ToggleEnable(originalHigh);
			Debug.WriteLine("Written high bits of " + originalHigh);


			// Low bits
			this.i2c.Write(bits_low);
			ToggleEnable(originalLow);
            Debug.WriteLine("Written low bits of " + originalLow);
        }

		private void ToggleEnable(int bits)
		{
			Thread.Sleep(E_DELAY);
			this.i2c.Write(BitConverter.GetBytes(bits | ENABLE));
			Thread.Sleep(E_PULSE);
			this.i2c.Write(BitConverter.GetBytes(bits & ~ENABLE));
			Thread.Sleep(E_DELAY);
		}

		public void Print(string text, int line = 1)
		{
			// display message string on LCD line 1 or 2
			byte lcd_line = default;
			if (line == 1) lcd_line = LCD_LINE_1;
			else if (line == 2) lcd_line = LCD_LINE_2;
			else Debug.WriteLine("Invalid line!");

			SendData(lcd_line, LCD_CMD);

			for (int i = 0; i < text.Length; i++)
			{
				SendData(text[i], LCD_CHR);
			}
		}

		public void Clear()
		{
			SendData(0x01, LCD_CMD);
		}
	}
}
