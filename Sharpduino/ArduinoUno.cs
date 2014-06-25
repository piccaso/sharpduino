using System;
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
    	#region IDisposable Members

		~ArduinoUno()
		{
			Dispose(false);
		}
		
        /// <summary>
        /// Internal variable which checks if Dispose has already been called
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(Boolean disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                firmata.Dispose();
            }
            //TODO: Unmanaged cleanup code here

            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Call the private Dispose(bool) helper and indicate 
            // that we are explicitly disposing
            this.Dispose(true);

            // Tell the garbage collector that the object doesn't require any
            // cleanup when collected since Dispose was called explicitly.
            GC.SuppressFinalize(this);
        }

        #endregion
				
        private readonly EasyFirmata firmata;

        /// <summary>
        /// Creates a new instance of the ArduinoUno. This implementation hides a lot
        /// of the complexity from the end user
        /// </summary>
        /// <param name="comPort">The port of the arduino board. All other parameters are supposed to be the default ones.</param>
        public ArduinoUno(string comPort)
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

        private void InitCheck()
        {
            if (!firmata.IsInitialized)
                throw new FirmataException("firmata not yet initialized");
        }

        /// <summary>
        /// Sets a pin to servo mode with specific min and max pulse and start angle
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="minPulse">The min pulse.</param>
        /// <param name="maxPulse">The max pulse.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <exception cref="InvalidPinModeException">If the pin doesn't support servo functionality</exception>
        public void ServoMode(ArduinoUnoPins pin, int minPulse, int maxPulse, int startAngle)
        {
            InitCheck();

            var currentPin = firmata.Pins[(int) pin];

            // Throw an exception if the pin doesn't have this capability
            if (!currentPin.HasPinCapability(PinModes.Servo))
                throw new InvalidPinModeException(PinModes.Servo,currentPin.Capabilities.Keys.ToList());

            // Configure the servo mode
            firmata.SendMessage(new ServoConfigMessage() { Pin = (byte) pin, Angle = startAngle,MinPulse = minPulse, MaxPulse = maxPulse});
            currentPin.CurrentMode = PinModes.Servo;
            currentPin.CurrentValue = startAngle;
        }

        public void PinMode(int pin, string mode)
        {
            PinMode(pin, (PinModes)Enum.Parse(typeof(PinModes), mode, true));
        }

        public void PinMode(ArduinoUnoPins pin, PinModes mode)
        {
            PinMode((int)pin, mode);
        }

        public void PinMode(int pin, PinModes mode)
        {
            InitCheck();

            var currentPin = firmata.Pins[pin];

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
            firmata.SendMessage(new PinStateQueryMessage() { Pin = (byte)pin });
        }

        public void DigitalWrite(ArduinoUnoPins pin, bool newValue)
        {
            DigitalWrite((int)pin, newValue);
        }

        public void DigitalWrite(int pin, bool newValue)
        {
            InitCheck();

            // find the port which this pin belongs to
            var aPin = firmata.Pins[pin];

            var port = aPin.Port;
            // get the values for the other pins in this port
            var previousValues = firmata.GetDigitalPortValues(port);
            // update the new value for this pin
            previousValues[pin % 8] = newValue;
            // Send the message to the board
            firmata.SendMessage(new DigitalMessage() { Port = port, PinStates = previousValues });
            // update the new value to the firmata pins list
            firmata.Pins[pin].CurrentValue = newValue ? 1 : 0;
        }

        public void AnalogWrite(int pin, int newValue)
        {
            InitCheck();
            var currentPin = firmata.Pins[pin];

            if (!currentPin.IsPWMMode())
                throw new ArgumentException(string.Format("Pin {0} is not in PWM mode", pin));

            // Send the message to the board
            firmata.SendMessage(new AnalogMessage() { Pin = (byte)pin, Value = newValue });

            // Update the firmata pins list
            currentPin.CurrentValue = newValue;
        }        

        public void AnalogWrite(ArduinoUnoPWMPins pin, int newValue)
        {
            AnalogWrite((int)pin, newValue);
        }

        public void SetServo(ArduinoUnoPins pin, int newValue)
        {
            SetServo((int)pin, newValue);
        }

        public void SetServo(int pin, int newValue)
        {
            InitCheck();
            var currentPin = firmata.Pins[pin];
            if (!currentPin.IsServoMode())
                throw new ArgumentException(string.Format("Pin {0} is not in servo mode", pin));

            firmata.SendMessage(new AnalogMessage(){Pin = (byte)pin,Value = newValue});

            // Update the firmata pins list
            currentPin.CurrentValue = newValue;
        }

        public void SetSamplingInterval(int milliseconds)
        {
            InitCheck();
            firmata.SendMessage(new SamplingIntervalMessage() { Interval = milliseconds });
        }

        public Pin GetCurrentPinState(ArduinoUnoPins pin)
        {
            return GetCurrentPinState((int)pin);
        }

        public Pin GetCurrentPinState(int pin)
        {
            InitCheck();
            return firmata.Pins[(int) pin];
        }

        public float AnalogRead(ArduinoUnoAnalogPins pin)
        {
            return AnalogRead((int)pin);
        }

        public float AnalogRead(int pin)
        {
            InitCheck();

            var currentPin = firmata.AnalogPins[pin];
            if (!currentPin.IsAnalogMode())
                throw new ArgumentException(string.Format("Pin {0} is not in analog mode", pin));

            return currentPin.CurrentValue;
        }

        public int DigitalRead(ArduinoUnoPins pin)
        {
            return DigitalRead((int)pin);
        }

        public int DigitalRead(int pin)
        {
            InitCheck();

            return firmata.Pins[(int)pin].CurrentValue;
        }
    }
}
