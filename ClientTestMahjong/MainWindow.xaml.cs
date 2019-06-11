using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LoveLive_Mahjong_Library;
using System.Linq;
using System.Timers;
using System.Windows.Markup;

namespace ClientTestMahjong
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MahjongLogic mahjongLogic = new MahjongLogic();
        Grid[] grdsHandcard;
        Grid[] grdsRivercard;
        Label[] lblsPlayerPoint;
        Label[] lblsPlayerWind;
        Grid[] grdsPlayerAction;
        Grid[] grdsFuru;
        ResourceManager mahjongResource = new ResourceManager(typeof(mahjong));
        Dictionary<MahjongCardName, ImageSource> cardImage;
        IEnumerable<IGrouping<int, PlayerAction>> playerActionGroup;

        public MainWindow()
        {
            InitializeComponent();
            LoveLive_MahjongClass.InitializeMahjongClass();
            LoadCardImages();

            StartNewGame();

            Timer timer = new Timer(250);

            timer.Elapsed += delegate
            {
                Dispatcher.Invoke(RefreshTable);
            };

            timer.Start();
        }

        public void StartNewGame()
        {
            mahjongLogic = new MahjongLogic();
            mahjongLogic.StartGamingThread();
            mahjongLogic.NewGame_Handle(25000, 4);
            mahjongLogic.NextScene();

            // Set callback
            mahjongLogic.PlayerActionResponseCallback += PlayerActionResponseCallback;
            mahjongLogic.PlayerActionAcceptedCallback += PlayerActionAcceptedCallback;

            // Initialize the players
            grdsHandcard = new Grid[] { grdHandcardSelf, grdHandcardDownwind, grdHandcardOppositewind, grdHandcardUpwind };
            grdsRivercard = new Grid[] { grdRiverSelf, grdRiverDownwind, grdRiverOppositewind, grdRiverUpwind };
            lblsPlayerPoint = new Label[] { lblPointSelf, lblPointDownwind, lblPointOppsitewind, lblPointUpwind };
            lblsPlayerWind = new Label[] { lblWindSelf, lblWindDownwind, lblWindOppositewind, lblWindUpwind };
            grdsPlayerAction = new Grid[] { grdActionSelf, grdActionDownwind, grdActionOppositewind, grdActionUpwind };
            grdsFuru = new Grid[] { grdFuruEast, grdFuruSouth, grdFuruWest, grdFuruNorth };

            // Clear
            foreach(Grid panel in grdsPlayerAction)
            {
                panel.Children.Clear();
            }
        }

        public int GetPlayerOrder(int PlayerId) => mahjongLogic.PlayerOrder.ToList().IndexOf(PlayerId);

        public int GetPlayerLocation(int PlayerId)
        {
            // From playing
            int playing = mahjongLogic.Playing;

            // Current location
            int playing_order = GetPlayerOrder(playing);
            int current_order = GetPlayerOrder(PlayerId);

            int location = current_order - playing_order;

            if (location < 0) location = 4 + location;
            if (location > 4) location = location - 4;

            return location;
        }

        public void LoadCardImages()
        {
            cardImage = new Dictionary<MahjongCardName, ImageSource>();

            string[] cardlist = Enum.GetNames(typeof(MahjongCardName));
            foreach(string card in cardlist)
            {
                Bitmap bitmap = mahjongResource.GetObject(card) as Bitmap;
                ImageSource source = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                cardImage.Add((MahjongCardName)Enum.Parse(typeof(MahjongCardName), card), source);
            }
        }

        public void RefreshTable()
        {
            // Show hand card
            ShowHandCard();

            // Show river card
            ShowRiverCard();

            // Show furu card
            ShowFuru();

            // Show game status
            ShowGameStatus();

            // Show player status
            ShowPlayerStatus();
        }

        public void ShowPlayerStatus()
        {
            for (int player = 0; player < 4; player++)
            { 
                int location = GetPlayerLocation(player);

                PlayerInfo playerInfo = mahjongLogic.GetPlayerInfo(player);

                lblsPlayerPoint[location].Content = $"{playerInfo.points}";

                string[] winds = { "东", "南", "西", "北" };
                string wind = $"{ winds[GetPlayerOrder(player)]}, {player}";

                lblsPlayerWind[location].Content = wind;

                if (player == mahjongLogic.Playing)
                    lblsPlayerWind[location].Foreground = System.Windows.Media.Brushes.Red;
                else
                    lblsPlayerWind[location].Foreground = System.Windows.Media.Brushes.White;
            }
        }

        public void ShowGameStatus()
        {
            // Scene and subscene
            MahjongGame game = mahjongLogic.game;
            int scene = mahjongLogic.scene;
            int subscene = mahjongLogic.subscene;

            string GameScene = "";
            switch (game)
            {
                case MahjongGame.East: GameScene = "东"; break;
                case MahjongGame.South: GameScene = "南"; break;
                case MahjongGame.West: GameScene = "西"; break;
                case MahjongGame.North: GameScene = "北"; break;
            }

            GameScene += $"{scene + 1}局";

            string SubScene = $"{subscene}本场";

            lblScene.Content = GameScene;
            lblSubScene.Content = SubScene;

            // Round
            lblRound.Content = $"余{mahjongLogic.total_rounds - mahjongLogic.round}";
        }

        public void ShowRiverCard()
        {
            for (int player = 0; player < 4; player++)
            {
                int location = GetPlayerLocation(player);
                Grid gridRiverCard = grdsRivercard[location];

                List<MahjongCard> riverCard = mahjongLogic.GetPlayerInfo(player).card_played;

                // Clear the children
                gridRiverCard.Children.Clear();

                int i = 0;
                foreach(MahjongCard card in riverCard)
                {
                    AddRiverCard(player, i, card, gridRiverCard);

                    i++;
                }
            }
        }

        public void ShowHandCard()
        {
            for (int player = 0; player < 4; player++)
            {
                int location = GetPlayerLocation(player);
                Grid gridHandCard = grdsHandcard[location];
                
                List<MahjongCard> handCard = mahjongLogic.GetPlayerCardOnHand(player);

                // Sort
                handCard.Sort((a, b) => a.name.CompareTo(b.name));

                // Clear the children
                gridHandCard.Children.Clear();

                int i = 0;
                foreach (MahjongCard card in handCard)
                {
                    AddHandCard(player, i, card, gridHandCard);

                    // Next
                    i++;
                }
            }
        }

        public void ShowFuru()
        {
            for (int player = 0; player < 4; player++)
            {
                int location = mahjongLogic.PlayerOrder.ToList().IndexOf(player);
                Grid gridFuru = grdsFuru[location];

                List<MahjongCardFuru> furuCards = mahjongLogic.player_info[player].card_furu;

                // Clear the children
                gridFuru.Children.Clear();

                int i = 0;
                foreach (MahjongCardFuru furu in furuCards)
                {
                    AddFuruCard(i, furu, gridFuru);

                    // Next
                    i++;
                }
            }
        }

        public void AddRiverCard(int player, int number, MahjongCard card, Grid container)
        {
            // Create a new card
            System.Windows.Controls.Image imgCard = new System.Windows.Controls.Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 40,
                Width = 24,
                Stretch = Stretch.Uniform,
            };

            // Set the render to high quality
            imgCard.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            // Set card position
            imgCard.Margin = new Thickness(number % 7 * imgCard.Width, number / 7 * imgCard.Height, 0, 0);

            // Set image
            imgCard.Source = cardImage[card.name];

            // Set tag
            HandCardInfo handCardInfo = new HandCardInfo()
            {
                card = card,
                player = player,
            };
            imgCard.Tag = handCardInfo;

            // Add the card
            container.Children.Add(imgCard);
        }

        private void AddHandCard(int player, int number, MahjongCard card, Grid container, int height = 75, int width = 46)
        {
            // Create a new card
            System.Windows.Controls.Image imgCard = new System.Windows.Controls.Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Height = height,
                Width = width,
                Stretch = Stretch.Uniform,
            };

            // Set the render to high quality
            imgCard.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            // Set card position
            imgCard.Margin = new Thickness((number * imgCard.Width) + 5, 10, 0, 0);

            // Set image
            imgCard.Source = cardImage[card.name];

            // Set mouse event
            imgCard.MouseEnter += HandCard_MouseEnter;
            imgCard.MouseLeave += HandCard_MouseLeave;

            // Set click event
            imgCard.MouseLeftButtonUp += HandCard_MouseLeftButtonUp;

            // Set tag
            HandCardInfo handCardInfo = new HandCardInfo()
            {
                card = card,
                player = player,
            };
            imgCard.Tag = handCardInfo;

            // Add the card
            container.Children.Add(imgCard);
        }

        private void AddFuruCard(int number, MahjongCardFuru furu, Grid container, int height = 40, int width = 24)
        {
            int i = 0;
            foreach(MahjongCard card in furu.cards)
            {
                // Create a new card
                System.Windows.Controls.Image imgCard = new System.Windows.Controls.Image()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Height = height,
                    Width = width,
                    Stretch = Stretch.Uniform,
                };

                // Set the render to high quality
                imgCard.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

                // Set card position
                imgCard.Margin = new Thickness(i * imgCard.Width, number * imgCard.Height, 0, 0);

                // Set image
                imgCard.Source = cardImage[card.name];

                // Add the card
                container.Children.Add(imgCard);

                i++;
            }
        }

        private void HandCard_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            HandCardInfo info = (HandCardInfo)(sender as System.Windows.Controls.Image).Tag;

            mahjongLogic.PlayCard(info.player, info.card);
        }

        private void HandCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as System.Windows.Controls.Image).Opacity = 1;
        }

        private void HandCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as System.Windows.Controls.Image).Opacity = 0.5;
        }

        public void PlayerActionResponseCallback(List<PlayerAction> playerActions)
        {
            playerActionGroup = from action in playerActions group action by action.playerId into g select g;

            foreach(IGrouping<int, PlayerAction> player in playerActionGroup)
            {
                bool[] actions = Enumerable.Repeat(false, 6).ToArray();
                foreach(PlayerAction action in player)
                {
                    switch (action.actionType)
                    {
                        case PlayerActionType.Ron:
                            actions[4] = true;
                            break;
                        case PlayerActionType.ChiGrade:
                            actions[0] = true;
                            break;
                        case PlayerActionType.ChiSquad:
                            actions[1] = true;
                            break;
                        case PlayerActionType.Pong:
                            actions[2] = true;
                            break;
                        case PlayerActionType.Kong:
                            actions[3] = true;
                            break;
                        case PlayerActionType.Kong_Self:
                            actions[3] = true;
                            break;
                        case PlayerActionType.Kong_Add:
                            actions[3] = true;
                            break;
                        case PlayerActionType.Tsumo:
                            actions[5] = true;
                            break;
                        case PlayerActionType.Cancel:
                            break;
                    }
                }

                if (actions.Contains(true))
                {
                   Dispatcher.Invoke(delegate { CreatePlayerActionPanel(player.Key, actions); });
                }
            }
        }

        public void PlayerActionAcceptedCallback(int player, bool accept)
        {

        }

        public PlayerActionType ConvertType(int button)
        {
            switch(button)
            {
                case 0: return PlayerActionType.ChiGrade;
                case 1: return PlayerActionType.ChiSquad;
                case 2: return PlayerActionType.Pong;
                case 3: return PlayerActionType.Kong;
                case 4: return PlayerActionType.Tsumo;
                case 5: return PlayerActionType.Ron;
                case 6: return PlayerActionType.Cancel;
                default: return PlayerActionType.Cancel;
            }
        }

        public void CreatePlayerActionPanel(int playerId, bool[] actions)
        {
            int location = GetPlayerLocation(playerId);
            Grid panel = grdsPlayerAction[location];
            panel.Children.Clear();

            string[] actions_str = { "吃（年级）", "吃（小组）", "碰", "杠", "荣", "自摸", "取消" };

            for (int i = 0; i < 6; i++)
            {
                Button btnAction = new Button()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 60,
                    Content = actions_str[i],
                    IsEnabled = actions[i],
                    Margin = new Thickness(10 + (i * 63), 10, 0, 0),
                };

                int f_i = i;

                btnAction.Click += delegate
                {
                    if ((f_i == 0) || (f_i == 1))
                    {
                        // Chi
                        IEnumerable<PlayerAction> acts = (from action in playerActionGroup where action.Key == playerId select action).First().ToList();
                        PlayerActionType type = ConvertType(f_i);
                        IEnumerable<PlayerAction> act = from action in acts where action.actionType == type select action;
                        int q = ShowChiDetermineDialog(playerId, act);
                        mahjongLogic.SendPlayerAction(act.ToList()[q]);
                        grdsPlayerAction[location].Children.Clear();
                    }
                    else
                    {
                        IEnumerable<PlayerAction> acts = (from action in playerActionGroup where action.Key == playerId select action).First().ToList();
                        PlayerActionType type = ConvertType(f_i);
                        PlayerAction act;

                        if (type == PlayerActionType.Kong)
                        {
                            act = (from action in acts
                                                where (action.actionType == PlayerActionType.Kong) || (action.actionType == PlayerActionType.Kong_Add) || (action.actionType == PlayerActionType.Kong_Self)
                                                select action).First();
                        }
                        else
                        {

                            act = (from action in acts where action.actionType == type select action).First();
                        }

                        mahjongLogic.SendPlayerAction(act);
                        grdsPlayerAction[location].Children.Clear();
                    }
                };

                panel.Children.Add(btnAction);
            }

            Button btnCancel = new Button()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 60,
                Content = actions_str[6],
                Margin = new Thickness(10 + (6 * 63), 10, 0, 0),
            };

            btnCancel.Click += delegate
            {
                mahjongLogic.SendPlayerAction(new PlayerAction(playerId) { actionType = PlayerActionType.Cancel });
                grdsPlayerAction[location].Children.Clear();
            };

            panel.Children.Add(btnCancel);
        }

        private void MnuStartNew_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private int ShowChiDetermineDialog(int playerId, IEnumerable<PlayerAction> ChiActions)
        {
            List<PlayerAction> listAction = ChiActions.ToList();

            ChiDetermineDialog dialog = new ChiDetermineDialog(playerId, ChiActions.ToList(), cardImage); 
            dialog.ShowDialog();
            return 0;
        }
    }

    class HandCardInfo
    {
        public MahjongCard card;
        public int player;
    }

    class ChiDetermineDialog : Window
    {
        Dictionary<MahjongCardName, ImageSource> cardImage;
        public int selected;

        public ChiDetermineDialog(int playerId, List<PlayerAction> listActions, Dictionary<MahjongCardName, ImageSource> cardImage)
        {
            this.cardImage = cardImage;

            int itemCount = listActions.Count;

            // Border = 5
            // Interval = 5
            // Card w = 46, h = 75
            int border = 5, interval = 5;
            int c_height = 75, c_width = 46;

            // Set the size
            Height = 2 * border + (itemCount - 1) * interval + itemCount * c_height + 20;
            Width = 2 * border + 2 * interval + 3 * c_width + 20;
            WindowStyle = WindowStyle.None;

            Grid panel = new Grid()
            {
                Margin = new Thickness(5, 5, 5, 5),
            };

            int i = 0;
            foreach(PlayerAction action in listActions)
            {
                int j = 0;
                foreach(MahjongCard card in action.effectCards)
                {
                    AddCard(i, j, card, panel, c_height, c_width, interval);
                    j++;
                }

                Button button = new Button()
                {
                    Height = c_height,
                    Width = c_width,
                    Margin = new Thickness(2 * interval + 2 * c_width, i * (c_height + interval), 0, 0),
                };

                button.Click += delegate
                {
                    selected = i;
                    Close();
                };

                panel.Children.Add(button);

                i++;
            }
            
            // Set the container to this
            this.AddChild(panel);
        }

        private void AddCard(int item, int number, MahjongCard card, Grid container, int height = 75, int width = 46, int interval = 5)
        {
            // Create a new card
            System.Windows.Controls.Image imgCard = new System.Windows.Controls.Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Height = height,
                Width = width,
                Stretch = Stretch.Uniform,
            };

            // Set the render to high quality
            imgCard.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            // Set card position
            imgCard.Margin = new Thickness(number * (imgCard.Width + interval), item * (imgCard.Height + interval), 0, 0);

            // Set image
            imgCard.Source = cardImage[card.name];

            // Add the card
            container.Children.Add(imgCard);
        }
    }
}
