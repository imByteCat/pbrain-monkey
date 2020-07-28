using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

partial class GomocupEngine : GomocupInterface
{
    Hashtable[] VCFTable = { new Hashtable(), new Hashtable(), new Hashtable() };//VCF置换表    
    long PresentVCFZobristHashCode = 0;
    bool VCF_IfRegenerationIsNeeded = false;
    //bool VCF_IfNotRenewTheValidPointsIsNeeded = false;
    List<Point> VCF_Order = new List<Point>();
    List<AttackAndDefend> VCF_DynamicValidPoints = new List<AttackAndDefend>();
    private bool VCF(int Color, int Depth)//使用ABBoard//true表示有VCF，false表示无VCF
    {
        if (Depth < 0) { VCF_AddChessboardToVCFHashTable(Color); return false; }
        if (VCF_VCFHashTableContainsThisSituation(Color)) return false;
        int OpponentColor = 3 - Color;
        List<AttackAndDefend> m = new List<AttackAndDefend>();
        //m = VCF_GenerateVCFLocation(Color);        
        if (VCF_IfRegenerationIsNeeded)
        {
            m = VCF_GenerateVCFLocation(Color);
            VCF_DynamicValidPoints = new List<AttackAndDefend>(m);
            VCF_IfRegenerationIsNeeded = false;
        }
        else
            m = VCF_RenewVCFLocation(Color);
        if (m.Count == 0) { VCF_AddChessboardToVCFHashTable(Color); return false; }
        else if (m.Count == 1)//如果对方有冲四
        {
            Point p = m[0].Attack;
            VCF_MakeMove(Color, p);
            if (IfLiveFour(p, Color) || IfFive(p, Color)) { VCF_UnMakeMove(Color, p); return true; }//如果活四，直接判断胜利
            Point dp = VCF_IfLevelAboveFour(p, Color);
            bool HasGotVCF = false;
            if (dp.X < width)
            {
                VCF_MakeMove(OpponentColor, dp);
                VCF_IfRegenerationIsNeeded = true;
                HasGotVCF = VCF(Color, Depth - 1);
                VCF_DynamicValidPoints = new List<AttackAndDefend>(m);
                VCF_UnMakeMove(OpponentColor, dp);
            }
            VCF_UnMakeMove(Color, p);
            if (HasGotVCF == true) return true;
        }
        else
        {
            for (int i = 0; i < m.Count; i++)
            {
                Point p = m[i].Attack, op = m[i].Defend;
                VCF_MakeMove(Color, p);
                if (IfLiveFour(p, Color) || IfFive(p, Color)) { VCF_UnMakeMove(Color, p); return true; }//如果活四，直接判断胜利
                VCF_MakeMove(OpponentColor, op);
                bool HasGotVCF = VCF(Color, Depth - 1);
                VCF_DynamicValidPoints = new List<AttackAndDefend>(m);
                VCF_UnMakeMove(OpponentColor, op);
                VCF_UnMakeMove(Color, p);
                if (HasGotVCF == true) return true;
            }
        }
        //VCF_PresentValidPoints = new List<AttackAndDefend>(m);
        VCF_AddChessboardToVCFHashTable(Color); return false;
    }
    private void VCF_AddChessboardToVCFHashTable(int Color)
    {
        if (!VCFTable[Color].ContainsKey(PresentVCFZobristHashCode))
            VCFTable[Color].Add(PresentVCFZobristHashCode, ChessmanNumber);
    }
    private bool VCF_VCFHashTableContainsThisSituation(int Color)//如果置换表中存在该情况，返回true
    {
        return VCFTable[Color].ContainsKey(PresentVCFZobristHashCode);
    }
    private void VCF_MakeMove(int Color, Point p)
    {
        Chessboard[p.X, p.Y] = Color;
        RenewPresentZobristHashCode(Color, p.X, p.Y);
        VCF_Order.Add(p);
        //Console.WriteLine("DEBUG Makemove" + p.X.ToString() + "," + p.Y.ToString());
    }
    private void VCF_UnMakeMove(int Color, Point p)
    {
        Chessboard[p.X, p.Y] = 0;
        RenewPresentZobristHashCode(Color, p.X, p.Y);
        VCF_Order.RemoveAt(VCF_Order.Count - 1);
        //Console.WriteLine("DEBUG Unmakemove" + p.X.ToString() + "," + p.Y.ToString());
    }
    private Point VCF_IfFour(Point p, int Color)//如果只形成一个冲四，则返回防守坐标，若形成活四、连五、多个冲四，返回(width+2,height+2)，若否，则返回(width+1,height+1)
    {
        if (Chessboard[p.X, p.Y] != Color) return new Point(width + 1, height + 1);
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;
        int LiveFours = 0;
        Point KeyPoint = new Point();
        for (int d = 0; d <= 3; d++)//d表示方向
        {
            int PositiveChessmanCount = 0;//正方向上越过Vacancy后的棋子数量
            bool PositiveVacancy = false;
            int NegativeChessmanCount = 0;
            bool NegativeVacancy = false;
            int MiddleChessmanCount = 1;
            Point PositiveKeyPoint = new Point(-1, -1);
            Point NegativeKeyPoint = new Point(-1, -1);
            //正方向
            for (int i = 1; i <= 4; i++)
            {
                int x = p.X + DeltaX[d] * i, y = p.Y + DeltaY[d] * i;
                if (x >= width || y >= height || y < 0) break;
                if (PositiveVacancy)
                {
                    if (Chessboard[x, y] != Color) break;
                    PositiveChessmanCount += 1;
                }
                else
                {
                    if (Chessboard[x, y] == OpponentColor) break;
                    else if (Chessboard[x, y] == 0)
                    {
                        PositiveKeyPoint = new Point(x, y);
                        PositiveVacancy = true;
                    }
                    else
                        MiddleChessmanCount += 1;
                }
            }
            //负方向
            for (int i = -1; i >= -4; i--)
            {
                int x = p.X + DeltaX[d] * i, y = p.Y + DeltaY[d] * i;
                if (x < 0 || y >= height || y < 0) break;
                if (NegativeVacancy)
                {
                    if (Chessboard[x, y] != Color) break;
                    NegativeChessmanCount += 1;
                }
                else
                {
                    if (Chessboard[x, y] == OpponentColor) break;
                    else if (Chessboard[x, y] == 0)
                    {
                        NegativeKeyPoint = new Point(x, y);
                        NegativeVacancy = true;
                    }
                    else
                        MiddleChessmanCount += 1;
                }
            }
            if (Math.Max(PositiveChessmanCount, NegativeChessmanCount) + MiddleChessmanCount >= 4)
            {
                if (MiddleChessmanCount == 4 && PositiveVacancy && NegativeVacancy)//如果是活四
                    return new Point(width + 2, height + 2);
                else if (MiddleChessmanCount >= 5)//如果连五
                    return new Point(width + 2, height + 2);
                else if (PositiveKeyPoint.X == -1 && NegativeKeyPoint.X == -1)
                    continue;
                else if (PositiveKeyPoint.X == -1)
                {
                    LiveFours += 1;
                    KeyPoint = NegativeKeyPoint;
                }
                else if (NegativeKeyPoint.X == -1)
                {
                    LiveFours += 1;
                    KeyPoint = PositiveKeyPoint;
                }
                else
                {
                    LiveFours += 1;
                    KeyPoint = PositiveChessmanCount >= NegativeChessmanCount ? PositiveKeyPoint : NegativeKeyPoint;
                }
            }
        }
        if (LiveFours == 0)
            return new Point(width + 1, height + 1);
        else if (LiveFours == 1)
            return KeyPoint;
        else
            return new Point(width + 2, height + 2);
        //以下算法效率没有上面的高，但绝对正确
        #region 
        /*
        //成五不包括在冲四中
        if (Chessboard[p.X, p.Y] != Color) return new Point(width + 1, height + 1);
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;
        for (int d = 0; d <= 3; d++)//d表示方向
            for (int i = 0; i <= 4; i++)//i表示偏移量
            {
                int ChessmanCount = 5;
                Point KeyPoint = new Point(0, 0);
                if (p.X + (-i) * DeltaX[d] >= width || p.X + (-i) * DeltaX[d] < 0 || p.Y + (-i) * DeltaY[d] >= height || p.Y + (-i) * DeltaY[d] < 0 ||
                    p.X + (4 - i) * DeltaX[d] >= width || p.X + (4 - i) * DeltaX[d] < 0 || p.Y + (4 - i) * DeltaY[d] >= height || p.Y + (4 - i) * DeltaY[d] < 0)
                    continue;
                for (int j = -i; j <= 4 - i; j++)//j表示离开point的距离
                {
                    int x = p.X + j * DeltaX[d], y = p.Y + j * DeltaY[d];
                    if (Chessboard[x, y] == OpponentColor) { ChessmanCount = 0; break; }
                    if (Chessboard[x, y] == 0)
                    {
                        ChessmanCount -= 1;
                        if (ChessmanCount < 4)
                            break;
                        KeyPoint = new Point(x, y);
                    }
                }
                if (ChessmanCount == 4)
                    return KeyPoint;
            }
        return new Point(width + 1, height + 1);
        */
        #endregion
    }
    private Point VCF_IfLevelAboveFour(Point p, int Color)//如果形成冲四、活四，则返回防守坐标，若未形成，则返回(width+1,height+1)，若形成连五，返回(width+2,height+2)
    {
        if (Chessboard[p.X, p.Y] != Color) return new Point(width + 1, height + 1);
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;
        for (int d = 0; d <= 3; d++)//d表示方向
        {
            int PositiveChessmanCount = 0;//正方向上越过Vacancy后的棋子数量
            bool PositiveVacancy = false;
            int NegativeChessmanCount = 0;
            bool NegativeVacancy = false;
            int MiddleChessmanCount = 1;
            Point PositiveKeyPoint = new Point(-1, -1);
            Point NegativeKeyPoint = new Point(-1, -1);
            //正方向
            for (int i = 1; i <= 4; i++)
            {
                int x = p.X + DeltaX[d] * i, y = p.Y + DeltaY[d] * i;
                if (x >= width || y >= height || y < 0) break;
                if (PositiveVacancy)
                {
                    if (Chessboard[x, y] != Color) break;
                    PositiveChessmanCount += 1;
                }
                else
                {
                    if (Chessboard[x, y] == OpponentColor) break;
                    else if (Chessboard[x, y] == 0)
                    {
                        PositiveKeyPoint = new Point(x, y);
                        PositiveVacancy = true;
                    }
                    else
                        MiddleChessmanCount += 1;
                }
            }
            //负方向
            for (int i = -1; i >= -4; i--)
            {
                int x = p.X + DeltaX[d] * i, y = p.Y + DeltaY[d] * i;
                if (x < 0 || y >= height || y < 0) break;
                if (NegativeVacancy)
                {
                    if (Chessboard[x, y] != Color) break;
                    NegativeChessmanCount += 1;
                }
                else
                {
                    if (Chessboard[x, y] == OpponentColor) break;
                    else if (Chessboard[x, y] == 0)
                    {
                        NegativeKeyPoint = new Point(x, y);
                        NegativeVacancy = true;
                    }
                    else
                        MiddleChessmanCount += 1;
                }
            }
            if (Math.Max(PositiveChessmanCount, NegativeChessmanCount) + MiddleChessmanCount >= 4)
            {
                //if(MiddleChessmanCount == 4 && PositiveVacancy && NegativeVacancy)//如果是活四
                //    return new Point(width + 2, height + 2);
                if (MiddleChessmanCount >= 5)//如果连五
                    return new Point(width + 2, height + 2);
                else if (PositiveKeyPoint.X == -1 && NegativeKeyPoint.X == -1)
                    continue;
                else if (PositiveKeyPoint.X == -1)
                    return NegativeKeyPoint;
                else if (NegativeKeyPoint.X == -1)
                    return PositiveKeyPoint;
                else
                    return PositiveChessmanCount >= NegativeChessmanCount ? PositiveKeyPoint : NegativeKeyPoint;
            }
        }
        return new Point(width + 1, height + 1);
    }
    private int VCF_EvaluateSingleChessman(Point p, int Color)//计算“将要”落下的棋子的价值
    {
        if (Chessboard[p.X, p.Y] != 0) return 0;
        Chessboard[p.X, p.Y] = Color;
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;//对手棋子
        int Evaluation = 0;
        int x = 0, y = 0;
        //先计算己方形成连接的价值
        for (int d = 0; d <= 3; d++)//d表示方向
        {
            int dx = DeltaX[d], dy = DeltaY[d];
            int NegativeMaxk = 0;//负方向
            for (int k = -1; k >= -4; k--)
            {
                x = p.X + dx * k;
                y = p.Y + dy * k;
                if (x < 0 || y < 0 || x >= width || y >= height) break;
                if (Chessboard[x, y] == OpponentColor) break;
                NegativeMaxk += 1;
            }
            int PositiveMaxk = 0;//正方向
            for (int k = 1; k <= 4; k++)
            {
                x = p.X + dx * k;
                y = p.Y + dy * k;
                if (x < 0 || y < 0 || x >= width || y >= height) break;
                if (Chessboard[x, y] == OpponentColor) break;
                PositiveMaxk += 1;
            }
            List<int> Line = new List<int>();
            for (int i = -NegativeMaxk; i <= PositiveMaxk; i++)
            {
                Line.Add(Chessboard[p.X + dx * i, p.Y + dy * i]);
            }
            Evaluation += EvaluateSingleLine(Line, Color);
        }
        Chessboard[p.X, p.Y] = 0;
        return Evaluation;
    }
    private List<AttackAndDefend> VCF_GenerateVCFLocation(int Color)//使用Chessboard
    {
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;
        List<AttackAndDefend> VCFValidPoints = new List<AttackAndDefend>();
        //先判断对手是否有冲四
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                if (Chessboard[i, j] == Color) continue;
                for (int k = 0; k <= 3; k++)//k表示方向
                {
                    int ChessmanCount = 5;
                    Point KeyPoint = new Point(-1, -1);
                    Point BugPoint = new Point(-1, -1);//如果对方活四，直接return，不返回任何坐标
                    if (i + 2 * DeltaX[k] < 0 || i + 2 * DeltaX[k] >= width || j + 2 * DeltaY[k] >= height || j + 2 * DeltaY[k] < 0
                        || i - 2 * DeltaX[k] < 0 || i - 2 * DeltaX[k] >= width || j - 2 * DeltaY[k] >= height || j - 2 * DeltaY[k] < 0) continue;
                    for (int m = -2; m <= 2; m++)
                    {
                        int x = i + m * DeltaX[k], y = j + m * DeltaY[k];
                        if (Chessboard[x, y] == Color) { ChessmanCount = 0; break; };
                        if (Chessboard[x, y] == 0)
                        {
                            ChessmanCount -= 1;
                            if (ChessmanCount < 4) break;
                            KeyPoint = new Point(x, y);
                            if (m == -2)
                            {
                                int bx = i + 3 * DeltaX[k], by = j + 3 * DeltaY[k];
                                if (bx < width && bx >= 0 && by < height && by >= 0)
                                    BugPoint = new Point(bx, by);
                            }
                            else if (m == 2)
                            {
                                int bx = i - 3 * DeltaX[k], by = j - 3 * DeltaY[k];
                                if (bx < width && bx >= 0 && by < height && by >= 0)
                                    BugPoint = new Point(bx, by);
                            }
                        }
                    }
                    if (ChessmanCount == 4)
                    {
                        if (BugPoint.X != -1)
                            if (Chessboard[BugPoint.X, BugPoint.Y] == 0)
                                return new List<AttackAndDefend>();
                        VCFValidPoints.Add(new AttackAndDefend(KeyPoint));
                        return VCFValidPoints;
                    }
                }
            }

