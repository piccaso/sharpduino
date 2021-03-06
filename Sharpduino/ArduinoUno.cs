﻿using System;
using System.Linq;
using Sharpduino.Constants;
using Sharpduino.Exceptions;
using Sharpduino.Messages;
using Sharpduino.Messages.Send;
using Sharpduino.Messages.TwoWay;
using Sharpduino.SerialProviders;

namespace Sharpduino
{
    public class ArduinoUno : IDisposable
    {
        private readonly EasyFirmata firmata;

        /// <summary>
        /// Creates a new instance of the ArduinoUno. This implementation hides a lot
        /// of the complexity from the end user
        /// </summary>
        /// <param name="comPort">The port of the arduino board. All other parameters are supposed to be the default ones.</param>
        public ArduinoUno(string comPort, bool autoReset = false)
        {
            var provider = new ComPortProvider(comPort, autoReset: autoReset);
            firmata = new EasyFirmata(provider);
        }

        /// <summary>
        /// Property to show that the library has been initialized.
        /// Nothing can happen before we are initialized.   
        /// </summary>
        public bool IsInitialized
        {
            get { return firmata.IsInitialized; }
        }

        /// <summary>
        /// Sets a pin to servo mode with specific min and max pulse and start angle
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="minPulse">The min pulse.</param>
        /// <param name="maxPulse">The max pulse.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <exception cref="InvalidPinModeException">If the pin doesn't support servo functionality</exception>
        public void SetServoMode(ArduinoUnoPins pin, int minPulse, int maxPulse, int startAngle)
        {
            if (firmata.IsInitialized == false)
                return;

            var currentPin = firmata.Pins[(int) pin];

            // Throw an exception if the pin doesn't have this capability
            if (!currentPin.HasPinCapability(PinModes.Servo))
                throw new InvalidPinModeException(PinModes.Servo,currentPin.Capabilities.Keys.ToList());

            // Configure the servo mode
            firmata.SendMessage(new ServoConfigMessage() { Pin = (byte) pin, Angle = startAngle,MinPulse = minPulse, MaxPulse = maxPulse});
            currentPin.CurrentMode = PinModes.Servo;
            currentPin.CurrentValue = startAngle;
        }

        public void SetPinMode(ArduinoUnoPins pin, PinModes mode)
        {
            if ( firmata.IsInitialized == false )
                return;
            
            var currentPin = firmata.Pins[(int)pin];
            
            // Throw an exception if the pin doesn't have this capability
            if (!currentPin.HasPinCapability(mode))
                throw new InvalidPinModeException(PinModes.Servo, currentPin.Capabilities.Keys.ToList());

            switch (mode)
            {
                case PinModes.I2C:
                    // TODO : Special case for I2C message...            
                    throw new NotImplementedException();
                case PinModes.Servo:
                    // Special case for servo message...            
                    firmata.SendMessage(new ServoConfigMessage() { Pin = (byte)pin });
                    break;
                default:
                    firmata.SendMessage(new PinModeMessage { Mode = mode, Pin = (byte)pin });
                    break;
            }
            
            
            // TODO : see if we need this or the next way
            //firmata.Pins[(byte) pin].CurrentMode = mode;
            
            // Update the pin state
            firmata.SendMessage(new PinStateQueryMessage(){Pin = (byte) pin});
        }
        /// <summary>
        /// Shift out a value
        /// </summary>
        /// <param name="val">Data to shift out in groups of 8</param>
        /// <param name="data">Serial Data pin</param>
        /// <param name="clock">Serial click pin</param>
        /// <param name="latch">Register clock pin (latch) - Optional</param>
        public void shiftOut(System.Collections.BitArray val, ArduinoUnoPins data, ArduinoUnoPins clock, ArduinoUnoPins latch = ArduinoUnoPins.NONE)
        {
            if ((val.Count % 8) != 0) throw new ArgumentException("bit count invalid");
            if(latch != ArduinoUnoPins.NONE) this.SetDO(latch, false);
            foreach (bool bit in val)
            {
                this.SetDO(data, bit);
                this.SetDO(clock, true);
                this.SetDO(clock, false);
            }
            if (latch != ArduinoUnoPins.NONE) this.SetDO(latch, true);
        }

        public void SetDO(ArduinoUnoPins pin, bool newValue)
        {
            if (firmata.IsInitialized == false)
                return;

            // TODO : Decide on whether this should throw an exception
            if ( !firmata.Pins[(int) pin].IsOutputMode() )
                return;

            // find the port which this pin belongs to
            var aPin = firmata.Pins[(int) pin];
            
            var port = aPin.Port;
            // get the values for the other pins in this port
            var previousValues = firmata.GetDigitalPortValues(port);
            // update the new value for this pin
            previousValues[(int) pin % 8] = newValue;
            // Send the message to the board
            firmata.SendMessage(new DigitalMessage(){Port = port, PinStates = previousValues});
            // update the new value to the firmata pins list
            firmata.Pins[(int) pin].CurrentValue = newValue ? 1 : 0;
        }

        public void SetPWM(ArduinoUnoPWMPins pin, int newValue)
        {
            if (firmata.IsInitialized == false)
                return;
            var currentPin = firmata.Pins[(int)pin];
            // TODO : Decide on whether this should throw an exception
            if (!currentPin.IsPWMMode())
                return;

            // Send the message to the board
            firmata.SendMessage(new AnalogMessage(){Pin = (byte)pin, Value = newValue});

            // Update the firmata pins list
            currentPin.CurrentValue = newValue;
        }

        public void SetServo(ArduinoUnoPins pin, int newValue)
        {
            if (firmata.IsInitialized == false)
                return;
            var currentPin = firmata.Pins[(int)pin];
            // TODO : Decide on whether this should throw an exception
            if (!currentPin.IsServoMode())
                return;

            firmata.SendMessage(new AnalogMessage(){Pin = (byte)pin,Value = newValue});

            // Update the firmata pins list
            currentPin.CurrentValue = newValue;
        }

        public void SetSamplingInterval(int milliseconds)
        {
            if ( !firmata.IsInitialized )
                return;

            firmata.SendMessage(new SamplingIntervalMessage(){Interval = milliseconds});
        }

        public Pin GetCurrentPinState(ArduinoUnoPins pin)
        {
            if (!firmata.IsInitialized)
                return null;

            return firmata.Pins[(int) pin];
        }

        public float ReadAnalog(ArduinoUnoAnalogPins pin)
        {
            if (firmata.IsInitialized == false)
                return -1;
            var currentPin = firmata.AnalogPins[(int) pin];
            // TODO : Decide on whether this should throw an exception
            if (!currentPin.IsAnalogMode())
                return -1;

            return currentPin.CurrentValue;
        }

        public int ReadDigital(ArduinoUnoPins pin)
        {
            if (firmata.IsInitialized == false)
                return -1;
            var currentPin = firmata.Pins[(int)pin];
            if (!currentPin.IsInputMode())
                return -1;

            return currentPin.CurrentValue;
        }


        public void Dispose()
        {
            firmata.Dispose();
        }
    }
}
