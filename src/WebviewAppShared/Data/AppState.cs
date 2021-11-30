// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace WebviewAppTest
{
    public class AppState
    {
        public event EventHandler inputChanged;

        private void OnInputChanged()
        {
            EventHandler eh = inputChanged;
            if (eh != null)
            {
                inputChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler outputChanged;

        public int Counter { get; set; }

        private int _selectedInputDeviceIndex;
        public int selectedInputDeviceIndex {
            get { return _selectedInputDeviceIndex; } 
            set { 
                if(value != _selectedInputDeviceIndex)
                {
                    _selectedInputDeviceIndex = value;
                    OnInputChanged();
                }
            } 
        }

        public string[] selectedInputDevice { get; set; }

        public void setInputDevices(string[] inp)
        {
            selectedInputDevice = inp;
        }

        public int selectedOutputDeviceIndex { get; set; }
        public string[] selectedOutputDevice { get; set; }


        ///RECORD STATUS

        public event EventHandler recordStatusChanged;
        public string _recordStatus;

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
    }
}
