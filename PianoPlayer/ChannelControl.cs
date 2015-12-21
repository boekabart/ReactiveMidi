using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;

namespace PianoPlayer
{
    public partial class ChannelControl : UserControl
    {
        public ChannelControl( int number )
        {
            InitializeComponent();
            laNumber.Text = (number + 1).ToString();
        }

        private void ChannelControl_Load(object sender, EventArgs e)
        {
            cbChosenInstrument.Items.Clear();
            for (Sanford.Multimedia.Midi.GeneralMidiInstrument instrument = Sanford.Multimedia.Midi.GeneralMidiInstrument.AcousticGrandPiano; instrument <= Sanford.Multimedia.Midi.GeneralMidiInstrument.Gunshot; instrument++)
                cbChosenInstrument.Items.Add(instrument);
            cbChosenInstrument.SelectedIndex = 0;
            UpdateText();
        }

        Sanford.Multimedia.Midi.GeneralMidiInstrument autoInstrument = Sanford.Multimedia.Midi.GeneralMidiInstrument.AcousticGrandPiano;
        public Sanford.Multimedia.Midi.GeneralMidiInstrument CurrentInstrument
        {
            get
            {
                if (cbAutoInstrument.Checked)
                    return autoInstrument;
                else
                    return selectedInstrument;
            }
            set
            {
                autoInstrument = value;

                if (InvokeRequired)
                    BeginInvoke(new UpdateTextDelegate(UpdateText));
                else
                    UpdateText();
            }
        }

        private delegate void UpdateTextDelegate();

        private void UpdateText()
        {
            var textToSpeak = autoInstrument.ToString();
            if (cbAutoInstrument.Text.Equals(textToSpeak))
                return;
            cbAutoInstrument.Text = textToSpeak;

            if (laNumber.Text=="1")
                SayAsync(textToSpeak);
        }

        private static void SayAsync(string textToSpeak)
        {
            var synthesizer = new SpeechSynthesizer
            {
                Volume = 100,
                Rate = 2
            };
            // 0...100
            // -10...10

            // Synchronous

            synthesizer.SelectVoiceByHints(VoiceGender.Female);
            synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Child);
            synthesizer.SpeakAsync(textToSpeak);
        }

        private delegate void UpdateLedDelegate();

        private void UpdateLed()
        {
            rbActive.Checked = active;
        }

        int otherSolo = 0;
        bool OtherSolo
        {
            get
            { 
                return otherSolo > 0; 
            }
            set
            {
                if (value)
                {
                    otherSolo++;
                    if (cbSolo.Checked)
                        cbSolo.Checked = false;
                }
                else
                    otherSolo--;
            }
        }

        volatile bool active;
        public bool Active
        {
            get { return active; }
            set 
            {
                if (active == value)
                    return;
                active = value;
                if (InvokeRequired)
                    BeginInvoke(new UpdateLedDelegate(UpdateLed));
                else
                    UpdateLed();
            }
        }

        public bool Muted
        {
            get
            {
                return OtherSolo || cbMute.Checked;
            }
        }

        private void cbMute_CheckedChanged(object sender, EventArgs e)
        {
            cbSolo.Enabled = !cbMute.Checked;
        }

        private void cbSolo_CheckedChanged(object sender, EventArgs e)
        {
            cbMute.Enabled = !cbSolo.Checked;

            foreach (Control c in Parent.Controls)
            {
                ChannelControl othercc = c as ChannelControl;
                if (othercc == null)
                    continue;

                if (othercc == this)
                    continue;

                othercc.OtherSolo = cbSolo.Checked;
            }
        }

        volatile Sanford.Multimedia.Midi.GeneralMidiInstrument selectedInstrument = Sanford.Multimedia.Midi.GeneralMidiInstrument.AcousticGrandPiano;
        private void cbChosenInstrument_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbChosenInstrument.SelectedItem != null)
                selectedInstrument = (Sanford.Multimedia.Midi.GeneralMidiInstrument)cbChosenInstrument.SelectedItem;
        }
    }
}
