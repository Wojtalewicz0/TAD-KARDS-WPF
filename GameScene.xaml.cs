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
        public List<listaCzegos> dane { get; set; } = new List<listaCzegos>();
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
            for (int i = 0; i < 25; i++)
            {
                Button cell = new Button
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51)), // Ciemno-szare pole
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 237, 237, 237)), // Jasna ramka #ededed
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(4), // Odstęp 4 piksele pomiędzy polami (kwadratowy efekt)
                    Cursor = Cursors.Hand
                };

                Image cellImage = new Image
                {
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(5),
                    IsHitTestVisible = false // Dzięki temu klikalność obsłuży sam Button
                };
                
                cell.Content = cellImage;
                cell.Click += Cell_Click;
                
                GameBoard.Children.Add(cell);
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            // Przykład ustawiania ikony (tu na próbę Gracz 1 jako placeholder logiczny)
            // W przyszłości będziesz tu wywoływał mechaniki na podstawie czyja jest runda
            if (sender is Button clickedCell && clickedCell.Content is Image img)
            {
                if (img.Source == null) // Sprawdza, czy nie jest to pole zajęte
                {
                    img.Source = Player1Icon.Source;
                }
            }
        }

        public GameScene(Action<SceneType> goToAction, GameState sharedState)
        {
            InitializeComponent();
            goTo = goToAction;
            state = sharedState;
            this.Loaded += GameScene_Loaded;
            this.Focusable = true;

            LoadPlayerData();
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
        public async void GameScene_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            await RunGameSequence();
        }
        public async Task RunGameSequence()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            FadeTo(GameBackground, 0.5, 1.5);
            FadeTo(GameUI, 1, 2);
        }
    }
}