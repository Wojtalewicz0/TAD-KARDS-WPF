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
        public int[,] boardState = new int[5, 5]; // 0 = puste, 1 = gracz 1, 2 = gracz 2
        public Image[,] boardImages = new Image[5, 5]; // Referencje do obrazków na planszy, ułatwi to edycję efektami
        public List<listaCzegos> dane { get; set; } = new List<listaCzegos>();
        private readonly List<string> backgroundImages = new List<string> { "cover2", "cover3", "cover7", "cover6" };
        
        public async void ShowWinScreen(int winnerId)
        {
            GameBoardContainer.IsHitTestVisible = false; // Blokada klikania w planszę
            WinAnnouncementText.Text = $"Zwyciężył: {(winnerId == 1 ? Player1Name.Text : Player2Name.Text)}";
            FadeTo(WinAnnouncementText, 1, 0.5);
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
        public class listaCzegos
        {
            public int Vmax { get; set; }
            public int Steering { get; set; }
            public int Acceleration { get; set; }
            public double Braking { get; set; }
            public int Durability { get; set; }
            public string Model { get; set; } = "";
            public int RequiredPoints { get; set; }
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
                        Background = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51)), // Ciemno-szare pole
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 237, 237, 237)), // Jasna ramka #ededed
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(4), // Odstęp 4 piksele pomiędzy polami (kwadratowy efekt)
                        Cursor = Cursors.Hand,
                        Tag = new Tuple<int, int>(row, col) // Zapisujemy współrzędne pola
                    };

                    Image cellImage = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(5),
                        IsHitTestVisible = false // Dzięki temu klikalność obsłuży sam Button
                    };
                    
                    boardImages[row, col] = cellImage; // Zapisujemy obrazek w tablicy do łatwizny animacyjnej / efektów
                    
                    cell.Content = cellImage;
                    cell.Click += Cell_Click;
                    
                    GameBoard.Children.Add(cell);
                }
            }
        }

        private async void Cell_Click(object sender, RoutedEventArgs e)
        {
            // Oznaczenie pionka w zależności od tego, czyja jest aktualnie runda
            if (sender is Button clickedCell && clickedCell.Content is Image img && clickedCell.Tag is Tuple<int, int> pos)
            {
                int row = pos.Item1;
                int col = pos.Item2;

                if (boardState[row, col] == 0) // Sprawdza w naszej tablicy logiki, czy pole jest puste
                {
                    // 1. Zapisanie właściciela w logice (0 - pusto, 1 - gracz 1, 2 - gracz 2)
                    boardState[row, col] = currentPlayer;

                    // 2. Aktualizacja wyglądu kafelka
                    img.Source = new BitmapImage(new Uri(currentPlayer == 1 ? "pack://application:,,,/blue.png" : "pack://application:,,,/red.png"));
                    img.Opacity = 0.5;

                    // 3. Sprawdzanie Systemu Wygranej
                    int winner = CheckWin();
                    if (winner != 0)
                    {
                        ShowWinScreen(winner);
                        return; // Przerywamy funkcję, wygrywający zablokuje planszę, tura się nie przekazuje dalej
                    }

                    GameBoardContainer.IsHitTestVisible = false;
                    ClearTurnHighlight();
                    
                    await Task.Delay(500); // Cooldown 0.5s

                    // Po wykonaniu ruchu przekaż turę drugiemu graczowi
                    currentPlayer = currentPlayer == 1 ? 2 : 1;
                    UpdateTurnHighlight();
                    GameBoardContainer.IsHitTestVisible = true;
                }
            }
        }

        private int CheckWin()
        {
            // Zwraca 1 gdy wygrywa gracz 1, 2 gdy wygrywa 2, 0 gdy na razie nikt.
            
            // 1. Sprawdzanie wszystkich Rzędów (Poziom)
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

            // 2. Sprawdzanie wszystkich Kolumn (Pion)
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

            // 3. Sprawdzanie Przekątnych (Skos: lewy-góra w stronę prawy-dół)
            if (boardState[0, 0] != 0 &&
                boardState[0, 0] == boardState[1, 1] &&
                boardState[1, 1] == boardState[2, 2] &&
                boardState[2, 2] == boardState[3, 3] &&
                boardState[3, 3] == boardState[4, 4])
            {
                return boardState[0, 0];
            }

            // 4. Sprawdzanie Przekątnych (Skos: lewy-dół w stronę prawy-góra)
            if (boardState[4, 0] != 0 &&
                boardState[4, 0] == boardState[3, 1] &&
                boardState[3, 1] == boardState[2, 2] &&
                boardState[2, 2] == boardState[1, 3] &&
                boardState[1, 3] == boardState[0, 4])
            {
                return boardState[4, 0];
            }

            return 0; // Brak wygranej
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

            SetRandomBackground();
            LoadPlayerData();
            DetermineStartingPlayer();
            InitializeBoard();

            dane.AddRange(new[]
            {
                new listaCzegos
                {
                    Vmax = 300,
                    Steering = 990,
                    Acceleration = 7,
                    Braking = 2.5,
                    Durability = 700,
                    Model = "Ford Mustang",
                    RequiredPoints = 0
                },
                new listaCzegos
                {
                    Vmax = 270,
                    Steering = 1400,
                    Acceleration = 8,
                    Braking = 4,
                    Durability = 400,
                    Model = "Porsche 911",
                    RequiredPoints = 0
                }
            });
        }

        public Dictionary<string, MediaPlayer> activeSounds = new Dictionary<string, MediaPlayer>();
        public Dictionary<string, double> activeSoundBaseVolumes = new Dictionary<string, double>();

        public void PlaySound(string fileName, double baseVolume = 1.0)
        {
            try
            {
                MediaPlayer player;
                
                // Jeśli ten sam dźwięk już jest w zakładkach, zastopuj go by zaczął od nowa.
                // Uchroni nas to przed gubieniem referencji i problemami z pamięcią.
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

                // Zapisujemy przypisaną podstawową głośność
                activeSoundBaseVolumes[fileName] = baseVolume;

                // Odtwarzanie relatywnie do folderu binarnego aplikacji (bin/Debug/...)
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                player.Open(new Uri(path));
                
                // mediaPlayer operuje głośnością od 0.0 do 1.0; state.globalSound jest od 0 do 100.
                player.Volume = baseVolume * (state.globalSound / 100.0);
                player.Play();
            }
            catch (Exception ex)
            {
                // W ten sposób jeśli nie wrzucisz pliku lub go zgubisz, gra się nie scrashuje, tylko zignoruje ten dźwięk
            }
        }

        public async void SoundFadeTo(string fileName, double targetVolume, double seconds)
        {
            // Odrzucamy, jeśli tego pliku nie ma na liście obecnie odtwarzanych
            if (!activeSounds.ContainsKey(fileName) || !activeSoundBaseVolumes.ContainsKey(fileName)) 
                return;

            MediaPlayer player = activeSounds[fileName];
            double startVolume = activeSoundBaseVolumes[fileName];
            double difference = targetVolume - startVolume;
            
            // Określamy liczbę kroków na sekundę (np. klatkarz 30 fps) do gładkiego przejścia
            int steps = (int)(seconds * 30); 
            if (steps <= 0) steps = 1;
            int delayMs = (int)(seconds * 1000 / steps);

            for (int i = 1; i <= steps; i++)
            {
                double currentVol = startVolume + (difference * i / steps);
                activeSoundBaseVolumes[fileName] = currentVol; // Aktualizujemy tak, by np. ponowny fade wystartował ze zaktualizowanego pułapu
                
                // Ustawiamy docelowy poziom uwarunkowany ogólną głośnością całej gry z Menu
                player.Volume = currentVol * (state.globalSound / 100.0);
                await Task.Delay(delayMs);
            }
        }

        public async void GameScene_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            await RunGameSequence();
        }
        public async Task RunGameSequence()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            FadeTo(GameBackground, 0.4, 1.5);
            FadeTo(GameProfiles, 1, 2);
            
            await Task.Delay(TimeSpan.FromSeconds(1));
            
            StartAnnouncementText.Text = $"Zaczyna: {(currentPlayer == 1 ? Player1Name.Text : Player2Name.Text)}";
            FadeTo(StartAnnouncementText, 1, 0.5);
            PlaySound("drums.mp3"); // <--- Odtworzenie dźwięku za pomocą naszej nowej funkcji
            
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            FadeTo(StartAnnouncementText, 0, 0.5);

            await Task.Delay(TimeSpan.FromSeconds(0.5));
            FadeTo(GameBoardContainer, 0.85, 2);
            UpdateTurnHighlight();
        }
    }
}