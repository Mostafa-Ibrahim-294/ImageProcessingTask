using System.ComponentModel;

namespace PaxeraHealthTask
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Button openImageButton;
        private Button invertFilterButton;
        private Button grayscaleFilterButton;
        private Button saveImageButton;
        private Button resetButton;
        private Panel imageHolder;
        private Label infoLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(96F, 96F);
            BackColor = Color.FromArgb(245, 245, 245);
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(700, 500);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Image Processing Task";

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Color.FromArgb(230, 230, 230),
                FlowDirection = FlowDirection.LeftToRight
            };

            openImageButton = new Button
            {
                Text = "Open Image",
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };
            invertFilterButton = new Button
            {
                Text = "Invert",
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };
            grayscaleFilterButton = new Button
            {
                Text = "Grayscale",
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };
            resetButton = new Button
            {
                Text = "Reset",
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };
            saveImageButton = new Button
            {
                Text = "Save Image",
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };

            openImageButton.Click += OpenImageButton_Click;
            invertFilterButton.Click += InvertFilterButton_Click;
            grayscaleFilterButton.Click += GrayscaleFilterButton_Click;
            resetButton.Click += ResetButton_Click;
            saveImageButton.Click += SaveImageButton_Click;

            toolbar.Controls.Add(openImageButton);
            toolbar.Controls.Add(invertFilterButton);
            toolbar.Controls.Add(grayscaleFilterButton);
            toolbar.Controls.Add(resetButton);
            toolbar.Controls.Add(saveImageButton);

            infoLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 28,
                Padding = new Padding(12, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            imageHolder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(44, 44, 44),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(12, 6, 12, 12)
            };

            Controls.Add(imageHolder);
            Controls.Add(infoLabel);
            Controls.Add(toolbar);
        }
    }
}