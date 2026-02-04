namespace fsh;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    Label lblTile1 = new Label();
    Panel pnlTile1 = new Panel();
    Label lblTile2 = new Label();
    Panel pnlTile2 = new Panel();
    Label lblStatus = new Label();
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
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(200, 450);
        Text = "Form1";

        this.TopMost = true;

        int YLocation = 8;
        int XLocation = 8;
        int YPadding = 8;

        Button btnSubmit = new Button();
        btnSubmit.Text = "Start";
        btnSubmit.Location = new Point(XLocation, YLocation);
        btnSubmit.BackColor = Color.LightBlue;
        btnSubmit.Click += (s, e) => { 
            Find.Toggle(); 
            btnSubmit.Text = Find.Started ? "Stop" : "Stop"; 
        };
        YLocation += btnSubmit.Size.Height + YPadding;


        lblTile1.Location = new Point(XLocation, YLocation);
        lblTile1.Text = "Brighness: {0}";
        YLocation += lblTile1.Size.Height + YPadding;

        pnlTile1.Size = new Size(128, 128);
        pnlTile1.Location = new Point(XLocation, YLocation);
        pnlTile1.BackColor = Color.DarkGray;
        YLocation += pnlTile1.Size.Height + YPadding;

        lblTile2.Location = new Point(XLocation, YLocation);
        lblTile2.Text = "Brighness: {0}";
        YLocation += lblTile2.Size.Height + YPadding;

        pnlTile2.Size = new Size(128, 128);
        pnlTile2.Location = new Point(XLocation, YLocation);
        pnlTile2.BackColor = Color.DarkGray;
        
        YLocation += pnlTile2.Size.Height + YPadding;

        lblStatus.Location = new Point(XLocation, YLocation);
        lblStatus.Text = "Status: {0}";
        YLocation += lblStatus.Size.Height + YPadding;

        this.Controls.Add(btnSubmit);
        this.Controls.Add(lblTile1);
        this.Controls.Add(lblTile2);
        this.Controls.Add(pnlTile1);
        this.Controls.Add(pnlTile2);
        this.Controls.Add(lblStatus);
    }

    #endregion
}
