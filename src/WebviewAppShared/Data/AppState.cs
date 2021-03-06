// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace WebviewAppTest
{
    public class AppState
    {
        public void init()
        {
            recordStatus = "stop";
            pitchStatus = "stop";
            normalRecord = "enabled";
            selectedInputDeviceIndex = -1;
            selectedOutputDeviceIndex = -1;
            pitchMethod = "None";
        }

        /// AVAILABLE INPUT DEVICES
        public string[] selectedInputDevice { get; set; }

        public void setInputDevices(string[] inp)
        {
            selectedInputDevice = inp;
        }

        /// AVAILABLE MIDI DEVICES
        public string[] selectedMIDIDevice { get; set; }
        public event EventHandler midiFetchStatus;

        public void setMIDIDevices(string[] inp)
        {
            selectedMIDIDevice = inp;
        }

        ///OUTPUT DEVICE STATUS

        public event EventHandler outputChanged;

        private void OnOutputChanged()
        {
            EventHandler eh = outputChanged;
            if (eh != null)
            {
                outputChanged(this, EventArgs.Empty);
            }
        }


        private int _selectedOutputDeviceIndex;
        public int selectedOutputDeviceIndex
        {
            get { return _selectedOutputDeviceIndex; }
            set
            {
                if (value != _selectedOutputDeviceIndex)
                {
                    _selectedOutputDeviceIndex = value;
                    OnOutputChanged();
                }
            }
        }
        public string[] selectedOutputDevice { get; set; }

        public void setOutputDevices(string[] inp)
        {
            selectedOutputDevice = inp;
        }

        ///RECORD STATUS

        public event EventHandler recordStatusChanged;
        public string _recordStatus;

        private void OnRecordStatusChanged()
        {
            System.Diagnostics.Debug.WriteLine("RECORD ST CHANGE");
            EventHandler eh = inputChanged;
            if (eh != null)
            {
                recordStatusChanged(this, EventArgs.Empty);
            }
        }

        public string recordStatus
        {
            get { return _recordStatus; }
            set
            {

                if (_selectedInputDeviceIndex != -1)
                {
                    _recordStatus = value;
                    OnRecordStatusChanged();
                }
            }
        }


        //SELECTED INPUT DEVICE

        public event EventHandler inputChanged;

        private void OnInputChanged()
        {
            EventHandler eh = inputChanged;
            if (eh != null)
            {
                inputChanged(this, EventArgs.Empty);
            }
        }


        private int _selectedInputDeviceIndex;
        public int selectedInputDeviceIndex
        {
            get { return _selectedInputDeviceIndex; }
            set
            {
                if (value != _selectedInputDeviceIndex)
                {
                    _selectedInputDeviceIndex = value;
                    OnInputChanged();
                }
            }
        }

        //SELECTED INPUT DEVICE

        public event EventHandler MIDIChanged;

        private void OnMIDIChanged()
        {
            EventHandler eh = MIDIChanged;
            if (eh != null)
            {
                MIDIChanged(this, EventArgs.Empty);
            }
        }


        private int _selectedMIDIDeviceIndex;
        public int selectedMIDIDeviceIndex
        {
            get { return _selectedMIDIDeviceIndex; }
            set
            {
                if (value != _selectedMIDIDeviceIndex)
                {
                    _selectedMIDIDeviceIndex = value;
                    OnMIDIChanged();
                }
            }
        }


        //RECORD PITCH VALUE & STATUS
        public event EventHandler pitchStatusChanged;
        public event EventHandler pitchValueChanged;
        public event EventHandler pitchMethodChanged;
        public string _pitchValue;
        public string _pitchStatus;
        public string _pitchMethod;

        private void OnPitchValueChanged()
        {
            EventHandler eh = pitchValueChanged;
            if (eh != null)
            {
                pitchValueChanged(this, EventArgs.Empty);
            }
        }

        private void OnPitchStatusChanged()
        {
            EventHandler eh = pitchStatusChanged;
            if (eh != null)
            {
                pitchStatusChanged(this, EventArgs.Empty);
            }
        }

        private void OnPitchMethodChanged()
        {
            EventHandler eh = pitchMethodChanged;
            if (eh != null)
            {
                System.Diagnostics.Debug.WriteLine(_pitchMethod);
                pitchMethodChanged(this, EventArgs.Empty);
            }
        }

        public string pitchValue
        {
            get { return _pitchValue; }
            set
            {
                System.Diagnostics.Debug.WriteLine(value);
                _pitchValue = value;
                OnPitchValueChanged();
            }
        }

        public void setPitchValue(string inp)
        {
            pitchValue = inp;
        }
        public string pitchStatus
        {
            get { return _pitchStatus; }
            set
            {
                _pitchStatus = value;
                OnPitchStatusChanged();
            }
        }

        public string pitchMethod
        {
            get { return _pitchMethod; }
            set
            {
                _pitchMethod= value;
                OnPitchMethodChanged();
            }
        }

        ///NORMAL RECORD STATUS

        public event EventHandler normalRecordChanged;
        public string _normalRecord;

        private void OnNormalRecordChanged()
        {
            EventHandler eh = normalRecordChanged;
            if (eh != null)
            {
                normalRecordChanged(this, EventArgs.Empty);
            }
        }

        public string normalRecord
        {
            get { return _normalRecord; }
            set
            {
                _normalRecord = value;
                OnNormalRecordChanged();

            }
        }

    }
}