        //若没有对手的，则生成自己的
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                if (Chessboard[i, j] == OpponentColor) continue;
                for (int k = 0; k <= 3; k++)//k表示方向
                {
                    int ChessmanCount = 5;
                    Point KeyPoint1 = new Point(-1, -1);
                    Point KeyPoint2 = new Point(-1, -1);
                    if (i + 2 * DeltaX[k] < 0 || i + 2 * DeltaX[k] >= width || j + 2 * DeltaY[k] >= height || j + 2 * DeltaY[k] < 0
                        || i - 2 * DeltaX[k] < 0 || i - 2 * DeltaX[k] >= width || j - 2 * DeltaY[k] >= height || j - 2 * DeltaY[k] < 0) continue;
                    //正负方向+2-2
                    for (int m = -2; m <= 2; m++)
                    {
                        int x = i + m * DeltaX[k], y = j + m * DeltaY[k];
                        if (Chessboard[x, y] == OpponentColor) { ChessmanCount = 0; break; };
                        if (Chessboard[x, y] == 0)
                        {
                            ChessmanCount -= 1;
                            if (ChessmanCount < 3) break;
                            if (KeyPoint1.X == -1)
                                KeyPoint1 = new Point(x, y);
                            else
                                KeyPoint2 = new Point(x, y);
                        }
                    }
                    if (ChessmanCount >= 3)//如果为4，则表示成五已胜
                    {
                        AttackAndDefend aad1 = new AttackAndDefend(KeyPoint1, KeyPoint2);
                        AttackAndDefend aad2 = new AttackAndDefend(KeyPoint2, KeyPoint1);
                        if (!VCFValidPoints.Contains(aad1))
                            VCFValidPoints.Add(aad1);
                        if (!VCFValidPoints.Contains(aad2))
                            VCFValidPoints.Add(aad2);
                    }
                }
            }
        //VCFValidPoints.Sort((a, b) => -VCF_EvaluateSingleChessman(a.Attack, Color).CompareTo(VCF_EvaluateSingleChessman(b.Attack, Color)));
        return VCFValidPoints;
    }
    private List<AttackAndDefend> VCF_RenewVCFLocation(int Color)
    {
        int[] DeltaX = { 0, 1, 1, 1 };
        int[] DeltaY = { 1, -1, 0, 1 };
        int OpponentColor = 3 - Color;
        List<AttackAndDefend> ValidPoints = new List<AttackAndDefend>();
        Point mc = VCF_Order[VCF_Order.Count - 2];//我的最后一个棋子  mc=my chessman
        Point oc = VCF_Order[VCF_Order.Count - 1];//对手的最后一个棋子  oc=opponent chessman
        //先判断对方是否有冲四
        Point dp = VCF_IfFour(oc, OpponentColor);
        if (dp.X < width)//如果对方有冲四
        {
            ValidPoints.Add(new AttackAndDefend(dp));
            VCF_DynamicValidPoints = new List<AttackAndDefend>(ValidPoints);
            return ValidPoints;
        }
        else if (dp.X == width + 2)//如果对方形成活四
            return ValidPoints;
        //把aad中所有含有mc与oc的项删掉
        for (int i = 0; i < VCF_DynamicValidPoints.Count; i++)
        {
            AttackAndDefend aad = VCF_DynamicValidPoints[i];
            if (aad.Attack.Equals(mc) || aad.Defend.Equals(mc) || aad.Attack.Equals(oc) || aad.Defend.Equals(oc))
            {
                VCF_DynamicValidPoints.RemoveAt(i);
                i--;
            }
        }
        //将四个方向4格内的 空格 存储到ValidRange内
        List<Point> ValidRange = new List<Point>();
        for (int d = 0; d <= 3; d++)
        {
            //正方向
            for (int i = 1; i <= 4; i++)
            {
                int mx = DeltaX[d] * i + mc.X, my = DeltaY[d] * i + mc.Y;
                if (mx >= width || my < 0 || my >= height) break;
                if (Chessboard[mx, my] != 0) continue;
                Point p = new Point(mx, my);
                if (!ValidRange.Contains(p))
                    ValidRange.Add(new Point(mx, my));
            }
            for (int i = 1; i <= 4; i++)
            {
                int ox = DeltaX[d] * i + oc.X, oy = DeltaY[d] * i + oc.Y;
                if (ox >= width || oy < 0 || oy >= height) break;
                if (Chessboard[ox, oy] != 0) continue;
                Point p = new Point(ox, oy);
                if (!ValidRange.Contains(p))
                    ValidRange.Add(new Point(ox, oy));
            }
            //负方向
            for (int i = -1; i >= -4; i--)
            {
                int mx = DeltaX[d] * i + mc.X, my = DeltaY[d] * i + mc.Y;
                if (mx < 0 || my < 0 || my >= height) break;
                if (Chessboard[mx, my] != 0) continue;
                Point p = new Point(mx, my);
                if (!ValidRange.Contains(p))
                    ValidRange.Add(new Point(mx, my));
            }
            for (int i = -1; i >= -4; i--)
            {
                int ox = DeltaX[d] * i + oc.X, oy = DeltaY[d] * i + oc.Y;
                if (ox < 0 || oy < 0 || oy >= height) break;
                if (Chessboard[ox, oy] != 0) continue;
                Point p = new Point(ox, oy);
                if (!ValidRange.Contains(p))
                    ValidRange.Add(new Point(ox, oy));
            }
        }
        //将ValidRange内的ValidPoint删掉               
        foreach (Point p in ValidRange)
            for (int i = 0; i < VCF_DynamicValidPoints.Count; i++)
            {
                AttackAndDefend aad = VCF_DynamicValidPoints[i];
                if (aad.Attack.Equals(p))
                {
                    VCF_DynamicValidPoints.RemoveAt(i);
                    break;
                }
            }
        //在ValidRange内判断是否是冲四点
        foreach (Point p in ValidRange)
        {
            Chessboard[p.X, p.Y] = Color;
            Point p2 = VCF_IfLevelAboveFour(p, Color);
            if (p2.X < width)
                //insert的效果会比add好，add会导致频繁转移战场
                VCF_DynamicValidPoints.Insert(0, new AttackAndDefend(p, p2));
            //p2.X == width + 2这句语句理论上在VCF迭代中永远不会被执行到，这里以防万一
            else if (p2.X == width + 2)
                return new List<AttackAndDefend> { new AttackAndDefend(p) };
            Chessboard[p.X, p.Y] = 0;
        }
        //按照棋子价值降序排列
        //VCF_DynamicValidPoints.Sort((a, b) => -VCF_EvaluateSingleChessman(a.Attack, Color).CompareTo(VCF_EvaluateSingleChessman(b.Attack, Color)));
        return new List<AttackAndDefend>(VCF_DynamicValidPoints);
    }
    private void MakeVCF()
    {
        //计算VCF
        List<AttackAndDefend> m = VCF_GenerateVCFLocation(1);
        VCF_DynamicValidPoints = new List<AttackAndDefend>(m);
        for (int i = 0; i < m.Count; i++)
        {
            Point p = m[i].Attack, op = m[i].Defend;
            VCF_MakeMove(1, p);
            VCF_MakeMove(2, op);
            bool HasGotVCF = VCF(1, 100);
            VCF_DynamicValidPoints = new List<AttackAndDefend>(m);
            VCF_UnMakeMove(2, op);
            VCF_UnMakeMove(1, p);
            if (HasGotVCF == true) { BestValue = 10000; ReturnAnswer(p); return; }
        }
        Console.WriteLine("DEBUG " + "No VCF!" + " " + watch.ElapsedMilliseconds.ToString() + "ms");
    }
}