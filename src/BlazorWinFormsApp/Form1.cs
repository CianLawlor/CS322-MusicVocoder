// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using WebviewAppShared;
using WebviewAppTest;
using WebviewAppTest.Data;

namespace BlazorWinFormsApp
{
    public partial class Form1 : Form
    {
        private AppState _appState = new();

        public Form1()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<AppState>(_appState);

            serviceCollection.AddSingleton<WeatherForecastService>();

            InitializeComponent();

            blazorWebView1.HostPage = @"wwwroot\index.html";
            blazorWebView1.Services = serviceCollection.BuildServiceProvider();
            blazorWebView1.RootComponents.Add<App>("#app");

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                owner: this,
                text: $"Clinking",
                caption: "Counter");

            _appState.setInputDevices(new string[] { "Test" });
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
