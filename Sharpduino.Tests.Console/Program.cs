using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Sharpduino.Base;
using Sharpduino.Constants;
using Sharpduino.EventArguments;
using Sharpduino.Messages.Send;
using Sharpduino.Messages.TwoWay;
using Sharpduino.SerialProviders;
using System.Collections;

namespace Sharpduino.Tests.Consoles
{
    class Program
    {
        private static bool isInitialized = false;
        static void Main(string[] args)
        {
            ArduinoUnoPins latchPin = ArduinoUnoPins.D2;
            ArduinoUnoPins clockPin = ArduinoUnoPins.D3_PWM;
            ArduinoUnoPins dataPin = ArduinoUnoPins.D4;

            BitArray bits = new BitArray(new bool[] { 
                false, true, false, true, false, false, false, false,
                false, true, false, true, false, false, false, false,
                false, true, false, true, false, false, false, false
            });

            uint cnt = 0;
            foreach (bool bit in bits)
            {
                Console.Write(bit ? "1" : "0");
                if (++cnt % 8 == 0) Console.WriteLine();
            }

            ArduinoUno uno = new ArduinoUno("COM5",true);
            Console.WriteLine("isInit = " + uno.IsInitialized.ToString());

            uno.shiftOut(bits, dataPin, clockPin, latchPin);

            Console.ReadKey();


            

        }

        static void easyFirmata_NewAnalogValue(object sender, NewAnalogValueEventArgs e)
        {
            Console.WriteLine("New Value for pin {0} : {1}",e.AnalogPin,e.NewValue);
        }

        static void easyFirmata_Initialized(object sender, EventArgs e)
        {
            Console.WriteLine("Firmata has initialized");
            isInitialized = true;
        }
    }
}
