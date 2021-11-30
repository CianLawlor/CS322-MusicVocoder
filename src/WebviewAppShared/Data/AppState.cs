// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace WebviewAppTest
{
    public class AppState
    {
        
        /// INPUT DEVICE STATUS
        public string[] selectedInputDevice { get; set; }

        public void setInputDevices(string[] inp)
        {
            selectedInputDevice = inp;
        }

        ///OUTPUT DEVICE STATUS
        
        public event EventHandler outputDeviceChanged;

        public int selectedOutputDeviceIndex { get; set; }
        public string[] selectedOutputDevice { get; set; }


        ///RECORD STATUS

        public event EventHandler recordStatusChanged;
        public string _recordStatus;

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

        private void OnRecordStatusChanged()
        {
            EventHandler eh = inputChanged;
            if (eh != null)
            {
                recordStatusChanged(this, EventArgs.Empty);
            }
        }

        public string recordStatus {
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
                 _pitchValue = value-1;
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
