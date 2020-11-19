﻿using GoldHunterAIGame.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GoldHunterAIGame
{
    public partial class Game : Form
    {
        public Game()
        {
            InitializeComponent();
        }

        private void Game_Load(object sender, EventArgs e)
        {
            CreateGame();
            OnLoadEvents();
            TurnTimer.Start();
        }

        #region Entities

        private readonly Random rnd = new Random(); // Random sayı üretmemiz için gerekli instance
        public static int areaXSize = 10;   // Oyunda ki bir satırda ki kare sayısı
        public static int areaYSize = 10;   // Oyunda ki bir stunda ki kare sayısı
        public static int goldRate = 20;    // Oyunda ki karelin % kaçının altın olduğunu tutan statik değişken
        public static int secretGoldRate = 10;  // Oyunda ki karelin % kaçının gizli altın olduğunu tutan statik değişken
        public static int turnMoveMAX = 4;  // Bir oyuncunun max hamle sayısını tutan statik değişken
        public static int playerTotalGold = 200; // Oyuncuların toplam altın sayısı
        public static int[] turnCost = { 5, 5, 5, 5 };  // Oyuncuların hamle maliyeti
        public static int[] findTargetCost = { 5, 10, 15, 20 };  // Oyuncuların hedef seçme maliyeti

        public static int turnMoveTEMP = 1;     // Sırası gelen oyuncunun hamle sayısını tutan temp değişken
        public static int playerTurn = 1;  // Sıranın hangi oyuncuda olduğunu tutuyor.
        public static int countLivePlayers = 4;  // Elenmemiş oyuncuların sayısı

        private readonly List<Player> playerList = new List<Player>();
        private readonly List<Gold> goldList = new List<Gold>(); // Oyunda ki altınların bilgilerinin tutulduğu liste

        #endregion Entities

        #region PlayerMechanics

        private void FindNextTarget(Player player)
        {
            switch (player.playerdID)
            {
                case 1:
                    player.target = FindTheClosestGold(player.playerLocation);
                    targetA.Text = player.target.ToString();
                    break;

                case 2:
                    player.target = FindTheMostProfitableGold(player.playerLocation);
                    targetB.Text = player.target.ToString();
                    break;

                case 3:
                    OpenTheClosestSecretGold(player.playerLocation);
                    player.target = FindTheMostProfitableGold(player.playerLocation);
                    targetC.Text = player.target.ToString();
                    break;

                case 4:
                    player.target = FindTheMostProfitableGoldForD(player.playerLocation);
                    targetD.Text = player.target.ToString();
                    break;
            }
        }

        private bool IsItClosestAccordingToOthers(int targetButton, int rangeD)
        {
            List<Player> tempTargets = playerList.Where(p => p.target == targetButton).ToList();
            Cordinant goldLocation = FindCordinant(targetButton);
            int turnCountD = Convert.ToInt32(Math.Ceiling((decimal)rangeD / (turnMoveMAX - 1)));
            foreach (var item in tempTargets)
            {
                int tempRange = Math.Abs(item.playerLocation.row - goldLocation.row) + Math.Abs(item.playerLocation.column - goldLocation.column);
                int tempTurnCount = Convert.ToInt32(Math.Ceiling((decimal)tempRange / (turnMoveMAX - 1)));

                if (turnCountD > tempTurnCount - 1)
                {
                    return false;
                }
            }

            return true;
        }

        private int FindTheMostProfitableGoldForD(Cordinant playerLocation)
        {
            int MostProfitableLocation = 0;
            int totalProfit = -9999999;

            List<Gold> tempList = goldList.Where(p => p.isSecret == false && p.isTaken == false).ToList();

            foreach (var item in tempList)
            {
                int tempRange = Math.Abs(playerLocation.row - item.goldLocation.row) + Math.Abs(playerLocation.column - item.goldLocation.column);
                int tempTotalproFit = item.value - Convert.ToInt32(Math.Ceiling((double)tempRange / (turnMoveMAX - 1)) * turnCost[playerTurn - 1]);
                List<Player> tempPlayerControl = playerList.Where(p => p.target == item.buttonNum).ToList();

                if (tempPlayerControl.Select(p => p.target).Contains(item.buttonNum))
                {
                    foreach (var player in tempPlayerControl)
                    {
                        if (IsItClosestAccordingToOthers(player.target, tempRange))
                        {
                            if (tempTotalproFit > totalProfit)
                            {
                                totalProfit = tempTotalproFit;
                                MostProfitableLocation = item.buttonNum;
                            }
                        }
                    }
                }
                else
                {
                    if (tempTotalproFit > totalProfit)
                    {
                        totalProfit = tempTotalproFit;
                        MostProfitableLocation = item.buttonNum;
                    }
                }
            }

            return MostProfitableLocation;
        }

        private int FindTheMostProfitableGold(Cordinant playerLocation)
        {
            int MostProfitableLocation = 0;
            int totalProfit = -9999999;

            List<Gold> tempList = goldList.Where(p => p.isSecret == false && p.isTaken == false).ToList();

            foreach (var item in tempList)
            {
                int tempRange = Math.Abs(playerLocation.row - item.goldLocation.row) + Math.Abs(playerLocation.column - item.goldLocation.column);
                int tempTotalproFit = item.value - Convert.ToInt32(Math.Ceiling((double)tempRange / (turnMoveMAX - 1)) * turnCost[playerTurn - 1]);
                if (tempTotalproFit > totalProfit)
                {
                    totalProfit = tempTotalproFit;
                    MostProfitableLocation = item.buttonNum;
                }
            }

            return MostProfitableLocation;
        }

        private int FindTheClosestGold(Cordinant playerLocation)
        {
            int closestLocation = 0;
            int range = 9999990;
            List<Gold> tempList = goldList.Where(p => p.isSecret == false && p.isTaken == false).ToList();

            foreach (var item in tempList)
            {
                int tempRange = Math.Abs(playerLocation.row - item.goldLocation.row) + Math.Abs(playerLocation.column - item.goldLocation.column);
                if (tempRange < range)
                {
                    range = tempRange;
                    closestLocation = item.buttonNum;
                }
            }

            return closestLocation;
        }   // Oyuncuya en yakın altını bulan fonksiyon

        private void OpenTheClosestSecretGold(Cordinant playerLocation)
        {
            int closestLocation = 0;
            int range = 9999990;

            for (int i = 0; i < 2; i++)
            {
                List<Gold> tempList = goldList.Where(p => p.isSecret == true).ToList();
                range = 9999990;
                foreach (var item in tempList)
                {
                    int tempRange = Math.Abs(playerLocation.row - item.goldLocation.row) + Math.Abs(playerLocation.column - item.goldLocation.column);
                    if (tempRange < range)
                    {
                        range = tempRange;
                        closestLocation = item.buttonNum;
                    }
                }

                OpenSecret(closestLocation);
            }
        }    // Oyuncuya en yakın gizli altınlardan 2 tanesinin gizliliğini kaldıran fonksiyon

        #endregion PlayerMechanics

        #region GlobalFunctions

        //private int FindButtonNumber(Cordinant map)
        //{
        //    int result = ((map.row - 1) * areaXSize + map.column);
        //    return result;
        //}   // satır ve stun bilgisi verilen butonun Numarasını veriyor.

        private Cordinant FindCordinant(int buttonNumber)
        {
            Cordinant map = new Cordinant
            {
                row = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(buttonNumber) / Convert.ToDouble(areaXSize)))
            };
            int rowLimit = map.row * areaXSize;
            map.column = areaXSize - (rowLimit - buttonNumber);
            return map;
        }  // Numarası verilen butonun satır ve stun bilgisini veriyor.

        public int getRandomValue()
        {
            int nextValue;
            nextValue = 5 * rnd.Next(5 / 5, 25 / 5);
            return nextValue;
        }  //Rastegele değer üreten fonksiyon (5-20) arası

        #endregion GlobalFunctions

        #region GameDynamics

        private void moveTo()
        {
            Player player = playerList.Where(p => p.playerdID == playerTurn).SingleOrDefault();
            Cordinant startSpot = player.playerLocation, endSpot = FindCordinant(player.target);
            Button playerButton = (pnlBoard.Controls["player" + player.playerName] as Button);
            if (startSpot.row < endSpot.row)
            {
                startSpot.row++;
                playerButton.Location = (pnlBoard.Controls["btn" + (player.buttonNum + areaXSize)] as Button).Location;
                player.playerLocation = startSpot;
                player.buttonNum += areaXSize;
                player.playerButtonName = "btn" + player.buttonNum.ToString();
                OpenSecret(player.buttonNum);
            }
            else if (startSpot.row > endSpot.row)
            {
                startSpot.row--;
                playerButton.Location = (pnlBoard.Controls["btn" + (player.buttonNum - areaXSize)] as Button).Location;
                player.playerLocation = startSpot;
                player.buttonNum -= areaXSize;
                player.playerButtonName = "btn" + player.buttonNum.ToString();
                OpenSecret(player.buttonNum);
            }
            else if (startSpot.column < endSpot.column)
            {
                startSpot.column++;
                playerButton.Location = (pnlBoard.Controls["btn" + (player.buttonNum + 1)] as Button).Location;
                player.playerLocation = startSpot;
                player.buttonNum += 1;
                player.playerButtonName = "btn" + player.buttonNum.ToString();
                OpenSecret(player.buttonNum);
            }
            else if (startSpot.column > endSpot.column)
            {
                startSpot.column--;
                playerButton.Location = (pnlBoard.Controls["btn" + (player.buttonNum - 1)] as Button).Location;
                player.playerLocation = startSpot;
                player.buttonNum -= 1;
                player.playerButtonName = "btn" + player.buttonNum.ToString();
                OpenSecret(player.buttonNum);
            }

            if (player.buttonNum == player.target)
            {
                playerTurn++;
                MoveTimer.Stop();

                Gold takenGold = goldList.Where(p => p.buttonNum == player.target).SingleOrDefault();
                takenGold.isTaken = true;
                (pnlBoard.Controls[takenGold.button] as Button).BackgroundImage = Properties.Resources.Dirt;
                List<Player> playerLoseTargetList = playerList.Where(p => p.target == player.target).ToList();
                foreach (var item in playerLoseTargetList)
                {
                    item.target = 0;
                }
                player.playerGold += takenGold.value;
                (pictureBox2.Controls["textPlayer" + player.playerName + "Coin"] as Label).Text = player.playerGold.ToString();
            }
        } // Sonraki hamleyi gerçekleştiren fonksiyon

        private void TurnTimer_Tick(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled == false)
            {
                if (playerTurn > 4) playerTurn = 1;
                turnMoveTEMP = 1;
                Player player = playerList.Where(p => p.playerdID == playerTurn).SingleOrDefault();

                int remainingGold = goldList.Where(p => p.isTaken == false && p.isSecret == false).Count();
                int remainingSecretGold = goldList.Where(p => p.isTaken == false).Count();
                Player playerC = playerList.Where(p => p.playerdID == 3).Single();
                int eliminatedPlayerNumber = playerList.Where(p => p.stat != 5).Count();

                if (remainingGold == 0 && playerC.playerGold < findTargetCost[2]
                 || remainingGold == 0 && remainingSecretGold == 0
                 || eliminatedPlayerNumber == 4)
                {
                    TurnTimer.Stop();
                    Player winnerPlayer = playerList.Where(t => t.playerGold == playerList.Select(p => p.playerGold).Max()).FirstOrDefault();
                    MessageBox.Show("Kazanan Player " + winnerPlayer.playerName);
                }
                else
                {
                    if (player.target != 0 && player.playerGold > turnCost[playerTurn - 1])
                    {
                        player.playerGold -= turnCost[playerTurn - 1];
                        MoveTimer.Start();
                    }
                    else if (player.target == 0 && player.playerGold >= findTargetCost[playerTurn - 1])
                    {
                        FindNextTarget(player);
                        if (player.target != 0)
                        {
                            player.playerGold -= findTargetCost[playerTurn - 1];
                            player.playerGold -= turnCost[playerTurn - 1];
                            MoveTimer.Start();
                        }
                        else
                        {
                            playerTurn++;
                        }
                    }
                    else
                    {
                        if (player.stat == 5)
                        {
                            player.stat = countLivePlayers;
                            countLivePlayers--;
                        }
                        playerTurn++;
                    }

                (pictureBox2.Controls["textPlayer" + player.playerName + "Coin"] as Label).Text = player.playerGold.ToString();
                }
            }
        }   // Oyun sırasını hareketlendiren timer

        private void MoveA_Tick(object sender, EventArgs e)
        {
            if (turnMoveTEMP == turnMoveMAX)
            {
                playerTurn++;
                MoveTimer.Stop();
            }
            else
            {
                moveTo();
                turnMoveTEMP++;
            }
        }  // Oyuncu hareketlerini sürdüren timer

        private void OpenSecret(int buttonNum)
        {
            Gold getGold = goldList.Where(p => p.buttonNum == buttonNum && p.isTaken == false).FirstOrDefault();
            if (getGold != null)
            {
                getGold.isSecret = false;
                string img = "Secret" + getGold.value.ToString();
                object obj = Properties.Resources.ResourceManager.GetObject(img);
                (pnlBoard.Controls["btn" + buttonNum.ToString()] as Button).BackgroundImage = (Image)obj;
            }
        }  // Gizli altınları açan fonksiyon

        #endregion GameDynamics

        #region InterfaceFunctions

        private void CreateGame()
        {
            pnlBoard.Controls.Clear();

            if (MoveTimer.Enabled == true) MoveTimer.Stop();
            if (TurnTimer.Enabled == true) TurnTimer.Stop();
            goldList.Clear();

            int areaTotalSize = areaXSize * areaYSize;   // Oyun alanında ki toplam kare sayısı
            int cellWidth = Convert.ToInt32(Math.Floor(Convert.ToDouble(pnlBoard.Width) / areaXSize));  // Alandaki Bir karenin genişliği
            int cellHeight = Convert.ToInt32(Math.Floor(Convert.ToDouble(pnlBoard.Height) / areaYSize)); // Alandaki Bir karenin uzunluğu

            int playerAFirstSpawn = 1;
            int playerBFirstSpawn = areaXSize;
            int playerCFirstSpawn = areaTotalSize;
            int playerDFirstSpawn = areaTotalSize - (areaXSize - 1);

            //Altınlar için rasgele yer türetiyoruz.
            GenerateGolds(areaTotalSize, playerAFirstSpawn, playerBFirstSpawn, playerCFirstSpawn, playerDFirstSpawn);

            GenerateMap(areaTotalSize, cellWidth, cellHeight);

            GeneratePlayers(cellWidth, cellHeight, playerAFirstSpawn, playerBFirstSpawn, playerCFirstSpawn, playerDFirstSpawn);
        }    // Oyunu oluşturan fonksiyon

        private void GenerateMap(int areaTotalSize, int cellWidth, int cellHeight)
        {
            int ButtonCount = 0;
            int x = 0, y = 0;
            for (int i = 0; i < areaTotalSize; i++)
            {
                Bitmap tempImage = new Bitmap(Properties.Resources.Dirt);
                Gold tempGold = goldList.Where(p => (p.buttonNum - 1) == i).SingleOrDefault();

                if (tempGold != null && tempGold.isSecret == false)
                {
                    string img = "Gold" + tempGold.value.ToString();
                    object obj = Properties.Resources.ResourceManager.GetObject(img);
                    tempImage = (Bitmap)obj;
                }

                Button btn = new Button
                {
                    Font = new System.Drawing.Font("Microsoft Sans Serif", (float)cellHeight / 11f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162))),
                    Size = new System.Drawing.Size(cellWidth, cellHeight),
                    TextAlign = System.Drawing.ContentAlignment.BottomLeft,
                    BackColor = Color.White,
                    Text = (i + 1).ToString(),
                    Name = "btn" + (i + 1),
                    BackgroundImageLayout = ImageLayout.Stretch,
                    BackgroundImage = tempImage,
                    Enabled = false,
                    FlatStyle = System.Windows.Forms.FlatStyle.Popup
                };

                if (ButtonCount < areaXSize)
                {
                    btn.Location = new Point(x, y);
                    pnlBoard.Controls.Add(btn);
                    x += cellWidth;
                    ButtonCount++;
                }
                else
                {
                    ButtonCount = 1;
                    x = 0;
                    y += cellHeight;
                    btn.Location = new Point(x, y);
                    pnlBoard.Controls.Add(btn);
                    x += cellWidth;
                }
            }
        }  // Oyun arenasını oluşturan fonksiyon

        private void GeneratePlayers(int cellWidth, int cellHeight, int playerAFirstSpawn, int playerBFirstSpawn, int playerCFirstSpawn, int playerDFirstSpawn)
        {
            pnlBoard.Controls.Add(playerA);
            playerA.BringToFront();
            playerA.Size = new System.Drawing.Size(cellWidth, cellHeight);
            playerA.Location = (pnlBoard.Controls["btn" + playerAFirstSpawn.ToString()] as Button).Location;
            playerA.AccessibleName = playerAFirstSpawn.ToString();
            playerList.Add(new Player { playerdID = 1, playerName = "A", playerLocation = FindCordinant(playerAFirstSpawn), playerGold = playerTotalGold, buttonNum = playerAFirstSpawn, playerButtonName = "btn" + playerAFirstSpawn.ToString(), target = 0, stat = 5 });

            pnlBoard.Controls.Add(playerB);
            playerB.BringToFront();
            playerB.Size = new System.Drawing.Size(cellWidth, cellHeight);
            playerB.Location = (pnlBoard.Controls["btn" + playerBFirstSpawn.ToString()] as Button).Location;
            playerB.AccessibleName = playerBFirstSpawn.ToString();
            playerList.Add(new Player { playerdID = 2, playerName = "B", playerLocation = FindCordinant(playerBFirstSpawn), playerGold = playerTotalGold, buttonNum = playerBFirstSpawn, playerButtonName = "btn" + playerBFirstSpawn.ToString(), target = 0, stat = 5 });

            pnlBoard.Controls.Add(playerC);
            playerC.BringToFront();
            playerC.Size = new System.Drawing.Size(cellWidth, cellHeight);
            playerC.Location = (pnlBoard.Controls["btn" + playerCFirstSpawn.ToString()] as Button).Location;
            playerC.AccessibleName = playerCFirstSpawn.ToString();
            playerList.Add(new Player { playerdID = 3, playerName = "C", playerLocation = FindCordinant(playerCFirstSpawn), playerGold = playerTotalGold, buttonNum = playerCFirstSpawn, playerButtonName = "btn" + playerCFirstSpawn.ToString(), target = 0, stat = 5 });

            pnlBoard.Controls.Add(playerD);
            playerD.BringToFront();
            playerD.Size = new System.Drawing.Size(cellWidth, cellHeight);
            playerD.Location = (pnlBoard.Controls["btn" + playerDFirstSpawn.ToString()] as Button).Location;
            playerD.AccessibleName = playerDFirstSpawn.ToString();
            playerList.Add(new Player { playerdID = 4, playerName = "D", playerLocation = FindCordinant(playerDFirstSpawn), playerGold = playerTotalGold, buttonNum = playerDFirstSpawn, playerButtonName = "btn" + playerDFirstSpawn.ToString(), target = 0, stat = 5 });
        }  // Oyuncuları üreten fonksiyon

        private void GenerateGolds(int areaTotalSize, int playerAFirstSpawn, int playerBFirstSpawn, int playerCFirstSpawn, int playerDFirstSpawn)
        {
            int spawnedTotalGold = 1;
            int goldIterator = 0;
            int totalGoldCount = (areaTotalSize * goldRate) / 100; // Alandaki Toplam Altın Sayısı
            int totalSecretGoldCount = (areaTotalSize * secretGoldRate) / 100; // Alandaki Toplam Gizli Altın Sayısı
            int[] goldSpawns = new int[totalGoldCount]; // Alandaki altınların yeri (Buton numarası olarak)
            int[] secretGoldSpawns = new int[totalSecretGoldCount]; // Alandaki altınların yeri (Buton numarası olarak)

            while (goldIterator < totalGoldCount)
            {
                int nextNumber = rnd.Next(areaTotalSize);
                if (nextNumber != playerAFirstSpawn && nextNumber != playerBFirstSpawn && nextNumber != playerCFirstSpawn && nextNumber != playerDFirstSpawn)
                {
                    if (!(goldSpawns.Contains(nextNumber)))
                    {
                        goldSpawns[goldIterator] = nextNumber;
                        goldList.Add(new Gold { goldID = spawnedTotalGold, isSecret = false, goldLocation = FindCordinant(nextNumber), button = "btn" + (nextNumber).ToString(), value = getRandomValue(), buttonNum = nextNumber, isTaken = false });
                        spawnedTotalGold++;
                        goldIterator++;
                    }
                }
            }

            //Gizli Altınlar için rasgele yer türetiyoruz.
            goldIterator = 0;
            while (goldIterator < totalSecretGoldCount)
            {
                int nextNumber = rnd.Next(areaTotalSize);
                if (nextNumber != playerAFirstSpawn && nextNumber != playerBFirstSpawn && nextNumber != playerCFirstSpawn && nextNumber != playerDFirstSpawn)
                {
                    if (!(goldSpawns.Contains(nextNumber)) && !(secretGoldSpawns.Contains(nextNumber)))
                    {
                        secretGoldSpawns[goldIterator] = nextNumber;
                        goldList.Add(new Gold { goldID = spawnedTotalGold, isSecret = true, goldLocation = FindCordinant(nextNumber), button = "btn" + (nextNumber).ToString(), value = getRandomValue(), buttonNum = nextNumber, isTaken = false });
                        spawnedTotalGold++;
                        goldIterator++;
                    }
                }
            }
        }  // Altınları oluşturan fonksiyon

        private void pnlBoard_Resize(object sender, EventArgs e)
        {
            CreateGame();
        }   // Oyun ekranının değişmesi durumunda oyunun tekrardan oluşması için gerekli trigger

        private void OnLoadEvents()
        {
            pictureBox2.Parent = pictureBox1;
            textPlayerACoin.Parent = pictureBox2;
            textPlayerBCoin.Parent = pictureBox2;
            textPlayerCCoin.Parent = pictureBox2;
            textPlayerDCoin.Parent = pictureBox2;
            textPlayerACoin.Text = playerTotalGold.ToString();
            textPlayerBCoin.Text = playerTotalGold.ToString();
            textPlayerCCoin.Text = playerTotalGold.ToString();
            textPlayerDCoin.Text = playerTotalGold.ToString();
            label5.Parent = pictureBox2;
            label6.Parent = pictureBox2;
            label7.Parent = pictureBox2;
            label8.Parent = pictureBox2;
            goldCoin1.Parent = pictureBox2;
            goldCoin2.Parent = pictureBox2;
            goldCoin3.Parent = pictureBox2;
            goldCoin4.Parent = pictureBox2;
        } // Controllerin düzenlemeleri yapılıyor

        #endregion InterfaceFunctions
    }
}