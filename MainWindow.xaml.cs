using System.Drawing;
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
    public class GameState
    {
        public string appDataFolder;
        public string everStartedFile;
        public string p1IconFile;
        public string p2IconFile;
        public string showGuideFile;
        public string p1NameFile;
        public string p2NameFile;
        public string easterEggFile;
        public double globalSound;
        public string globalSoundFile;
        public GameState()
        {
            appDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wojtkiewicz_Projekt");
            everStartedFile = System.IO.Path.Combine(appDataFolder, "ever.tar");
            p1IconFile = System.IO.Path.Combine(appDataFolder, "p1.dat");
            p2IconFile = System.IO.Path.Combine(appDataFolder, "p2.raw");
            showGuideFile = System.IO.Path.Combine(appDataFolder, "show_guide.zsrr");
            p1NameFile = System.IO.Path.Combine(appDataFolder, "p1_name.iso");
            p2NameFile = System.IO.Path.Combine(appDataFolder, "p2_name.mp5");
            easterEggFile = System.IO.Path.Combine(appDataFolder, "sobiePlik.sobieformat");
            globalSoundFile = System.IO.Path.Combine(appDataFolder, "global_sound.KochamZSEZARY");
            globalSound = 100;
        }
    }
    public enum SceneType
    {
        Menu,
        Game
    }
    public partial class MainWindow : Window
    {
        public readonly GameState state = new();
        public readonly Dictionary<SceneType, UserControl> scenes;
        public void GoTo(SceneType scene)
        {
            SceneHost.Content = scenes[scene];
        }
        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(state.everStartedFile))
            {
                Directory.CreateDirectory(state.appDataFolder);
                File.WriteAllText(state.everStartedFile, "0");
            }
            if (!File.Exists(state.p1IconFile))
            {
                File.WriteAllText(state.p1IconFile, "1");
            }
            if (!File.Exists(state.p2IconFile))
            {
                File.WriteAllText(state.p2IconFile, "2");
            }
            if (!File.Exists(state.showGuideFile))
            {
                File.WriteAllText(state.showGuideFile, "0");
            }
            if (!File.Exists(state.p1NameFile))
            {
                File.WriteAllText(state.p1NameFile, "Gracz 1");
            }
            if (!File.Exists(state.p2NameFile))
            {
                File.WriteAllText(state.p2NameFile, "Gracz 2");
            }
            if (!File.Exists(state.easterEggFile))
            {
                File.WriteAllText(state.easterEggFile, "sobie_zawartosc");
            }
            if (!File.Exists(state.globalSoundFile))
            {
                File.WriteAllText(state.globalSoundFile, state.globalSound.ToString());
            }
            state.globalSound = double.Parse(File.ReadAllText(state.globalSoundFile));
            scenes = new Dictionary<SceneType, UserControl>
            {
                { SceneType.Menu, new MenuScene(GoTo, state) },
                { SceneType.Game, new GameScene(GoTo, state) }
            };
            GoTo(SceneType.Menu);
        }
    }
}