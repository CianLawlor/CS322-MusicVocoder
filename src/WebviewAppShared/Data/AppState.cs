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
            selectedInputDeviceIndex = -1;
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

        public event EventHandler outputDeviceChanged;

        public int selectedOutputDeviceIndex { get; set; }
        public string[] selectedOutputDevice { get; set; }


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


        //RECORD PITCH VALUE & STATUS
        public event EventHandler pitchStatusChanged;
        public event EventHandler pitchValueChanged;
        public int _pitchValue;
        public string _pitchStatus;

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

        public int pitchValue
        {
            get { return _pitchValue; }
            set
            {
                _pitchValue = value - 1;
                OnPitchValueChanged();
            }
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

    }
}