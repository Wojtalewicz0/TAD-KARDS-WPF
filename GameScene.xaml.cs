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
        public GameScene(Action<SceneType> goToAction, GameState sharedState)
        {
            InitializeComponent();
            goTo = goToAction;
            state = sharedState;
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
    }
}