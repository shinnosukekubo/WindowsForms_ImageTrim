using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 切り出す画像サイズ
        /// </summary>
        private Size clipSize = new Size(200, 200);

        /// <summary>
        /// 表示する画像
        /// </summary>
        private Bitmap currentImage;

        /// <summary>
        /// 倍率,位置変更後の画像のサイズと位置
        /// </summary>
        private Rectangle drawRectangle;

        /// <summary>
        /// 画像劣化を防ぐため、出力用の原寸大の画像のサイズと位置
        /// </summary>
        private Rectangle drawDpiRectangle;

        /// <summary>
        /// 画像を移動中か
        /// </summary>
        private bool isDraging = false;

        /// <summary>
        /// 移動前の位置
        /// </summary>
        private Point? diffPoint = null;

        private int dpi = 96;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.pictureBox1.AllowDrop = true;
            this.dpi = (int)this.CreateGraphics().DpiX / 96;
        }

        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            string fileName = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

            // 画像の設定
            this.currentImage = new Bitmap(fileName);

            //中央に設定
            this.drawRectangle = GetPictureBoxCenterRect(this.currentImage.Size);
            this.drawDpiRectangle = GetPictureBoxCenterRect(this.CalcDpiSize(this.currentImage.Size, true));

            // 初期拡大率の設定
            var barCenterValue = (int)((this.trackBar1.Maximum - this.trackBar1.Minimum) / 2);
            this.trackBar1.Value = (int)(barCenterValue * this.GetInitialSizePer());
            this.ChangeTrackBar();

            // Invalidate実行後のOnPaintイベント後に1度だけトリミング処理
            PaintEventHandler handler = null;
            handler = (sender2, e2) =>
            {
                TrimPicture();
                this.pictureBox1.Paint -= handler;
            };
            this.pictureBox1.Paint += handler;
            this.pictureBox1.Invalidate();
        }

        /// <summary>
        /// 画像をトリムします
        /// </summary>
        private void TrimPicture()
        {
            if (this.currentImage == null)
            {
                return;
            }

            Bitmap canvas = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
            Graphics g = Graphics.FromImage(canvas);

            // dpiの関係で元画像の200*200から画像を切り出さないとぼやけるためcurrentImage
            g.DrawImage(this.currentImage, this.drawDpiRectangle);
            g.Dispose();

            // 切り取る位置を真ん中にする
            var clipRect = this.GetPictureBoxCenterRect(this.clipSize);

            var resultImage = this.TrimToBitmap(canvas, this.clipSize, clipRect);

            // プレビューに表示
            this.DrawCustomerRoundImage(this.pictureBox2, resultImage);
        }

        /// <summary>
        /// 画像の一部を切り取った画像を返す
        /// </summary>
        /// <param name="image">元画像</param>
        /// <param name="size">描画サイズ</param>
        /// <param name="rect">切り取り範囲</param>
        /// <returns描画サイズの画像></returns>
        public Bitmap TrimToBitmap(Image image, Size size, Rectangle rect)
        {
            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(size.Width, size.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(canvas);

            //切り取る部分の範囲を決定する
            Rectangle srcRect = rect;

            //描画する部分の範囲を決定する
            Rectangle desRect = new Rectangle(0, 0, size.Width, size.Height);

            //画像の一部を描画する
            g.DrawImage(image, desRect, srcRect, GraphicsUnit.Pixel);

            //Graphicsオブジェクトのリソースを解放する
            g.Dispose();

            return canvas;
        }

        /// <summary>
        /// 円形の画像を表示する
        /// </summary>
        /// <param name="pictureBox">ピクチャーボックス</param>
        /// <param name="image">画像</param>
        public void DrawCustomerRoundImage(PictureBox pictureBox, Image image)
        {
            if (image == null)
            {
                pictureBox.Image = null;
                return;
            }
            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(canvas);

            //楕円の領域を追加する
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(g.VisibleClipBounds);

            //Regionを作成する
            Region rgn = new Region(gp);

            //クリッピング領域を変更する
            g.Clip = rgn;

            //画像を表示する
            g.DrawImage(image, g.VisibleClipBounds);

            //リソースを解放する
            g.Dispose();

            //PictureBox1に表示する
            pictureBox.Image = canvas;
        }

        private void ChangeTrackBar()
        {
            if (this.currentImage == null)
            {
                return;
            }
            var zoomRatio = this.GetZoomRatio();

            //倍率変更後の画像のサイズと位置を計算する
            // 中心を軸に拡縮する
            var diffWidth = (int)Math.Round(this.currentImage.Width * zoomRatio) - this.drawRectangle.Width;
            this.drawRectangle.Width += diffWidth;
            this.drawRectangle.X -= diffWidth / 2;

            var diffHeight = (int)Math.Round(this.currentImage.Height * zoomRatio) - this.drawRectangle.Height;
            this.drawRectangle.Height += diffHeight;
            this.drawRectangle.Y -= diffHeight / 2;


            // 原寸大の画像のRect
            diffWidth = this.CalcDpiSize(diffWidth, true);
            this.drawDpiRectangle.Width += diffWidth;
            this.drawDpiRectangle.X -= diffWidth / 2;

            diffHeight = this.CalcDpiSize(diffHeight, true);
            this.drawDpiRectangle.Height += diffHeight;
            this.drawDpiRectangle.Y -= diffHeight / 2;

            //画像を表示する
            this.pictureBox1.Invalidate();
        }

        /// <summary>
        /// TrackBarの値に対応する拡大率を取得
        /// </summary>
        /// <returns>拡大率</returns>
        private double GetZoomRatio()
        {
            // barの中心で１になるよう2倍
            return 2 * (double)(this.trackBar1.Value) / (double)(this.trackBar1.Maximum - this.trackBar1.Minimum);
        }

        /// <summary>
        /// 画像の初期表示サイズ率を取得します
        /// </summary>
        /// <returns>画像の初期表示サイズ率</returns>
        private double GetInitialSizePer()
        {
            if (this.currentImage.Width < this.pictureBox1.Width
                && this.currentImage.Height < this.pictureBox1.Height)
            {
                return 1;
            }

            // 縦横、サイズが大きいほうの割合に合わせる
            if (this.currentImage.Width > this.currentImage.Height)
            {
                return (double)this.pictureBox1.Width / (double)this.currentImage.Width;
            }
            else
            {
                return (double)this.pictureBox1.Height / (double)this.currentImage.Height;
            }
        }

        /// <summary>
        /// editPictureBoxのセンターRectを取得
        /// </summary>
        /// <returns>初期画像Rect</returns>
        private Rectangle GetPictureBoxCenterRect(Size size)
        {
            var x = (int)(((double)this.pictureBox1.Width / 2) - ((double)size.Width / 2));
            var y = (int)(((double)this.pictureBox1.Height / 2) - ((double)size.Height / 2));
            return new Rectangle(x, y, size.Width, size.Height);
        }

        public int CalcDpiSize(int value, bool IsDivision)
        {
            if (IsDivision)
            {
                return (int)(value / this.dpi);
            }
            else
            {
                return (int)(value * this.dpi);
            }
        }

        public Size CalcDpiSize(Size value, bool IsDivision)
        {
            if (IsDivision)
            {
                return new Size((int)(value.Width / this.dpi), (int)(value.Height / this.dpi));
            }
            else
            {
                return new Size((int)(value.Width * this.dpi), (int)(value.Height * this.dpi));
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.ChangeTrackBar();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (this.currentImage == null)
            {
                return;
            }

            // PictureBoxサイズの画像を作成
            Bitmap canvas = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
            Graphics g = Graphics.FromImage(canvas);

            //画像を指定された位置、サイズでイメージを保持する
            g.DrawImage(this.currentImage, this.drawRectangle);
            g.Dispose();

            //画像を指定された位置、サイズで描画する
            e.Graphics.DrawImage(this.currentImage, this.drawRectangle);

            GraphicsPath path = new GraphicsPath();
            // 切り出す範囲を四角を取得
            Rectangle srcRect = this.GetPictureBoxCenterRect(this.clipSize);

            // 四角に沿った円形を追加
            path.AddEllipse(srcRect);

            // 円に沿ってpenで描画
            Pen pen = new Pen(Color.FromArgb(70, 255, 0, 0), 5);
            e.Graphics.DrawPath(pen, path);
        }

        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            // ドラッグアイコンの変更
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Hand;
            this.isDraging = true;
            this.diffPoint = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.isDraging)
            {
                return;
            }

            this.drawRectangle.X += e.X - this.diffPoint.Value.X;
            this.drawRectangle.Y += e.Y - this.diffPoint.Value.Y;

            // 原寸大の画像の座標
            this.drawDpiRectangle.X += this.CalcDpiSize(e.X - this.diffPoint.Value.X, true);
            this.drawDpiRectangle.Y += this.CalcDpiSize(e.Y - this.diffPoint.Value.Y, true);

            this.diffPoint = new Point(e.X, e.Y);
            this.pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Cursor.Current = Cursors.Default;
            this.isDraging = false;
            TrimPicture();
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            TrimPicture();
        }
    }
}
