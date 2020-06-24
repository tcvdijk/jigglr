using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace jigglr
{
    public partial class Main : Form
    {

        private Timer jiggleTimer;

        enum State { Drawing, Editing };
        State state = State.Drawing;

        public Main()
        {
            InitializeComponent();
            poly.importImage( viewport.Image);
            closePen.DashPattern = new float[] { 4.0F, 4.0F };

            jiggleTimer = new Timer();
            jiggleTimer.Tick += new EventHandler(jiggleTick);
            jiggleTimer.Interval = 1; // in miliseconds; go max speed
            jiggleTimer.Start();
            frameStopwatch.Start();

            double.TryParse(jiggleAmount.Text, out searchSize);

        }

        Stopwatch frameStopwatch = new Stopwatch();
        long frametime;

        private void jiggleTick(object sender, EventArgs e)
        {
            if (state==State.Editing)
            {
                jiggle( poly.ps.Count );
                viewport.Invalidate();
            }
            frametime = frameStopwatch.ElapsedMilliseconds;
            frameStopwatch.Restart();
        }

        Pen pen = new Pen(Color.Red, 3);
        Pen closePen = new Pen(Color.Yellow, 3);
        Pen backgroundPen = new Pen(Color.LightBlue, 2);

        Brush brush = Brushes.Red;
        Font font = SystemFonts.DefaultFont;

        Polygon poly = new Polygon();
        bool dragging = false;
        int draggingIndex = -1;

        Random rand = new Random();

        private double searchSize = 20;

        private int nodeProgress = 0;

        private bool searchStep()
        {
            double oldScore = poly.eval();
            int maxI = poly.ps.Count;
            //if (state==State.Drawing) maxI -= 2;
            //int i = rand.Next(poly.ps.Count);
            ++nodeProgress;
            if (nodeProgress >= poly.ps.Count) nodeProgress = 0;
            int i = nodeProgress;
            //if (state == State.Drawing) i += 1;
            if (i >= poly.ps.Count) return false;
            PointF oldP = poly.ps[i];
            PointF newP = new PointF(
                (float)(oldP.X + searchSize * rand.NextDouble() - 0.5 * searchSize),
                (float)(oldP.Y + searchSize * rand.NextDouble() - 0.5 * searchSize)
                );
            poly.ps[i] = newP;
            double newScore = poly.eval();
            if (newScore > oldScore)
            {
                poly.ps[i] = oldP;
                return false;
            }
            return true;
        }

        private bool searchStep2()
        {
            double oldScore = poly.eval();
            int maxI = poly.ps.Count;
            //if (state==State.Drawing) maxI -= 2;
            //int i = rand.Next(poly.ps.Count);
            ++nodeProgress;
            if( nodeProgress >= poly.ps.Count ) nodeProgress = 0;
            int i = nodeProgress;
            i = rand.Next(poly.ps.Count);
            if (i >= poly.ps.Count) return false;
            bool changedSomething = false;
            int W = 2;
            for (int dx = -W; dx <= W; ++dx)
            {
                for (int dy = -W; dy <= W; ++dy)
                {
                    PointF oldP = poly.ps[i];
                    PointF newP = new PointF(
                        (float)(oldP.X + dx),
                        (float)(oldP.Y + dy)
                        );
                    if (newP.X < 0) continue;
                    if (newP.Y < 0) continue;
                    if (newP.X >= viewport.Image.Width) continue;
                    if (newP.Y >= viewport.Image.Height) continue;
                    poly.ps[i] = newP;
                    double newScore = poly.eval();
                    if (newScore > oldScore)
                    {
                        poly.ps[i] = oldP;
                    }
                    else
                    {
                        changedSomething = true;
                    }
                }
            }
            return changedSomething;            
        }

        private void jiggle( int maxSteps )
        {
            int failCount = 0;
            for (int round = 0; round < maxSteps; ++round)
            {
                if (searchStep() )
                {
                    failCount = 0;
                }
                else
                {
                    ++failCount;
                }
                if (failCount > 30) break;
            }
        }

        public static double dist(PointF p, PointF q )
        {
            double dx = q.X - p.X;
            double dy = q.Y - p.Y;
            return Math.Sqrt( dx * dx + dy * dy );
        }

        private void viewport_MouseDown(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left)
            {
                PointF click = new PointF(args.X, args.Y);
                switch (state)
                {
                    case State.Drawing:
                        if (poly.ps.Count == 0)
                        {
                            poly.ps.Add(click);
                            dragging = true;
                            draggingIndex = poly.ps.Count - 1;
                        }
                        else
                        {
                            if (dist(click, poly.ps[0]) < 50)
                            {
                                state = State.Editing;
                            }
                            else
                            {
                                poly.ps.Add(click);
                                dragging = true;
                                draggingIndex = poly.ps.Count - 1;
                            }
                        }
                        break;
                    case State.Editing:
                        double closest = 40;
                        draggingIndex = -1;
                        for (int i = 0; i < poly.ps.Count; ++i)
                        {
                            double myDist = dist(click, poly.ps[i]);
                            if (myDist<closest)
                            {
                                dragging = true;
                                draggingIndex = i;
                                closest = myDist;
                            }
                        }
                        if( draggingIndex >= 0 ) return;
                        // we didn't click on a vertex.
                        // did we click on an edge?
                        for (int i = 1; i < poly.ps.Count; ++i)
                        {
                            if (poly.pointNearEdge(i - 1, i, click))
                            {
                                poly.ps.Insert(i, click);
                                dragging = true;
                                draggingIndex = i;
                                return;
                            }
                        }
                        if (poly.pointNearEdge(0, poly.ps.Count-1, click))
                        {
                            draggingIndex = poly.ps.Count;
                            poly.ps.Insert(poly.ps.Count, click);
                            dragging = true;
                        }
                        
                        break;
                }
            }
            viewport.Invalidate();
        }

        private void viewport_MouseMove(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left && dragging )
            {
                poly.ps[draggingIndex] = new PointF(args.X, args.Y);
            }
            viewport.Invalidate();
        }

        private void viewport_MouseUp(object sender, MouseEventArgs args)
        {
            dragging = false;
            draggingIndex = -1;
            if (args.Button == MouseButtons.Right)
            {
                int nearest = poly.findNearest(new PointF(args.X,args.Y), 30);
                if (nearest >= 0 && poly.ps.Count>1 )
                {
                    poly.ps.RemoveAt(nearest);
                }
            }
            viewport.Invalidate();
        }

        private void clear_Click(object sender, EventArgs e)
        {
            reset();
        }

        private void reset()
        {
            poly.ps.Clear();
            state = State.Drawing;
            viewport.Invalidate();
        }

        private Pen qualityPen(PointF p, PointF q)
        {
            double score = poly.evalSegment(p, q);
            score -= poly.minScore;
            score /= poly.maxScore - poly.minScore;
            score *= 1.5;
            score -= 0.25;
            int c = (int)(255 * score);
            if (c < 0) c = 0;
            if(c>255)c=255;
            return new Pen(Color.FromArgb(64 + 3 * (c / 4), 255 - c, 0), 5);
        }

        private void viewport_Paint(object sender, PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.DrawString("" + poly.eval(), font, brush, 50, 50);
            gr.DrawString("" + frametime + " ms", font, brush, 50, 80);
            if (state == State.Drawing)
            {
                gr.DrawString("DRAWING", font, brush, 50, 20);
                if (poly.ps.Count > 0)
                {
                    PointF prev = poly.ps[0];
                    gr.DrawEllipse(backgroundPen, prev.X - 40, prev.Y - 40, 80, 80);
                    for (int i = 1; i < poly.ps.Count; ++i)
                    {
                        gr.DrawLine(qualityPen (prev,poly.ps[i]), prev, poly.ps[i]);
                        prev = poly.ps[i];
                    }
                    gr.DrawLine(closePen, prev, poly.ps[0]);
                    foreach( PointF p in poly.ps ) {
                        gr.DrawEllipse(pen, p.X - 10, p.Y - 10, 20, 20);
                    }
                    if (dragging)
                    {
                        float x = poly.ps[draggingIndex].X;
                        float y = poly.ps[draggingIndex].Y;
                        gr.DrawEllipse(pen, x - 20, y - 20, 40, 40);
                    }

                }
            }
            else if (state == State.Editing)
            {
                gr.DrawString("EDITING", font, brush, 50, 20);

                PointF prev = poly.ps[0];
                for (int i = 1; i < poly.ps.Count; ++i)
                {
                    gr.DrawLine(qualityPen(prev,poly.ps[i]), prev, poly.ps[i]);
                    prev = poly.ps[i];
                }
                gr.DrawLine(qualityPen(prev,poly.ps[0]), prev, poly.ps[0]);
                foreach (PointF p in poly.ps)
                {
                    gr.DrawEllipse(pen, p.X - 10, p.Y - 10, 20, 20);
                }
                if (dragging)
                {
                    float x = poly.ps[draggingIndex].X;
                    float y = poly.ps[draggingIndex].Y;
                    gr.DrawEllipse(pen, x - 20, y - 20, 40, 40);
                }
            }
        }

        private void load_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Open image...";
            theDialog.Filter = "Image files|*";
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                viewport.Image = Image.FromFile(theDialog.FileName.ToString());
                poly.ps.Clear();
                poly.importImage(viewport.Image);
                reset();
            }
        }

        private void jiggleAmount_SelectedValueChanged(object sender, EventArgs e)
        {
            double.TryParse(jiggleAmount.Text, out searchSize);
        }

        private void viewport_Click(object sender, EventArgs e)
        {

        }
    }

    class Polygon {
        public List<PointF> ps = new List<PointF>();
        private int imageWidth, imageHeight;
        public int findNearest(PointF p, double maxDist)
        {
            int result = -1;
            for (int i = 0; i < ps.Count; ++i)
            {
                double d = Main.dist(p, ps[i]);
                if (d < maxDist)
                {
                    result = i;
                    maxDist = d;
                }
            }
            return result;
        }
        public bool pointNearEdge(int i, int j, PointF click)
        {
            PointF p = ps[i];
            PointF q = ps[j];
            PointF e = new PointF(q.X - p.X, q.Y - p.Y);
            float eLen = (float)Main.dist(new PointF(0, 0), e);
            e = new PointF(e.X / eLen, e.Y / eLen);
            PointF ePerp = new PointF(-e.Y, e.X);
            PointF c = new PointF(click.X - p.X, click.Y - p.Y);

            double along = dot(e, c);
            double offset = dot(ePerp, c);

            if (along < 0) return false;
            if (along > eLen) return false;
            if (offset < -20) return false;
            if (offset > 20) return false;
            return true;
        }
        private static double dot(PointF p, PointF q)
        {
            return p.X * q.X + p.Y * q.Y;
        }
        public double eval()
        {
            if (ps.Count < 3) return 0;
            double score = 0.0;
            double len = 0.0;
            for( int i=1; i<ps.Count; ++i ) {
                score += evalSegment(ps[i - 1], ps[i]);
                len += 1; // Main.dist(ps[i - 1], ps[i]);
            }
            score += evalSegment(ps[ps.Count - 1], ps[0]);
            len += 1; // Main.dist(ps[ps.Count - 1], ps[0]);
            return score / len;
        }
        public double evalSegment(int i, int j)
        {
            return evalSegment(ps[i], ps[j]);
        }
        public double evalSegment(PointF p, PointF q)
        {
            double score = 0.0;
            double max = 0.0;
            int n = 0;
            for (double a = 0.0; a <= 1.0; a += 0.01)
            {
                double here = evalPoint(lerp(p, q, a));
                score += here;
                if (here > max) max = here;
                ++n;
            }
            return 0.5 * ( (score / n) + max );
        }
        private double evalSegmentAvg(PointF p, PointF q)
        {
            double score = 0.0;
            int n = 0;
            for (double a = 0.0; a <= 1.0; a += 0.1)
            {
                score += evalPoint(lerp(p, q, a));
                ++n;
            }
            return score / n;
        }
        private double evalPoint(PointF p)
        {
            try
            {
                return score[(int)p.X, (int)p.Y];
            }
            catch (IndexOutOfRangeException e) { return 1; }
        }
        private PointF lerp( PointF p, PointF q, double a ) {
            return new PointF(
                (float)((1 - a) * (double)p.X + a * (double)q.X),
                (float)((1 - a) * (double)p.Y + a * (double)q.Y)
                );
        }

        private double[,] score;
        public double minScore, maxScore;
        public void importImage(Image im)
        {
            minScore = 1;
            maxScore = 0;
            Bitmap bm = new Bitmap(im);
            imageWidth = im.Width;
            imageHeight = im.Height;
            score = new double[imageWidth, imageHeight];
            for (int y = 0; y < imageHeight; ++y)
            {
                for (int x = 0; x < imageWidth; ++x)
                {
                    double s = bm.GetPixel(x, y).GetBrightness();
                    score[x, y] = s;
                    if (s < minScore) minScore = s;
                    if (s > maxScore) maxScore = s;
                }
            }
            filter( 20 );
        }

        private double lerp(double x, double y, double a)
        {
            return (1 - a) * x + a * y;
        }
        private void filter( int radius )
        {
            double[,] temp = new double[imageWidth,imageHeight];
            for (int x = 0; x < imageWidth; ++x)
            {
                for (int y = radius; y < imageHeight - radius; ++y)
                {
                    double original = score[x,y];
                    double best = original;
                    for (int dy = -radius; dy <= radius; ++dy)
                    {
                        double a = Math.Abs(dy) / (double)radius;
                        //a = Math.Sqrt(a);
                        double here = lerp(score[x, y + dy], original, a);
                        if (here < best) best = here;
                    }
                    temp[x, y] = best;
                }
            }
            for (int y = 0; y < imageHeight; ++y)
            {
                for (int x = radius; x < imageWidth - radius; ++x)
                {
                    double original = temp[x, y];
                    double best = original;
                    for (int dx = -radius; dx <= radius; ++dx)
                    {
                        double a = Math.Abs(dx) / (double)radius;
                        //a = Math.Sqrt(a);
                        double here = lerp(temp[x+dx, y], original, a );
                        if (here < best) best = here;
                    }
                    score[x, y] = (score[x,y] + best ) / 2;
                }
            }
            Bitmap b = new Bitmap(imageWidth, imageHeight,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int y = 0; y < imageHeight; ++y)
            {
                for (int x = 0; x < imageWidth; ++x)
                {
                    int val = (int)(255*score[x,y]);
                    b.SetPixel(x,y,Color.FromArgb(val,val,val));
                }
            }
            b.Save("debug.png");
        }

    }

}
