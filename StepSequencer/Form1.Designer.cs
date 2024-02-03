using NAudio.Midi;

namespace StepSequencer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBoxMidiInDevices = new ComboBox();
            SuspendLayout();
            // 
            // comboBoxMidiInDevices
            // 
            comboBoxMidiInDevices.FormattingEnabled = true;
            comboBoxMidiInDevices.Location = new Point(315, 109);
            comboBoxMidiInDevices.Name = "comboBoxMidiInDevices";
            comboBoxMidiInDevices.Size = new Size(151, 28);
            comboBoxMidiInDevices.TabIndex = 0;
            comboBoxMidiInDevices.SelectedIndexChanged += comboBoxMidiInDevices_SelectedIndexChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(comboBoxMidiInDevices);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private ComboBox comboBoxMidiInDevices;
    }
}