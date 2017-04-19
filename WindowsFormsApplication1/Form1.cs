using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;


// Line 278 . Line 419 

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        //宣告要用的物件
        Pen p; 
        Bitmap b;
        Brush aBrush;
        int lines = 0;
        Stopwatch sw;

        static Random random = new Random();
        static double Compare = 0.006 ;      //與隨機選擇城市機率比較的參數
        static double Q  = 5.0;
        static double Alpha = 0.4 ;
        static double Beta = 2.0 ;
        static double rho  = 0.2;
        static int[] Part_Best_Visited_Trail;
        static int[] Best_Visited_Trail;     //試做最佳探訪城市順序，不先做初始化
        //static int[] number;
        static int[] x = null;
        static int[] y = null;
        static double[,] Dist;
        static double BestTrail;
        static int numAnts;
        static int maxtime;

        static int firstCity(int numcity)
        {
            return random.Next(0, numcity);
        }
        static double[,] distance(int[] cityX, int[] cityY)
        {
            double[,] dist = new double[cityX.Length, cityY.Length];
            for (var i = 0; i < cityX.Length; i++)
            {
                for (var j = 0; j < cityY.Length; j++)
                {
                    dist[i, j] = Math.Sqrt(Math.Pow(cityX[i] - cityX[j], 2) + (Math.Pow(cityY[i] - cityY[j], 2)));
                }
            }
            return dist;  //計算城市與城市之間距離，回傳
        }
        static int[,] initCityVisited(int[,] Visited)
        {
            Array.Clear(Visited, 0, Visited.Length);
            return Visited;
        }

        /*初始化費洛蒙
        以一座城市到另一城市距離/到其他城市的總距離*/
        static double[,] initpheromones(double[,] Dist)
        {
            double DistSum = 0;
            double[,] pher = new double[Dist.GetLength(0), Dist.GetLength(1)];
            for (var i = 0; i < Dist.GetLength(0); i++)
            {
                for (var j = 0; j < Dist.GetLength(1); j++) { DistSum += Dist[i, j]; }
                for (var j = 0; j < Dist.GetLength(1); j++)
                {
                    pher[i, j] = 0.01;
                    //pher[i,j] = (1*Dist[i, j] / DistSum)*10;
                    if (i == j) { pher[i, j] = 0; }
                    //Console.WriteLine("第" + i + "到第" + j + "的費洛蒙濃度為" + pher[i, j]);
                }
            }
            return pher;
        }
        //前往下一座城市
        static int GoToNextCity(double[,] dist, double[,] pheromones, int currentcity, bool[,] checkcity, int whichant, double compare, double alpha, double beta)
        {
            double sum_prob = 0;
            double[,] every_prob = new double[pheromones.GetLength(0), pheromones.GetLength(1)];
            int nextcity = dist.GetLength(1) + 1;
            double max_every_prob = 0;
            int max_prob_city = dist.GetLength(1) + 1;
            for (var i = 0; i < checkcity.GetLength(1); i++)
            {
                if (checkcity[whichant, i] == true)     //如果城市走過了，就不列入計算
                {
                    every_prob[currentcity, i] = 0;
                }
                else
                {
                    double formula = (Math.Pow(pheromones[currentcity, i], Alpha)) * (Math.Pow(1 / dist[currentcity, i], Beta));
                    every_prob[currentcity, i] = formula;
                    sum_prob += every_prob[currentcity, i];
                }
                if (every_prob[currentcity, i] > max_every_prob)
                {
                    max_every_prob = every_prob[currentcity, i];
                    max_prob_city = i;
                }
            }
            double random_prob = randomNextCity_prob(max_every_prob);
            if (random_prob > compare)
            {
                double closeto_prob = max_every_prob;
                for (var i = 0; i < every_prob.GetLength(1); i++)
                {

                    if (every_prob[currentcity, i] > random_prob)
                    {
                        if (every_prob[currentcity, i] <= closeto_prob)
                        {
                            closeto_prob = every_prob[currentcity, i];
                            nextcity = i;
                        }
                    }
                }
            }
            else
            {
                nextcity = max_prob_city;
            }
            return nextcity;
        }
        //每隻螞蟻的探訪的距離總和
        static double[] Distsum(int[,] visited, double[,] dist)
        {
            double[] AntLength = new double[visited.GetLength(0)];
            for (var i = 0; i < visited.GetLength(0); i++)
            {
                for (var j = 0; j < visited.GetLength(1) - 1; j++)
                { AntLength[i] += dist[visited[i, j], visited[i, j + 1]]; }
            }
            return AntLength;
        }
        //更新最短距離
        static double besttrail(double[] antlength, double trail, int[,] visited)
        {
            double part_best_antlength = antlength.Max();
            for (var i = 0; i < antlength.Length; i++)
            {
                if (antlength[i] <= part_best_antlength)
                {
                    part_best_antlength = antlength[i];
                    for (var j = 0; j < visited.GetLength(1); j++)
                    {
                        Part_Best_Visited_Trail[j] = visited[i, j];      //記錄一次循環中，最好的那隻螞蟻的路徑
                    }
                }
                if (trail == 0)
                {
                    trail = antlength[i];
                    for (var j = 0; j < Best_Visited_Trail.Length; j++)
                    {
                        Best_Visited_Trail[j] = visited[i, j];
                    }
                }
                else if (trail > antlength[i])
                {
                    trail = antlength[i];
                    for (var j = 0; j < Best_Visited_Trail.Length; j++)
                    {
                        Best_Visited_Trail[j] = visited[i, j];
                    }
                }
            }
            return trail;
        }
        //區域費洛蒙更新
        static double partofupdatepheromones(double part_update_pher, double rho, double dist, int cities)
        {
            part_update_pher = rho * part_update_pher + (1 - rho) * (1 / (cities * dist));
            return part_update_pher;
        }
        //整體費洛蒙更新
        static double[,] updatepheromones(double[,] pheromones, int[,] visited, double[,] dist, double rho, double Q)
        {
            int[,] VisitedLength = new int[pheromones.GetLength(0), pheromones.GetLength(1)];    //記錄哪兩座城市之間有被走過幾次
            Array.Clear(VisitedLength, 0, VisitedLength.Length);      //先將VisitedLength清空
            double low = 0.00001;
            double high = 1.000;

            //只選擇本次循環中路徑最短的螞蟻的尋訪路徑作為費洛蒙更新的依據
            for (var i = 0; i < Part_Best_Visited_Trail.Length - 1; i++)
            {
                VisitedLength[Part_Best_Visited_Trail[i], Part_Best_Visited_Trail[i + 1]] += 1;
                VisitedLength[Part_Best_Visited_Trail[i + 1], Part_Best_Visited_Trail[i]] += 1;
            }

            //將所有螞蟻的尋訪路徑作為費洛蒙更新的依據
            /*for (var i = 0; i < visited.GetLength(0); i++)
            {
                for (var j = 0; j < visited.GetLength(1)-1; j++)
                {
                    VisitedLength[visited[i, j], visited[i, j + 1]] += 1;
                    VisitedLength[visited[i, j + 1], visited[i, j]] += 1;
                }
            }*/
            for (var i = 0; i < pheromones.GetLength(0); i++)
            {
                for (var j = 0; j < pheromones.GetLength(1); j++)
                {
                    if (i == j)
                    {
                        pheromones[i, j] = 0;
                    }
                    else
                    {
                        pheromones[i, j] = rho * pheromones[i, j] + Q / dist[i, j] * VisitedLength[i, j] * (1 - rho);
                        if (pheromones[i, j] < low) { pheromones[i, j] = low; }
                        if (pheromones[i, j] > high) { pheromones[i, j] = high; }
                    }

                }
            }
            return pheromones;
        }
        //選擇下一座城市的機率
        static double randomNextCity_prob(double max_random_prob)
        {
            int k = 10000000;
            double random_prob = max_random_prob * k;
            random_prob = random.Next(0, (int)random_prob);
            return random_prob / k;
        }

        public Form1()
        {
            InitializeComponent();
        }

        
        private void pixel(Graphics g, Brush b, int x1, int y1, int w, int h)
        {
            g.FillRectangle(b, x1, y1, w, h); // 畫點坐標，不能使用DrawLine
        }

        private void line(Graphics g, Brush b, int x1, int y1, int x2, int y2)
        {
            g.DrawLine(p, x1, y1, x2, y2); //畫線，先擺著
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                loadfile();
                //b = new Bitmap(x.Max(), y.Max());
                button2.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                
            }
            catch (Exception)
            {
                coordinate_list.Items.Clear();
                coordinate_list.Items.Add("請填寫完整");
            }
            //coordinate_list.Items.Clear(); //先清除再讀取

            /*for (int i = 0; i < number.Length; i++)
            {
                coordinate_list.Items.Add("Node" + number[i].ToString() + " : " + x[i].ToString() + " , " + y[i].ToString());
            }*/
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            b = new Bitmap(coordinate_show.Width, coordinate_show.Height);
            try
            {
                for (int i = 0; i < x.Length; i++)  //傳值至pixel中
                {
                    //----------------------------Here--------------------------------
                    if (x.Length == 51)
                    { pixel(Graphics.FromImage(b), aBrush, x[i] * 7 - 4, 500 - y[i] * 7 - 4, 9, 9); }
                    else
                    { pixel(Graphics.FromImage(b), aBrush, x[i] /8 +50 , 500 - y[i] / 8, 3, 3); }
                    //--------------------------------------------------------------------
                }
                coordinate_show.Image = b; // b=bitmap 在剛才的迴圈中已經處理了坐標點
                button4.Enabled = true;
            }
            catch(Exception)
            {
                coordinate_list.Items.Clear();
                coordinate_list.Items.Add("尚未讀取檔案");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           aBrush = (Brush)Brushes.Red; // Form讀取時更改物件的值
           p = new Pen(Color.Black, 2);
           b = new Bitmap(coordinate_show.Width, coordinate_show.Height);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                b = new Bitmap(coordinate_show.Width, coordinate_show.Height);
                for (int i = 0; i < Best_Visited_Trail.Length - 1; i++)  //傳值至pixel中
                {
                    
                    if (x.Length == 51)
                    {
                        pixel(Graphics.FromImage(b), aBrush, x[i] * 7 - 4, 500 - y[i] * 7 - 4, 9, 9);
                        line(Graphics.FromImage(b), aBrush, x[Best_Visited_Trail[i]] * 7, 500 - y[Best_Visited_Trail[i]] * 7, x[Best_Visited_Trail[i + 1]] * 7, 500 - y[Best_Visited_Trail[i + 1]] * 7);
                        
                    }
                    else
                    {
                        pixel(Graphics.FromImage(b), aBrush, x[i] / 8 + 50 -2 , 500 - y[i] / 8 -2, 5, 5);
                        line(Graphics.FromImage(b), aBrush, x[Best_Visited_Trail[i]] / 8 + 50, 500 - y[Best_Visited_Trail[i]] / 8, x[Best_Visited_Trail[i + 1]] / 8 + 50, 500 - y[Best_Visited_Trail[i + 1]] / 8);
                        
                    }
                }
                coordinate_show.Image = b; // b=bitmap 在剛才的迴圈中已經處理了坐標點
                button4.Enabled = true;
            }
            catch(Exception)
            {
                result_list.Items.Clear();
                result_list.Items.Add("尚未計算");
            }



        }
        private void button4_Click(object sender, EventArgs e)
        {
            coordinate_show.Image = null; // 清除畫面
        }
        
        private void calculate_Click(object sender, EventArgs e)
        {
            result_list.Items.Clear();
            result_list.Items.Add("Computing Start......");
            sw = new Stopwatch();
            sw.Reset();
            sw.Restart();
            
            int cities = x.Length;      //城市數量
            //int numAnts = x.Length / 2;     //螞蟻數量
            //int maxtime = 2000;         //搜尋次數
            double[,] Pheromones = new double[cities, cities]; //費洛蒙
            

            double[] AntLength = new double[numAnts];   //記錄每隻螞蟻所走的路徑長度
            int[,] Visited = new int[numAnts, cities + 1]; //紀錄尋訪過的城市標號，第一個城市存第一格，第二個城市存第二格
            Part_Best_Visited_Trail = new int[Visited.GetLength(1)];
            Best_Visited_Trail = new int[Visited.GetLength(1)]; //紀錄最佳探訪城市順序
            Visited = initCityVisited(Visited); //初始探訪城市陣列，全設為0
            Dist = distance(x, y);     //計算各城市間路徑
            Pheromones = initpheromones(Dist);  //初始化費洛蒙
            BestTrail = 0;

            for (var final = 0; final < maxtime; final++)
            {

                //新寫法
                int count = 1;   //
                int[] currentCity = new int[numAnts];      //第幾隻螞蟻走到哪座城市，陣列大小僅幾隻螞蟻
                bool[,] checkCity = new bool[Visited.GetLength(0), Visited.GetLength(1) - 1];                //探訪城市的迴圈
                Array.Clear(checkCity, 0, checkCity.Length);
                for (var i = 0; i < numAnts; i++)
                {
                    currentCity[i] = firstCity(cities);     //隨機決定第一座城市
                    Visited[i, 0] = currentCity[i];
                    checkCity[i, currentCity[i]] = true;
                }

                while (count != cities)  //當城市未被探訪完時，持續執行
                {

                    for (var whichant = 0; whichant < numAnts; whichant++)        //每一隻螞蟻同時探訪下一座城市
                    {
                        currentCity[whichant] = GoToNextCity(Dist, Pheromones, currentCity[whichant], checkCity, whichant, Compare, Alpha, Beta);
                        Visited[whichant, count] = currentCity[whichant];
                        checkCity[whichant, currentCity[whichant]] = true;
                        Pheromones[Visited[whichant, count - 1], Visited[whichant, count]] = partofupdatepheromones( Pheromones[Visited[whichant, count - 1], Visited[whichant, count]], rho, Dist[Visited[whichant, count - 1], Visited[whichant, count]], cities);
                        Pheromones[Visited[whichant, count], Visited[whichant, count - 1]] = Pheromones[Visited[whichant, count - 1], Visited[whichant, count]];
                    }
                    count++;
                }

                for (var i = 0; i < Visited.GetLength(0); i++)
                {
                    Visited[i, Visited.GetLength(1) - 1] = Visited[i, 0];
                }
                
                AntLength = Distsum(Visited, Dist);     //計算每隻螞蟻路徑
                BestTrail = besttrail(AntLength, BestTrail, Visited);
                Pheromones = updatepheromones(Pheromones, Visited, Dist, rho, Q);   //費洛蒙更新
            }
            sw.Stop();
            result_list.Items.Clear();
            result_list.Items.Add("花費時間：" + sw.Elapsed.ToString());
            result_list.Items.Add("最佳路徑" + BestTrail);
            result_list.Items.Add("探訪順序為：");
            for (var i = 0; i < Best_Visited_Trail.Length; i++)
            {
                result_list.Items.Add("第" + (i + 1) + "座城市：City" + (Best_Visited_Trail[i] + 1));
            }
            button3.Enabled = true;
            button7.Enabled = true;
        }

        public void loadfile()      //讀取檔案函式
        {
            

            coordinate_list.Items.Clear();
            OpenFileDialog openFileDialog1 = new OpenFileDialog();  //產生OpenFileDialog
            openFileDialog1.Filter = "txt Files|*.txt";
            openFileDialog1.Title = "Select a tsp dataset";

            // Show the Dialog.
            // If the user clicked OK in the dialog 
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Assign the cursor in the Stream to the Form's Cursor property.
                System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog1.FileName);
                // MessageBox.Show(sr.ReadToEnd());
                
                string line = null;
                char cut = ' '; //切割字串用

                //---------------------------------here----------------------
                lines = 0; // 歸0
                
                x = null;
                y = null;
                // Or can use x = new int[]; but it is warning .
                //-------------------------------------------------------------
                while ((line = sr.ReadLine()) != null)
                {
                    lines += 1;                             //判斷行數
                    Array.Resize(ref x, lines);   //每讀取一行便改變陣列大小一次
                    Array.Resize(ref y, lines);

                    string[] subline = line.Split(cut);     //讀取到空白時切割字串
                    foreach (var substring in subline) ;     //將子字串放到陣列中

                    x[lines - 1] = Convert.ToInt32(subline[0]); //string to double , insert to array.
                    y[lines - 1] = Convert.ToInt32(subline[1]);

                }
                sr.Close();     //close 否則後面不會動作
                for (int i = 0; i < lines; i++)
                {
                    coordinate_list.Items.Add("City" + (i + 1) + " 的座標: " + x[i] + ", " + y[i]);
                }
                
            } 
        }



        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                result_list.Items.Clear();
                if (textBox1.Text == "") { Compare = 0.006; }
                else { Compare = Convert.ToDouble(textBox1.Text); }
                result_list.Items.Add("Compare = " + Compare);
                if (textBox2.Text == "") { Q = 5.0; }
                else { Q = Convert.ToDouble(textBox2.Text); }
                result_list.Items.Add("Q = " + Q);
                if (textBox3.Text == "") { Alpha = 0.4; }
                else { Alpha = Convert.ToDouble(textBox3.Text); }
                result_list.Items.Add("Alpha = " + Alpha);
                if (textBox4.Text == "") { Beta = 2.0; }
                else { Beta = Convert.ToDouble(textBox4.Text); }
                result_list.Items.Add("Beta = " + Beta);
                if (textBox5.Text == "") { rho = 0.2; }
                else { rho = Convert.ToDouble(textBox5.Text); }
                result_list.Items.Add("rho = " + rho);
                if (textBox6.Text == "") { numAnts = x.Length / 2; }
                else { numAnts = Convert.ToInt32(textBox6.Text); }
                result_list.Items.Add("numAnts = " + numAnts);
                if (textBox7.Text == "") { maxtime = 2000; }
                else { maxtime = Convert.ToInt32(textBox7.Text); }
                result_list.Items.Add("maxtime = " + maxtime);
                calculate.Enabled = true;
            }
            catch (Exception)
            {
                result_list.Items.Clear();
                result_list.Items.Add("檔案尚未讀入");
            }
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                Compare = 0.006;
                Q = 5.0;
                Alpha = 0.4;
                Beta = 2.0;
                rho = 0.2;
                numAnts = x.Length / 2;
                maxtime = 2000;
                textBox1.Clear();
                textBox2.Clear();
                textBox3.Clear();
                textBox4.Clear();
                textBox5.Clear();
                textBox6.Clear();
                textBox7.Clear();
                result_list.Items.Clear();
                result_list.Items.Add("Compare = " + Compare);
                result_list.Items.Add("Q = " + Q);
                result_list.Items.Add("Alpha = " + Alpha);
                result_list.Items.Add("Beta = " + Beta);
                result_list.Items.Add("rho = " + rho);
                result_list.Items.Add("numAnts = " + numAnts);
                result_list.Items.Add("maxtime = " + maxtime);
                calculate.Enabled = true;
            }
            catch (Exception)
            {
                result_list.Items.Clear();
                result_list.Items.Add("檔案尚未讀入");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "txt Files|*.txt";
            saveFileDialog1.Title = "Save a Result File";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.ShowDialog() == DialogResult.OK && saveFileDialog1.FileName != null)
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(saveFileDialog1.OpenFile());
                for (int i = 0; i < Best_Visited_Trail.Length; i++)
                {

                    // Compose a string that consists of three lines.
                    String lines = (Best_Visited_Trail[i] + 1).ToString() + " " + x[Best_Visited_Trail[i]].ToString() + " " + y[Best_Visited_Trail[i]].ToString();
                    // Write the string to a file.

                    file.WriteLine(lines);

                }
                file.WriteLine("花費時間為" + sw.Elapsed.ToString());
                file.Write("最佳路徑長度為：" + BestTrail);
                file.Close();
                
            }
        }
    }
}
