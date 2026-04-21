using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Animation;
namespace TADprojekt
{
    public partial class GameScene : UserControl
    {
        private readonly Action<SceneType> goTo;
        private readonly GameState state;
        public int currentPlayer = 1;
        public bool draw = false;
        public int player1Moves = 0;
        public int player2Moves = 0;
        public bool isPlayer1CardAvailable = false;
        public bool isPlayer2CardAvailable = false;
        public GameCard? player1CurrentCard;
        public GameCard? player2CurrentCard;
        public bool isAwaitingTargetCell = false;
        public int remainingTargetCellsCount = 0;
        public int[,] boardState = new int[5, 5];
        public Image[,] boardImages = new Image[5, 5];
        public List<GameCard> CardsList { get; set; } = new List<GameCard>();
        public enum CardMechanic
        {
            None,
            DestroySingleCell,
            DestroyRow,
            RevealFog,
            DestroyRandomCells,
            DestroyAll,
            ResetOpponentMoves,
            DestroyCenter3x3,
            DestroyBorders,
            HorizontalCarpetBombing,
            VerticalCarpetBombing,
            DestroyTwoCells
        }
        public enum TargetRestriction
        {
            Any,
            EnemyOnly,
            OwnAndEmpty
        }
        public class GameCard
        {
            public string FileName { get; set; } = "";
            public int NumberOfVarations { get; set; }
            public bool CanMoveAgain { get; set; }
            public double DropWeight { get; set; } = 1.0;
            public CardMechanic Mechanic { get; set; }
            public TargetRestriction AllowedTargets { get; set; }
        }
        private readonly List<string> backgroundImages = new List<string> { "cover2", "cover3", "cover7", "cover1", "cover5" };
        public void PlaySound(string fileName, double baseVolume = 1.0, bool loop = false)
        {
            try
            {
                MediaPlayer player;
                if (activeSounds.ContainsKey(fileName))
                {
                    player = activeSounds[fileName];
                    player.Stop();
                }
                else
                {
                    player = new MediaPlayer();
                    activeSounds[fileName] = player;
                }
                activeSoundBaseVolumes[fileName] = baseVolume;
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                player.Open(new Uri(path));
                if (loop)
                {
                    player.MediaEnded += (s, e) =>
                    {
                        player.Position = TimeSpan.Zero;
                        player.Play();
                    };
                }
                player.Volume = baseVolume * (state.globalSound / 100.0);
                player.Play();
            }
            catch (Exception ex){}
        }
        public void StopSound(string fileName)
        {
            if (activeSounds.ContainsKey(fileName))
            {
                activeSounds[fileName].Stop();
                activeSounds.Remove(fileName);
                activeSoundBaseVolumes.Remove(fileName);
            }
        }
        public async void SoundFadeTo(string fileName, double targetVolume, double seconds)
        {
            if (!activeSounds.ContainsKey(fileName) || !activeSoundBaseVolumes.ContainsKey(fileName))
                return;
            MediaPlayer player = activeSounds[fileName];
            double startVolume = activeSoundBaseVolumes[fileName];
            double difference = targetVolume - startVolume;
            int steps = (int)(seconds * 30);
            if (steps <= 0) steps = 1;
            int delayMs = (int)(seconds * 1000 / steps);
            for (int i = 1; i <= steps; i++)
            {
                double currentVol = startVolume + (difference * i / steps);
                activeSoundBaseVolumes[fileName] = currentVol;
                player.Volume = currentVol * (state.globalSound / 100.0);
                await Task.Delay(delayMs);
            }
        }
        public async void ShowWinScreen(int winnerId)
        {
            GameBoardContainer.IsHitTestVisible = false;
            ClearTurnHighlight();
            SoundFadeTo("bf1942.mp3", 0.0, 0.6);
            if (draw == false) {
                WinAnnouncementText.Text = $"Zwyciężył: {(winnerId == 1 ? Player1Name.Text : Player2Name.Text)}";
            } else {
                WinAnnouncementText.Text = $"Remis!";
            }
            FadeTo(WinAnnouncementText, 1, 2.5);
            PlaySound("hd3.mp3", 0.0);
            SoundFadeTo("hd3.mp3", 1.0, 1.0);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            StopSound("bf1942.mp3");
            await Task.Delay(TimeSpan.FromSeconds(9));
            FadeTo(WinAnnouncementText, 0, 6.0);
            FadeTo(GameBoardContainer, 0, 6.0);
            FadeTo(GameProfiles, 0, 6.0);
            FadeTo(GameBackground, 0, 6.0);
            await Task.Delay(TimeSpan.FromSeconds(6));
            SoundFadeTo("hd3.mp3", 0.0, 1.0);
            await Task.Delay(TimeSpan.FromSeconds(1));
            ResetGame();
            StopSound("hd3.mp3");
            state.gameFinishedOnce = 1;
            goTo(SceneType.Menu);
        }
        private void ResetGame()
        {
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    boardState[row, col] = 0;
                    if (boardImages[row, col] != null)
                    {
                        boardImages[row, col].Source = null;
                        boardImages[row, col].Opacity = 1.0;
                    }
                }
            }
            ClearTurnHighlight();
            WinAnnouncementText.Text = "";
            WinAnnouncementText.Opacity = 0;
            SetRandomBackground();
            DetermineStartingPlayer();
            LoadPlayerData();
            ResetCardCounters();
            GameBoardContainer.IsHitTestVisible = true;
        }
        private void ResetCardCounters()
        {
            player1Moves = 0;
            player2Moves = 0;
            isPlayer1CardAvailable = false;
            isPlayer2CardAvailable = false;
            Player1CardCounter.Text = "Do Zagrania: 0 / 3";
            Player2CardCounter.Text = "Do Zagrania: 0 / 3";
            SolidColorBrush grayBrush = new SolidColorBrush(Colors.Gray);
            Player1PlayCardButton.Foreground = grayBrush;
            Player1PlayCardButton.BorderBrush = grayBrush;
            Player2PlayCardButton.Foreground = grayBrush;
            Player2PlayCardButton.BorderBrush = grayBrush;
            Player1CardImage.Source = new BitmapImage(new Uri("pack://application:,,,/reverse.png"));
            Player1CardImage.Opacity = 0.8;
            Player2CardImage.Source = new BitmapImage(new Uri("pack://application:,,,/reverse.png"));
            Player2CardImage.Opacity = 0.8;
        }
        private void ResetPlayerCard(int playerId, bool keepMoves)
        {
            SolidColorBrush grayBrush = new SolidColorBrush(Colors.Gray);
            if (playerId == 1)
            {
                isPlayer1CardAvailable = false;
                Player1PlayCardButton.Foreground = grayBrush;
                Player1PlayCardButton.BorderBrush = grayBrush;
                Player1CardImage.Source = new BitmapImage(new Uri("pack://application:,,,/reverse.png"));
                Player1CardImage.Opacity = 0.8;
                if (!keepMoves)
                {
                    player1Moves = 0;
                    Player1CardCounter.Text = $"Do Zagrania: {player1Moves} / 3";
                }
            }
            else if (playerId == 2)
            {
                isPlayer2CardAvailable = false;
                Player2PlayCardButton.Foreground = grayBrush;
                Player2PlayCardButton.BorderBrush = grayBrush;
                Player2CardImage.Source = new BitmapImage(new Uri("pack://application:,,,/reverse.png"));
                Player2CardImage.Opacity = 0.8;
                if (!keepMoves)
                {
                    player2Moves = 0;
                    Player2CardCounter.Text = $"Do Zagrania: {player2Moves} / 3";
                }
            }
        }
        private async void ExecuteCardEffect(int playerId, GameCard card)
        {
            ResetPlayerCard(playerId, false);
            GameBoardContainer.IsHitTestVisible = false;
            ClearTurnHighlight();
            await Task.Delay(500);
            if (card != null)
            {
                if (card.Mechanic == CardMechanic.DestroyRandomCells)
                {
                    List<Tuple<int, int>> allCells = new List<Tuple<int, int>>();
                    for (int r = 0; r < 5; r++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            allCells.Add(new Tuple<int, int>(r, c));
                        }
                    }
                    for (int i = 0; i < allCells.Count; i++)
                    {
                        int swapIdx = _randomCardGenerator.Next(allCells.Count);
                        var temp = allCells[i];
                        allCells[i] = allCells[swapIdx];
                        allCells[swapIdx] = temp;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int row = allCells[i].Item1;
                        int col = allCells[i].Item2;
                        boardState[row, col] = 0;
                        boardImages[row, col].Source = null;
                        BoomEffect(row, col);
                    }
                    PlaySound("smallboom.mp3", 1.0);
                    await Task.Delay(1000);
                }
                else if (card.Mechanic == CardMechanic.DestroySingleCell)
                {
                    isAwaitingTargetCell = true;
                    UpdateTurnHighlight();
                    GameBoardContainer.IsHitTestVisible = true;
                    return;
                }
                else if (card.Mechanic == CardMechanic.DestroyTwoCells)
                {
                    remainingTargetCellsCount = 2;
                    UpdateTurnHighlight();
                    GameBoardContainer.IsHitTestVisible = true;
                    return;
                }
                else if (card.Mechanic == CardMechanic.DestroyAll)
                {
                    for (int r = 0; r < 5; r++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            boardState[r, c] = 0;
                            boardImages[r, c].Source = null;
                        }
                    }
                    BoomEffect(2, 2, 11.0);
                    PlaySound("bigboom.mp3", 1.0);
                    await Task.Delay(1500);
                }
                else if (card.Mechanic == CardMechanic.ResetOpponentMoves)
                {
                    int opponentId = playerId == 1 ? 2 : 1;
                    ResetPlayerCard(opponentId, false);
                    if (opponentId == 1)
                    {
                        Player1CardCounter.Text = "Do Zagrania: 0 / 0";
                    }
                    else
                    {
                        Player2CardCounter.Text = "Do Zagrania: 0 / 0";
                    }
                    UpdateTurnHighlight();
                    CheckForDraw();
                    GameBoardContainer.IsHitTestVisible = true;
                    return;
                }
                else if (card.Mechanic == CardMechanic.DestroyCenter3x3)
                {
                    for (int r = 1; r <= 3; r++)
                    {
                        for (int c = 1; c <= 3; c++)
                        {
                            boardState[r, c] = 0;
                            boardImages[r, c].Source = null;
                        }
                    }
                    BoomEffect(2, 2, 6.0);
                    PlaySound("bigboom.mp3", 1.0);
                    await Task.Delay(1200);
                }
                else if (card.Mechanic == CardMechanic.DestroyBorders)
                {
                    for (int r = 0; r < 5; r++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            if (r == 0 || r == 4 || c == 0 || c == 4)
                            {
                                boardState[r, c] = 0;
                                boardImages[r, c].Source = null;
                                BoomEffect(r, c, 2.0);
                            }
                        }
                    }
                    PlaySound("smallboom.mp3", 1.0);
                    await Task.Delay(1200);
                }
                else if (card.Mechanic == CardMechanic.HorizontalCarpetBombing)
                {
                    for (int r = 0; r < 5; r++)
                    {
                        bool shouldDestroy = false;
                        if (card.FileName == "parzystyhorizontalbomber" && (r == 1 || r == 3))
                        {
                            shouldDestroy = true;
                        }
                        else if (card.FileName == "nieparzystyhorizontalbomber" && (r == 0 || r == 2 || r == 4))
                        {
                            shouldDestroy = true;
                        }
                        if (shouldDestroy)
                        {
                            for (int c = 0; c < 5; c++)
                            {
                                boardState[r, c] = 0;
                                boardImages[r, c].Source = null;
                                BoomEffect(r, c, 2.0);
                            }
                        }
                    }
                    PlaySound("smallboom.mp3", 1.0);
                    await Task.Delay(1200);
                }
                else if (card.Mechanic == CardMechanic.VerticalCarpetBombing)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        bool shouldDestroy = false;
                        if (card.FileName == "parzystyverticalbomber" && (c == 1 || c == 3))
                        {
                            shouldDestroy = true;
                        }
                        else if (card.FileName == "nieparzystyverticalbomber" && (c == 0 || c == 2 || c == 4))
                        {
                            shouldDestroy = true;
                        }
                        if (shouldDestroy)
                        {
                            for (int r = 0; r < 5; r++)
                            {
                                boardState[r, c] = 0;
                                boardImages[r, c].Source = null;
                                BoomEffect(r, c, 2.0);
                            }
                        }
                    }
                    PlaySound("smallboom.mp3", 1.0);
                    await Task.Delay(1200);
                }
            }
            currentPlayer = currentPlayer == 1 ? 2 : 1;
            UpdateTurnHighlight();
            CheckTurnStartCards(currentPlayer);
            CheckForDraw();
            GameBoardContainer.IsHitTestVisible = true;
        }
        private void PlayCard1_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayer1CardAvailable) return;
            ExecuteCardEffect(1, player1CurrentCard);
        }
        private void PlayCard2_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayer2CardAvailable) return;
            ExecuteCardEffect(2, player2CurrentCard);
        }
        private void Player1Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isPlayer1CardAvailable)
            {
                PlaySound("card.mp3", 1.0);
                LargeCardPreviewImage.Source = Player1CardImage.Source;
                CardPreviewOverlay.Visibility = Visibility.Visible;
            }
        }
        private void Player2Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isPlayer2CardAvailable)
            {
                PlaySound("card.mp3", 1.0);
                LargeCardPreviewImage.Source = Player2CardImage.Source;
                CardPreviewOverlay.Visibility = Visibility.Visible;
            }
        }
        private void CardPreviewOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CardPreviewOverlay.Visibility = Visibility.Collapsed;
        }
        public void BoomEffect(int row, int col, double scaleRatio = 2.1)
        {
            double cellSize = 154.0;
            double targetSize = cellSize * scaleRatio;
            Ellipse burst = new Ellipse
            {
                Width = targetSize,
                Height = targetSize,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(burst, (col * cellSize + cellSize / 2) - targetSize / 2);
            Canvas.SetTop(burst, (row * cellSize + cellSize / 2) - targetSize / 2);
            GradientStop centerStop = new GradientStop(Colors.Yellow, 0.0);
            GradientStop midStop = new GradientStop(Colors.DarkOrange, 0.4);
            GradientStop edgeStop = new GradientStop(Color.FromArgb(0, 30, 30, 30), 1.0);
            RadialGradientBrush radialBrush = new RadialGradientBrush();
            radialBrush.GradientStops.Add(centerStop);
            radialBrush.GradientStops.Add(midStop);
            radialBrush.GradientStops.Add(edgeStop);
            burst.Fill = radialBrush;
            ScaleTransform scaleTransform = new ScaleTransform(0, 0, targetSize / 2, targetSize / 2);
            burst.RenderTransform = scaleTransform;
            EffectsCanvas.Children.Add(burst);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation scaleAnimation = new DoubleAnimation(0.1, 1.0, TimeSpan.FromSeconds(0.7));
            Storyboard.SetTarget(scaleAnimation, burst);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
            storyboard.Children.Add(scaleAnimation);
            DoubleAnimation scaleAnimationY = new DoubleAnimation(0.1, 1.0, TimeSpan.FromSeconds(0.7));
            Storyboard.SetTarget(scaleAnimationY, burst);
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
            storyboard.Children.Add(scaleAnimationY);
            ColorAnimation colorYellowToOrange = new ColorAnimation(Colors.Yellow, Colors.Orange, TimeSpan.FromSeconds(0.1));
            Storyboard.SetTarget(colorYellowToOrange, burst);
            Storyboard.SetTargetProperty(colorYellowToOrange, new PropertyPath("Fill.GradientStops[0].Color"));
            storyboard.Children.Add(colorYellowToOrange);
            ColorAnimation colorOrangeToGray = new ColorAnimation(Colors.Orange, Color.FromArgb(255, 90, 90, 90), TimeSpan.FromSeconds(0.6)) { BeginTime = TimeSpan.FromSeconds(0.1) };
            Storyboard.SetTarget(colorOrangeToGray, burst);
            Storyboard.SetTargetProperty(colorOrangeToGray, new PropertyPath("Fill.GradientStops[0].Color"));
            storyboard.Children.Add(colorOrangeToGray);
            ColorAnimation midOrangeToGray = new ColorAnimation(Colors.DarkOrange, Color.FromArgb(255, 70, 70, 70), TimeSpan.FromSeconds(0.7));
            Storyboard.SetTarget(midOrangeToGray, burst);
            Storyboard.SetTargetProperty(midOrangeToGray, new PropertyPath("Fill.GradientStops[1].Color"));
            storyboard.Children.Add(midOrangeToGray);
            DoubleAnimation fadeAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(0.3)) { BeginTime = TimeSpan.FromSeconds(0.7) };
            Storyboard.SetTarget(fadeAnimation, burst);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeAnimation);
            storyboard.Completed += (s, ev) =>
            {
                EffectsCanvas.Children.Remove(burst);
            };
            storyboard.Begin();
        }
        public void SlideIn(UIElement element, TranslateTransform transform, double seconds)
        {
            DoubleAnimation slide = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(seconds)
            };
            transform.BeginAnimation(TranslateTransform.YProperty, slide);
        }
        public void FadeTo(UIElement element, double targetOpacity, double seconds)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromSeconds(seconds),
            };
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        public void ScaleElement(ScaleTransform transform, double toScale, double seconds)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = toScale,
                Duration = TimeSpan.FromSeconds(seconds)
            };
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
        private void ClearTurnHighlight()
        {
            DoubleAnimation anim1 = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromSeconds(0.1) };
            Player1Glow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, anim1);
            DoubleAnimation anim2 = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromSeconds(0.1) };
            Player2Glow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, anim2);
        }
        private void UpdateTurnHighlight()
        {
            DoubleAnimation anim1 = new DoubleAnimation { To = currentPlayer == 1 ? 1.0 : 0.0, Duration = TimeSpan.FromSeconds(0.3) };
            Player1Glow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, anim1);
            DoubleAnimation anim2 = new DoubleAnimation { To = currentPlayer == 2 ? 1.0 : 0.0, Duration = TimeSpan.FromSeconds(0.3) };
            Player2Glow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, anim2);
        }
        private void CheckForDraw()
        {
            if (CheckWin() != 0) return;
            bool isFull = true;
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    if (boardState[r, c] == 0)
                    {
                        isFull = false;
                        break;
                    }
                }
                if (!isFull) break;
            }
            if (isFull)
            {
                bool currentPlayerHasCard = (currentPlayer == 1 && isPlayer1CardAvailable) || (currentPlayer == 2 && isPlayer2CardAvailable);
                if (!currentPlayerHasCard)
                {
                    draw = true;
                    ShowWinScreen(0);
                }
            }
        }
        private void CheckTurnStartCards(int playerId)
        {
            SolidColorBrush whiteBrush = new SolidColorBrush(Colors.White);
            SolidColorBrush lightGrayBrush = new SolidColorBrush(Color.FromRgb(237, 237, 237));
            if (playerId == 1 && player1Moves >= 3 && !isPlayer1CardAvailable)
            {
                isPlayer1CardAvailable = true;
                Player1PlayCardButton.Foreground = whiteBrush;
                Player1PlayCardButton.BorderBrush = lightGrayBrush;
                DrawRandomCard(1);
            }
            else if (playerId == 2 && player2Moves >= 3 && !isPlayer2CardAvailable)
            {
                isPlayer2CardAvailable = true;
                Player2PlayCardButton.Foreground = whiteBrush;
                Player2PlayCardButton.BorderBrush = lightGrayBrush;
                DrawRandomCard(2);
            }
        }
        private void LoadPlayerData()
        {
            if (File.Exists(state.p1NameFile))
                Player1Name.Text = File.ReadAllText(state.p1NameFile);
            if (File.Exists(state.p1IconFile))
            {
                string id1 = File.ReadAllText(state.p1IconFile).Trim();
                Player1Icon.Source = new BitmapImage(new Uri($"/icon{id1}.png", UriKind.Relative));
            }
            if (File.Exists(state.p2NameFile))
                Player2Name.Text = File.ReadAllText(state.p2NameFile);
            if (File.Exists(state.p2IconFile))
            {
                string id2 = File.ReadAllText(state.p2IconFile).Trim();
                Player2Icon.Source = new BitmapImage(new Uri($"/icon{id2}.png", UriKind.Relative));
            }
        }
        private void InitializeBoard()
        {
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    Button cell = new Button
                    {
                        Background = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 237, 237, 237)),
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(4),
                        Cursor = Cursors.Hand,
                        Tag = new Tuple<int, int>(row, col)
                    };
                    Image cellImage = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(5),
                        IsHitTestVisible = false
                    };
                    boardImages[row, col] = cellImage;
                    cell.Content = cellImage;
                    cell.Click += Cell_Click;
                    GameBoard.Children.Add(cell);
                }
            }
        }
        private async void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedCell && clickedCell.Content is Image img && clickedCell.Tag is Tuple<int, int> pos)
            {
                int row = pos.Item1;
                int col = pos.Item2;
                if (isAwaitingTargetCell)
                {
                    isAwaitingTargetCell = false;
                    boardState[row, col] = 0;
                    img.Source = null;
                    BoomEffect(row, col);
                    PlaySound("smallboom.mp3", 1.0);
                    return;
                }
                if (remainingTargetCellsCount > 0)
                {
                    remainingTargetCellsCount--;
                    boardState[row, col] = 0;
                    img.Source = null;
                    BoomEffect(row, col, 2.0);
                    PlaySound("smallboom.mp3", 1.0);
                    if (remainingTargetCellsCount == 0)
                    {
                        currentPlayer = currentPlayer == 1 ? 2 : 1;
                        UpdateTurnHighlight();
                        CheckTurnStartCards(currentPlayer);
                        CheckForDraw();
                    }
                    return;
                }
                if (boardState[row, col] == 0)
                {
                    if (currentPlayer == 1 && isPlayer1CardAvailable)
                    {
                        ResetPlayerCard(1, true);
                    }
                    else if (currentPlayer == 2 && isPlayer2CardAvailable)
                    {
                        ResetPlayerCard(2, true);
                    }
                    boardState[row, col] = currentPlayer;
                    UpdateCardProgress(currentPlayer);
                    img.Source = new BitmapImage(new Uri(currentPlayer == 1 ? "pack://application:,,,/blue.png" : "pack://application:,,,/red.png"));
                    img.Opacity = 0.5;
                    int winner = CheckWin();
                    if (winner != 0)
                    {
                        ShowWinScreen(winner);
                        return;
                    }
                    GameBoardContainer.IsHitTestVisible = false;
                    ClearTurnHighlight();
                    await Task.Delay(500);
                    currentPlayer = currentPlayer == 1 ? 2 : 1;
                    UpdateTurnHighlight();
                    CheckTurnStartCards(currentPlayer);
                    CheckForDraw();
                    GameBoardContainer.IsHitTestVisible = true;
                }
            }
        }
        private Random _randomCardGenerator = new Random();
        private void DrawRandomCard(int playerId)
        {
            if (CardsList.Count == 0) return;
            double totalWeight = 0;
            foreach (var card in CardsList)
            {
                totalWeight += card.DropWeight;
            }
            double randomValue = _randomCardGenerator.NextDouble() * totalWeight;
            GameCard selectedCard = CardsList[0];
            double cumulativeWeight = 0;
            foreach (var card in CardsList)
            {
                cumulativeWeight += card.DropWeight;
                if (randomValue < cumulativeWeight)
                {
                    selectedCard = card;
                    break;
                }
            }
            int variation = _randomCardGenerator.Next(1, selectedCard.NumberOfVarations + 1);
            string imagePath = $"pack://application:,,,/{selectedCard.FileName}{variation}.png";
            if (playerId == 1)
            {
                player1CurrentCard = selectedCard;
                Player1CardImage.Source = new BitmapImage(new Uri(imagePath));
                Player1CardImage.Opacity = 1.0;
            }
            else if (playerId == 2)
            {
                player2CurrentCard = selectedCard;
                Player2CardImage.Source = new BitmapImage(new Uri(imagePath));
                Player2CardImage.Opacity = 1.0;
            }
        }
        private void UpdateCardProgress(int playerId)
        {
            if (playerId == 1)
            {
                if (player1Moves < 3)
                {
                    player1Moves++;
                }
                Player1CardCounter.Text = $"Do Zagrania: {player1Moves} / 3";
            }
            else if (playerId == 2)
            {
                if (player2Moves < 3)
                {
                    player2Moves++;
                }
                Player2CardCounter.Text = $"Do Zagrania: {player2Moves} / 3";
            }
        }
        private int CheckWin()
        {
            for (int row = 0; row < 5; row++)
            {
                if (boardState[row, 0] != 0 &&
                    boardState[row, 0] == boardState[row, 1] &&
                    boardState[row, 1] == boardState[row, 2] &&
                    boardState[row, 2] == boardState[row, 3] &&
                    boardState[row, 3] == boardState[row, 4])
                {
                    return boardState[row, 0];
                }
            }
            for (int col = 0; col < 5; col++)
            {
                if (boardState[0, col] != 0 &&
                    boardState[0, col] == boardState[1, col] &&
                    boardState[1, col] == boardState[2, col] &&
                    boardState[2, col] == boardState[3, col] &&
                    boardState[3, col] == boardState[4, col])
                {
                    return boardState[0, col];
                }
            }
            if (boardState[0, 0] != 0 &&
                boardState[0, 0] == boardState[1, 1] &&
                boardState[1, 1] == boardState[2, 2] &&
                boardState[2, 2] == boardState[3, 3] &&
                boardState[3, 3] == boardState[4, 4])
            {
                return boardState[0, 0];
            }
            if (boardState[4, 0] != 0 &&
                boardState[4, 0] == boardState[3, 1] &&
                boardState[3, 1] == boardState[2, 2] &&
                boardState[2, 2] == boardState[1, 3] &&
                boardState[1, 3] == boardState[0, 4])
            {
                return boardState[4, 0];
            }
            return 0;
        }
        private void SetRandomBackground()
        {
            Random rnd = new Random();
            string selectedBg = backgroundImages[rnd.Next(backgroundImages.Count)];
            GameBackground.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri($"pack://application:,,,/{selectedBg}.jpg")),
                Stretch = Stretch.UniformToFill
            };
        }
        private void DetermineStartingPlayer()
        {
            if (File.Exists(state.randomStartFile) && File.ReadAllText(state.randomStartFile).Trim() == "0")
            {
                Random rnd = new Random();
                currentPlayer = rnd.Next(1, 3);
            }
            else
            {
                currentPlayer = 1;
            }
        }
        public GameScene(Action<SceneType> goToAction, GameState sharedState)
        {
            InitializeComponent();
            goTo = goToAction;
            state = sharedState;
            this.Loaded += GameScene_Loaded;
            this.Focusable = true;
            this.KeyDown += GameScene_KeyDown;
            SetRandomBackground();
            LoadPlayerData();
            DetermineStartingPlayer();
            InitializeBoard();
            CardsList.AddRange(new[]
            {
                new GameCard
                {
                    FileName = "smallbomber",
                    NumberOfVarations = 9,
                    CanMoveAgain = true,
                    DropWeight = 2,
                    Mechanic = CardMechanic.DestroySingleCell,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "randombomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.DestroyRandomCells,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "wholebomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.DestroyAll,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "brainbomber",
                    NumberOfVarations = 4,
                    CanMoveAgain = true,
                    DropWeight = 1,
                    Mechanic = CardMechanic.ResetOpponentMoves,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "heavybomber",
                    NumberOfVarations = 4,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.DestroyCenter3x3,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "mediumbomber",
                    NumberOfVarations = 8,
                    CanMoveAgain = false,
                    DropWeight = 2,
                    Mechanic = CardMechanic.DestroyTwoCells,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "heavyaroundbomber",
                    NumberOfVarations = 3,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.DestroyBorders,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "parzystyhorizontalbomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.HorizontalCarpetBombing,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "nieparzystyhorizontalbomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.HorizontalCarpetBombing,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "parzystyverticalbomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.VerticalCarpetBombing,
                    AllowedTargets = TargetRestriction.Any
                },
                new GameCard
                {
                    FileName = "nieparzystyverticalbomber",
                    NumberOfVarations = 2,
                    CanMoveAgain = false,
                    DropWeight = 1,
                    Mechanic = CardMechanic.VerticalCarpetBombing,
                    AllowedTargets = TargetRestriction.Any
                }
            });
        }
        public Dictionary<string, MediaPlayer> activeSounds = new Dictionary<string, MediaPlayer>();
        public Dictionary<string, double> activeSoundBaseVolumes = new Dictionary<string, double>();
        public async void GameScene_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            await RunGameSequence();
        }
        private void SurrenderButton_Click(object sender, RoutedEventArgs e)
        {
            int winnerId = currentPlayer == 1 ? 2 : 1;
            ShowWinScreen(winnerId);
        }
        public async Task RunGameSequence()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            FadeTo(GameBackground, 0.37, 1.5);
            FadeTo(GameProfiles, 1, 2);
            await Task.Delay(TimeSpan.FromSeconds(1));
            StartAnnouncementText.Text = $"Zaczyna: {(currentPlayer == 1 ? Player1Name.Text : Player2Name.Text)}";
            FadeTo(StartAnnouncementText, 1, 0.5);
            PlaySound("drums.mp3");
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            FadeTo(StartAnnouncementText, 0, 0.5);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            FadeTo(GameBoardContainer, 0.85, 2);
            FadeTo(SurrenderButton, 1, 2);
            UpdateTurnHighlight();
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            PlaySound("bf1942.mp3", 0.0, true);
            SoundFadeTo("bf1942.mp3", 1.0, 1.0);
            StopSound("drums.mp3");
        }
        private void GameScene_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (activeSoundBaseVolumes.TryGetValue("bf1942.mp3", out double vol) && vol > 0)
                {
                    if (RandomCardPreviewOverlay.Visibility == Visibility.Visible)
                    {
                        RandomCardPreviewOverlay.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        PopulateAllCardsGrid();
                        RandomCardPreviewOverlay.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        private void PopulateAllCardsGrid()
        {
            AllCardsGrid.Children.Clear();
            List<UIElement> cardElements = new List<UIElement>();
            foreach (var card in CardsList)
            {
                int variants = card.NumberOfVarations > 0 ? card.NumberOfVarations : 1;
                for (int i = 1; i <= variants; i++)
                {
                    string variation = card.NumberOfVarations > 1 ? i.ToString() : "";
                    string imagePath = $"pack://application:,,,/{card.FileName}{variation}.png";
                    try
                    {
                        Border cardBorder = new Border
                        {
                            CornerRadius = new CornerRadius(10),
                            ClipToBounds = true,
                            Margin = new Thickness(10, 15, 10, 15),
                            Width = 240,
                            Height = 336,
                            BorderBrush = new SolidColorBrush(Colors.Gray),
                            BorderThickness = new Thickness(2)
                        };
                        Image img = new Image
                        {
                            Source = new BitmapImage(new Uri(imagePath)),
                            Stretch = Stretch.UniformToFill
                        };
                        cardBorder.Child = img;
                        cardElements.Add(cardBorder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Błąd wczytywania obrazka: " + ex.Message);
                    }
                }
            }
            var random = new Random();
            int n = cardElements.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                UIElement value = cardElements[k];
                cardElements[k] = cardElements[n];
                cardElements[n] = value;
            }
            foreach (var element in cardElements)
            {
                AllCardsGrid.Children.Add(element);
            }
        }
        private void InnerOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
        private void RandomCardPreviewOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RandomCardPreviewOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
