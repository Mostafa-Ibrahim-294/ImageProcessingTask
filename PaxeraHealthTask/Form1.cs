using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace PaxeraHealthTask
{
    public partial class Form1 : Form
    {
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 20f;
        private const float ZoomSensitivity = 100f;

        private enum DragMode { None, Pan, Zoom, Draw }

        private Bitmap? originalBitmap;
        private Bitmap? currentBitmap;
        private readonly List<LineSegment> annotations = [];
        private LineSegment? draftLine;

        private float zoomFactor = 1f;
        private PointF panOffset = PointF.Empty;

        private DragMode dragMode = DragMode.None;
        private MouseButtons dragButton;
        private Point lastMousePosition;
        private Point zoomStartMousePosition;
        private float startZoom;

        private float DisplayScale => GetFitScale() * zoomFactor;

        public Form1()
        {
            InitializeComponent();
            EnableDoubleBuffering(imageHolder);
            SetupEvents();
            imageHolder.BackColor = Color.FromArgb(44, 44, 44);
            infoLabel.Text = "No image loaded";
        }

        private void SetupEvents()
        {
            imageHolder.Paint += ImageHolder_Paint;
            imageHolder.Resize += (_, _) => imageHolder.Invalidate();
            imageHolder.MouseDown += ImageHolder_MouseDown;
            imageHolder.MouseMove += ImageHolder_MouseMove;
            imageHolder.MouseUp += ImageHolder_MouseUp;
        }

        private static void EnableDoubleBuffering(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true);
        }

        private void OpenImageButton_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tiff;*.tif|All Files|*.*",
                Title = "Select an image",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            Bitmap loaded;
            try
            {
                using var source = new Bitmap(dialog.FileName);
                loaded = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(loaded);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to load image: {ex.Message}", "Image Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            originalBitmap?.Dispose();
            originalBitmap = loaded;

            currentBitmap?.Dispose();
            currentBitmap = new Bitmap(loaded);

            ResetEditingState();

            var createdTime = File.GetCreationTime(dialog.FileName);
            infoLabel.Text =
                $"Width: {loaded.Width}px | Height: {loaded.Height}px | Created: {createdTime:dd/MM/yyyy HH:mm:ss}";

            imageHolder.Invalidate();
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            if (originalBitmap is null)
            {
                return;
            }

            currentBitmap?.Dispose();
            currentBitmap = new Bitmap(originalBitmap);

            ResetEditingState();
            imageHolder.Invalidate();
        }

        private void ResetEditingState()
        {
            annotations.Clear();
            draftLine = null;
            zoomFactor = 1f;
            panOffset = PointF.Empty;
        }

        private float GetFitScale()
        {
            if (currentBitmap is null || imageHolder.ClientSize.Width <= 0 || imageHolder.ClientSize.Height <= 0)
            {
                return 1f;
            }

            var scaleX = (float)imageHolder.ClientSize.Width / currentBitmap.Width;
            var scaleY = (float)imageHolder.ClientSize.Height / currentBitmap.Height;
            return Math.Min(scaleX, scaleY);
        }

        private RectangleF GetImageDisplayRectangle()
        {
            if (currentBitmap is null)
            {
                return RectangleF.Empty;
            }

            var width = currentBitmap.Width * DisplayScale;
            var height = currentBitmap.Height * DisplayScale;
            var centerX = imageHolder.ClientSize.Width / 2f + panOffset.X;
            var centerY = imageHolder.ClientSize.Height / 2f + panOffset.Y;

            return new RectangleF(centerX - width / 2f, centerY - height / 2f, width, height);
        }

        private PointF ScreenToImage(Point screenPoint)
        {
            var rect = GetImageDisplayRectangle();
            return new PointF(
                (screenPoint.X - rect.Left) / DisplayScale,
                (screenPoint.Y - rect.Top) / DisplayScale);
        }

        private bool IsInsideImage(PointF imagePoint)
        {
            return currentBitmap is not null
                && imagePoint.X >= 0 && imagePoint.Y >= 0
                && imagePoint.X <= currentBitmap.Width && imagePoint.Y <= currentBitmap.Height;
        }

        private PointF ClampToImage(PointF imagePoint)
        {
            if (currentBitmap is null)
            {
                return imagePoint;
            }

            return new PointF(
                Math.Clamp(imagePoint.X, 0f, currentBitmap.Width),
                Math.Clamp(imagePoint.Y, 0f, currentBitmap.Height));
        }

        private void ImageHolder_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(imageHolder.BackColor);

            if (currentBitmap is null)
            {
                return;
            }

            var rect = GetImageDisplayRectangle();

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.TranslateTransform(rect.Left, rect.Top);
            g.ScaleTransform(DisplayScale, DisplayScale);

            g.DrawImage(currentBitmap, 0, 0, currentBitmap.Width, currentBitmap.Height);
            DrawAnnotations(g, currentBitmap);
        }

        private void DrawAnnotations(Graphics g, Bitmap reference)
        {
            var strokeWidth = Math.Max(2f, Math.Max(reference.Width, reference.Height) / 300f);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.Red, strokeWidth);

            foreach (var line in annotations)
            {
                g.DrawLine(pen, line.Start, line.End);
            }

            if (draftLine is { } draft)
            {
                g.DrawLine(pen, draft.Start, draft.End);
            }
        }

        private void ImageHolder_MouseDown(object? sender, MouseEventArgs e)
        {
            if (currentBitmap is null || dragMode != DragMode.None)
            {
                return;
            }

            switch (e.Button)
            {
                case MouseButtons.Left:
                    dragMode = DragMode.Pan;
                    break;

                case MouseButtons.Middle:
                    dragMode = DragMode.Zoom;
                    startZoom = zoomFactor;
                    zoomStartMousePosition = e.Location;
                    break;

                case MouseButtons.Right:
                    var start = ScreenToImage(e.Location);
                    if (!IsInsideImage(start))
                    {
                        return;
                    }

                    dragMode = DragMode.Draw;
                    draftLine = new LineSegment(start, start);
                    break;

                default:
                    return;
            }

            dragButton = e.Button;
            lastMousePosition = e.Location;
            imageHolder.Capture = true;
        }

        private void ImageHolder_MouseMove(object? sender, MouseEventArgs e)
        {
            switch (dragMode)
            {
                case DragMode.Pan:
                    panOffset = new PointF(
                        panOffset.X + (e.X - lastMousePosition.X),
                        panOffset.Y + (e.Y - lastMousePosition.Y));
                    lastMousePosition = e.Location;
                    break;

                case DragMode.Zoom:
                    var deltaY = zoomStartMousePosition.Y - e.Y;
                    zoomFactor = Math.Clamp(
                        startZoom * MathF.Pow(2f, deltaY / ZoomSensitivity),
                        MinZoom,
                        MaxZoom);
                    break;

                case DragMode.Draw when draftLine is { } draft:
                    draftLine = new LineSegment(draft.Start, ClampToImage(ScreenToImage(e.Location)));
                    break;

                default:
                    return;
            }

            imageHolder.Invalidate();
        }

        private void ImageHolder_MouseUp(object? sender, MouseEventArgs e)
        {
            if (dragMode == DragMode.None || e.Button != dragButton)
            {
                return;
            }

            if (dragMode == DragMode.Draw && draftLine is { } line && line.Start != line.End)
            {
                annotations.Add(line);
            }

            draftLine = null;
            dragMode = DragMode.None;
            imageHolder.Capture = false;
            imageHolder.Invalidate();
        }

        private void InvertFilterButton_Click(object sender, EventArgs e)
        {
            if (currentBitmap is not null)
            {
                ReplaceCurrentBitmap(ImageFilters.Invert(currentBitmap));
            }
        }

        private void GrayscaleFilterButton_Click(object sender, EventArgs e)
        {
            if (currentBitmap is not null)
            {
                ReplaceCurrentBitmap(ImageFilters.Grayscale(currentBitmap));
            }
        }

        private void ReplaceCurrentBitmap(Bitmap newBitmap)
        {
            currentBitmap?.Dispose();
            currentBitmap = newBitmap;
            imageHolder.Invalidate();
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
            if (currentBitmap is null)
            {
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|Bitmap Image|*.bmp|JPEG Image|*.jpg;*.jpeg|GIF Image|*.gif|TIFF Image|*.tif;*.tiff",
                Title = "Save current image",
                DefaultExt = "png",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                using var finalImage = CreateSavedImage(currentBitmap);
                finalImage.Save(dialog.FileName, GetImageFormat(Path.GetExtension(dialog.FileName)));
                MessageBox.Show(this, "Image saved successfully.", "Save Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to save image: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap CreateSavedImage(Bitmap source)
        {
            var saved = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(saved);
            g.DrawImage(source, 0, 0, source.Width, source.Height);
            DrawAnnotations(g, source);
            return saved;
        }

        private static ImageFormat GetImageFormat(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".bmp" => ImageFormat.Bmp,
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".gif" => ImageFormat.Gif,
                ".tif" or ".tiff" => ImageFormat.Tiff,
                _ => ImageFormat.Png,
            };
        }
    }

    public readonly record struct LineSegment(PointF Start, PointF End);
}