namespace fsh;

public partial class MainForm : Form
{
    //public Thread? thread;
    public MainForm()
    {
        InitializeComponent();

        Load += (n, e) =>
        {
            Find.StatusChanged += (sender, args) => lblStatus.Invoke(() => lblStatus.Text = args.Status);
            Find.SelectedTileChanged += (sender, args) => 
            {
                lblTile1.Invoke(() => {
                    lblTile1.Text = string.Format("B: {0:P4}", args.Brightness);
                });
                pnlTile1.Invoke(() => {
                    DrawTo(pnlTile1, args.Image, args.Area);
                });
            };

            Find.TestedTileChanged += (sender, args) => 
            {
                lblTile2.Invoke(() => {
                    lblTile2.Text = string.Format("B: {0:P4}", args.Brightness);
                });

                pnlTile2.Invoke(() => {
                    DrawTo(pnlTile2, args.Image, args.Area);
                });
            };

            pnlTile1.Paint += (sender, args) =>
            {
                if(_image != null)
                    args.Graphics.DrawImage(_image, new Rectangle(0, 0, _area.Width, _area.Height), _area, GraphicsUnit.Pixel);
            };

            pnlTile2.Paint += (sender, args) =>
            {
                if(_image != null)
                    args.Graphics.DrawImage(_image, new Rectangle(0, 0, _area.Width, _area.Height), _area, GraphicsUnit.Pixel);
            };

            
        }; 

        KeyUp += (n, e) =>
        {
            if(e.KeyCode == Keys.Escape)
            {
                if(Find.Started) Find.Toggle();
            }
        };

        FormClosing += (n, e) =>
        {
            if(Find.Started) Find.Toggle();
            e.Cancel = false;
        };
    }

    static readonly object _locked = new object();

    Bitmap? _image;
    Rectangle _area;
    
    public void DrawTo(Panel pnl, Bitmap image, Rectangle area)
    {
        lock(_locked)
        {
            _image = image;
            _area = area;
            pnl.Refresh();
        }
    }

    public void UpdateUI()
    {
        try
        {
            lblStatus.Text = "Status: {0}";
        }
        catch(Exception err)
        {
            MessageBox.Show(err.Message);
        }
    }
}
