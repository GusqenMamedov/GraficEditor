using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GraphicsEditor
{
    public partial class MainGraphicEditorForm : Form
    {
        //определяет текущий режим рисования
        public DrawingMode currentMode;
        //была ли нажата клавиша мыши
        public bool isWasMouseDown = false;
        //начальная позиция мыши
        public Point oldPosition;
        GraphicsPath currPath;
        BackgroundWorker bgWorker;

        public MainGraphicEditorForm()
        {
            InitializeComponent();
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            bgWorker.ProgressChanged += new ProgressChangedEventHandler
                    (backgroundWorker_ProgressChanged);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (backgroundWorker_RunWorkerCompleted);
            bgWorker.WorkerReportsProgress = true;
            bgWorker.WorkerSupportsCancellation = true;
        }

        private void btnPencil_Click(object sender, EventArgs e)
        {
            currentMode = DrawingMode.Pencil;
            btnBackColor.Enabled = false;
        }

        private void btnLine_Click(object sender, EventArgs e)
        {
            currentMode = DrawingMode.Line;
            btnBackColor.Enabled = false;
        }

        private void btnRectangle_Click(object sender, EventArgs e)
        {
            currentMode = DrawingMode.Rectangle;
            btnBackColor.Enabled = true;
        }

        private void btnCircle_Click(object sender, EventArgs e)
        {
            currentMode = DrawingMode.Circle;
            btnBackColor.Enabled = true;
        }

        private void btnBorderColor_Click(object sender, EventArgs e)
        {
            //выбор цвета для рисования из открывшегося диалогового окна
            //меняется цвет btnBorderColor
            ColorDialog dlgChooseColorDraw = new ColorDialog();
            dlgChooseColorDraw.Color = btnBorderColor.BackColor;

            if (dlgChooseColorDraw.ShowDialog() == DialogResult.OK)
            {
                btnBorderColor.BackColor = dlgChooseColorDraw.Color;
            }
        }

        private void btnBackColor_Click(object sender, EventArgs e)
        {
            //выбор заливки доступен только в режиме рисования прямоугольника или круга
            if (currentMode == DrawingMode.Rectangle || currentMode == DrawingMode.Circle)
            {
                ColorDialog dlgChooseColorDraw = new ColorDialog();
                dlgChooseColorDraw.Color = btnBackColor.BackColor;

                if (dlgChooseColorDraw.ShowDialog() == DialogResult.OK)
                {
                    btnBackColor.BackColor = dlgChooseColorDraw.Color;
                }
            }
        }

        private void MainGraphicEditorForm_Load(object sender, EventArgs e)
        {
            currentMode = DrawingMode.Pencil;
            btnBackColor.Enabled = false;
            pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
        }

        /// <summary>
        /// загрузка из файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog();
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                pictureBox.Image = Image.FromFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// Сохранение в файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "PNG(*.png)|*.png|BMP(*.bmp)|*.bmp|GIF (*.gif)|*.gif|JPEG (*.jpg)|*.jpg";
            saveDlg.AddExtension = true;
            saveDlg.OverwritePrompt = true;

            if (saveDlg.ShowDialog() == DialogResult.OK 
                && !saveDlg.FileName.Equals(String.Empty))
            {
                ImageFormat format = ImageFormat.Png; ;
                string extension = System.IO.Path.GetExtension(saveDlg.FileName);
                switch (extension)
                {
                    case ".jpg":
                        {
                            format = ImageFormat.Jpeg;
                            break;
                        }
                    case ".bmp":
                        {
                            format = ImageFormat.Bmp;
                            break;
                        }
                    case ".gif":
                        {
                            format = ImageFormat.Gif;
                            break;
                        }
                    case ".png":
                        {
                            format = ImageFormat.Png;
                            break;
                        }
                }

                //this.Text = saveDlg.FileName;
                pictureBox.Image.Save(saveDlg.FileName,format);
            }
        }

        private void mnuInvertImg_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            bgWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Обработчик нажатия левой мыши по pictureBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                isWasMouseDown = true;
                oldPosition = e.Location;

                //создаем объект новой фигуры которую будем рисовать
                GraphicsPath path = new GraphicsPath();
                currPath = path;
            }
        }

        /// <summary>
        /// Обработчик движения мыши по элементу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            //обрабатывает только если была нажата левая клавиша мыши раннее
            if (isWasMouseDown)
            {
                //последний элемент в коллекции тот что создается сейчас
                //GraphicsPath path = allDrawingFigures[allDrawingFigures.Count - 1].Path;
                switch (currentMode)
                {
                    case DrawingMode.Pencil:
                    {
                        currPath.AddLine(oldPosition, e.Location);
                        oldPosition = e.Location;
                        break;
                    }
                    case DrawingMode.Line:
                    {
                        //при рисовании линии нам важна только линия соед. нач и кон.точку
                        //линии между нач. и промежуточными точками при движении не должны рисоваться
                        //аналогично и прямоугольник и круг - только нач.точка с коненой
                        currPath.Reset();
                        currPath.AddLine(oldPosition, e.Location);
                        break;
                    }
                    case DrawingMode.Rectangle:
                    {
                        currPath.Reset();
                        Rectangle rect = DefineRect(oldPosition, e.Location);
                        //Rectangle rect = 
                        //    new Rectangle(e.Location, new Size(e.Location.X - oldPosition.X, e.Location.Y - oldPosition.Y));
                        currPath.AddRectangle(rect);
                        break;
                    }
                    case DrawingMode.Circle:
                    {
                        currPath.Reset();
                        Rectangle rect = DefineRect(oldPosition, e.Location,true);
                        //Rectangle rect =
                        //    new Rectangle(e.Location, new Size(e.Location.X - oldPosition.X, e.Location.Y - oldPosition.Y));
                        currPath.AddEllipse(rect);
                        break;
                    }
                }

                pictureBox.Refresh(); 
            }
            
        }

        /// <summary>
        /// Обработчик отпуска клавиши мыши
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isWasMouseDown)
            {
                //нарисуем последнюю фигуру на самом изображении а не на элементе pictureBox
                //?? просто если рисовать все как в pictureBox_Paint то само изображение не изменяется
                Graphics gr = Graphics.FromImage(pictureBox.Image);
                gr.DrawPath(new Pen(btnBorderColor.BackColor, 3), currPath);
                if (currentMode == DrawingMode.Rectangle || currentMode == DrawingMode.Circle)
                    gr.FillPath(new SolidBrush(btnBackColor.BackColor), currPath);
            
            }
            //завершаем рисование
            isWasMouseDown = false;
        }

        /// <summary>
        /// Отрисовка эелемента pictureBox,с одержащего изображение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!isWasMouseDown) return;

            //рисуем только последнюю новую фигуру
            e.Graphics.DrawPath(new Pen(btnBorderColor.BackColor, 3), currPath);
            if (currentMode == DrawingMode.Rectangle || currentMode == DrawingMode.Circle)
                e.Graphics.FillPath(new SolidBrush(btnBackColor.BackColor), currPath);
        }

        /// <summary>
        /// Определение прямоугольной области длия рисования фигуры
        /// </summary>
        /// <param name="oldPos"></param>
        /// <param name="newPos"></param>
        /// <param name="defSquare">признак рисуем область для прямоугольника или квадрата</param>
        /// <returns></returns>
        private Rectangle DefineRect(Point oldPos, Point newPos, bool defSquare = false)
        {
            int deltaX = Math.Abs(newPos.X - oldPos.X),
                deltaY = Math.Abs(newPos.Y - oldPos.Y);
            Rectangle rect = new Rectangle();
            if (defSquare) deltaY = deltaX;
            rect.Size = new Size(deltaX,deltaY);

            if (newPos.X > oldPos.X)
            {
                if (newPos.Y < oldPos.Y) { rect.X = oldPos.X; rect.Y = defSquare ? (oldPos.Y - deltaY): newPos.Y; }
                else { rect.X = oldPos.X; rect.Y =  oldPos.Y; }
            }
            else
            {
                if (newPos.Y < oldPos.Y) { rect.X = newPos.X; rect.Y = defSquare ? (oldPos.Y - deltaY) : newPos.Y; }
                else { rect.X = newPos.X; rect.Y = oldPos.Y; }
            }
            
            return rect;
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bmp = new Bitmap(pictureBox.Image);
            int size = bmp.Size.Width * bmp.Size.Height;
            
            //число обработанных пикселе (можно перемножить (Х-1)*У +У )? но я ночью уже туго соображала=)
            int k = 0;
            for (int x = 0; x <= bmp.Width - 1; x++)
            {
                for (int y = 0; y <= bmp.Height - 1; y += 1)
                {
                    k++;
                    Color oldColor = bmp.GetPixel(x, y);
                    Color newColor;
                    //заменяем вет на противоположны1
                    newColor = Color.FromArgb(oldColor.A, 255 - oldColor.R, 255 - oldColor.G, 255 - oldColor.B);
                    bmp.SetPixel(x, y, newColor);
                    
                    bgWorker.ReportProgress(k*100/size);
                    if (bgWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        bgWorker.ReportProgress(0);
                        return;
                    }
                }
            }

            bgWorker.ReportProgress(100);
            pictureBox.Image = bmp;
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            //lbPercent.Text = e.ProgressPercentage.ToString() + "%";
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //progressBar1.Value = 0;
        }
    }

    /// <summary>
    /// Режимы рисования
    /// </summary>
    public enum DrawingMode
    {
        /// <summary>
        /// Рисование карандашом
        /// </summary>
        [Description("Рисование инструментом карандаш")]
        Pencil = 0,
        /// <summary>
        /// Рисование прямой линии
        /// </summary>
        [Description("Рисование инструментом прямая линия")]
        Line = 1,
        /// <summary>
        /// Рисование прямоугольника
        /// </summary>
        [Description("Рисование инструментом прямоугольник")]
        Rectangle = 2,
        /// <summary>
        /// Рисование круга
        /// </summary>
        [Description("Рисование инструментом круг")]
        Circle = 3
    }

}
