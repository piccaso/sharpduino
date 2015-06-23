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
    public class ArduinoMega : IDisposable
    {
        private readonly EasyFirmata firmata;

        /// <summary>
        /// Creates a new instance of the ArduinoMega. This implementation hides a lot
        /// of the complexity from the end user
        /// </summary>
        /// <param name="comPort">The port of the arduino board. All other parameters are supposed to be the default ones.</param>
        public ArduinoMega(string comPort)
        {
            var provider = new ComPortProvider(comPort);
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
        /// <param name="pin">The pinNum.</param>
        /// <param name="minPulse">The min pulse.</param>
        /// <param name="maxPulse">The max pulse.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <exception cref="InvalidPinModeException">If the pin doesn't support servo functionality</exception>
        public void SetServoMode(ArduinoMegaPins pin, int minPulse, int maxPulse, int startAngle)
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

        public void SetPinMode(ArduinoMegaPins pin, PinModes mode)
        {
            if ( firmata.IsInitialized == false )
                return;
            var currentPin = firmata.Pins[(int) pin];
            // Throw an exception if the pin doesn't have this capability
            if (!firmata.Pins[(int)pin].HasPinCapability(mode))
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
            
            // Update the pin state
            firmata.SendMessage(new PinStateQueryMessage(){Pin = (byte) pin});
        }

        public void SetDO(ArduinoMegaPins pinNum, bool newValue)
        {
            if (firmata.IsInitialized == false)
                return;


            var pin = firmata.Pins[(int) pinNum];

            // TODO : Decide on whether this should throw an exception
            if ( !pin.IsOutputMode() )
                return;


            // find the port which this pin belongs to
            var port = pin.Port;
            // get the values for the other pins in this port
            var previousValues = firmata.GetDigitalPortValues(port);
            // update the new value for this pin
            previousValues[(int) pinNum % 8] = newValue;
            // Send the message to the board
            firmata.SendMessage(new DigitalMessage(){Port = port, PinStates = previousValues});
            // update the new value to the firmata pins list
            firmata.Pins[(int) pinNum].CurrentValue = newValue ? 1 : 0;
        }

        public void SetPWM(ArduinoMegaPWMPins pin, int newValue)
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

        public void SetServo(ArduinoMegaPins pin, int newValue)
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

        public Pin GetCurrentPinState(ArduinoMegaPins pin)
        {
            if (!firmata.IsInitialized)
                return null;

            return firmata.Pins[(int) pin];
        }

        public float ReadAnalog(ArduinoMegaAnalogPins pin)
        {
            if (firmata.IsInitialized == false)
                return -1;
            var currentPin = firmata.AnalogPins[(int)pin];
            // TODO : Decide on whether this should throw an exception
            if (!currentPin.IsAnalogMode())
                return -1;

            return currentPin.CurrentValue;
        }

        public int ReadDigital(ArduinoMegaPins pin)
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
