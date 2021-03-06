﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.SmartCards;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MiFare;
using MiFare.Classic;
using MiFare.PcSc;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MiFareReader.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SmartCardReader Reader;
        private MiFareCard card;

        public MainPage()
        {
            this.InitializeComponent();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Application.Current.UnhandledException += Current_UnhandledException;

            GetDevices();
        }


        /// <summary>
        /// Enumerates NFC reader and registers event handlers for card added/removed
        /// </summary>
        /// <returns>None</returns>
        async private void GetDevices()
        {
            try
            {
                Reader = await CardReader.FindAsync();
                if (Reader == null)
                {
                    PopupMessage("No Readers Found");
                    return;
                }

                Reader.CardAdded += CardAdded;
                Reader.CardRemoved += CardRemoved;
            }
            catch (Exception e)
            {
                PopupMessage("Exception: " + e.Message);
            }
        }
        /// <summary>
        /// Card added event handler gets triggered when card enters nfc field
        /// </summary>
        /// <returns>None</returns>
        public async void CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            try
            {
                ChangeTextBlockFontColor(TextBlock_Header, Windows.UI.Colors.Green);
                await HandleCard(args);
            }
            catch (Exception e)
            {
                PopupMessage("CardAdded Exception: " + e.Message);
            }
        }
        /// <summary>
        /// Card removed event handler gets triggered when card leaves nfc field
        /// </summary>
        /// <returns>None</returns>
        void CardRemoved(SmartCardReader sender, CardRemovedEventArgs args)
        {

            card?.Dispose();

            ChangeTextBlockFontColor(TextBlock_Header, Windows.UI.Colors.Red);
        }
        /// <summary>
        /// Sample code to hande a couple of different cards based on the identification process
        /// </summary>
        /// <returns>None</returns>
        private async Task HandleCard(CardAddedEventArgs args)
        {
            try
            {
                card?.Dispose();
                card = args.SmartCard.CreateMiFareCard();



                var cardIdentification = await card.GetCardInfo();


                DisplayText("Connected to card\r\nPC/SC device class: " + cardIdentification.PcscDeviceClass.ToString() + "\r\nCard name: " + cardIdentification.PcscCardName.ToString());

                if (cardIdentification.PcscDeviceClass == MiFare.PcSc.DeviceClass.StorageClass
                     && (cardIdentification.PcscCardName == CardName.MifareStandard1K || cardIdentification.PcscCardName == CardName.MifareStandard4K))
                {
                    // Handle MIFARE Standard/Classic
                    DisplayText("MIFARE Standard/Classic card detected");


                    var uid = await card.GetUid();
                    DisplayText("UID:  " + BitConverter.ToString(uid));




                    // 16 sectors, print out each one
                    for (var sector = 0; sector < 16; sector++)
                    {
                        try
                        {
                            var data = await card.GetData(sector, 0, 48);

                            string hexString = "";
                            for (int i = 0; i < data.Length; i++)
                            {
                                hexString += data[i].ToString("X2") + " ";
                            }

                            DisplayText(string.Format("Sector '{0}':{1}", sector, hexString));

                        }
                        catch (Exception)
                        {
                            DisplayText("Failed to load sector: " + sector);
                        }
                    }



                }
            }
            catch (Exception e)
            {
                PopupMessage("HandleCard Exception: " + e.Message);
            }
        }

        /// <summary>
        /// Change text of UI textbox
        /// </summary>
        /// <returns>None</returns>
        private void DisplayText(string message)
        {
            Debug.WriteLine(message);
            var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                txtLog.Text += message + Environment.NewLine;
            });
        }

        /// <summary>
        /// Changes font color of main application banner
        /// </summary>
        /// <returns>None</returns>
        private void ChangeTextBlockFontColor(TextBlock textBlock, Windows.UI.Color color)
        {
            var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                textBlock.Foreground = new SolidColorBrush(color);
            });
        }
        /// <summary>
        /// Display message via dialogue box
        /// </summary>
        /// <returns>None</returns>
        public async void PopupMessage(string message)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var dlg = new MessageDialog(message);
                await dlg.ShowAsync();
            });
        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string message = e.Exception.Message;
            if (e.Exception.InnerException != null)
            {
                message += Environment.NewLine + e.Exception.InnerException.Message;
            }

            PopupMessage(message);
        }
        /// <summary>
        /// Capture any unobserved exception
        /// </summary>
        /// <returns>None</returns>
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            string message = e.Exception.Message;
            if (e.Exception.InnerException != null)
            {
                message += Environment.NewLine + e.Exception.InnerException.Message;
            }

            PopupMessage(message);
        }
    }
}
