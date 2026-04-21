using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
namespace TADprojekt
{
    public partial class MenuScene : UserControl
    {
        public Action<SceneType> goTo;
        public GameState state;
        public bool isSpacePressed = false;
        public bool titleStarted = false;
        public int currentEditingPlayer = 1;
        public int whatMenu = 0;
        public double dev = 1;
        public Dictionary<string, MediaPlayer> activeSounds = new Dictionary<string, MediaPlayer>();
        public Dictionary<string, double> activeSoundBaseVolumes = new Dictionary<string, double>();
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
            catch (Exception ex)
            {
            }
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
        private void MenuScene_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && !isSpacePressed && titleStarted)
            {
                isSpacePressed = true;
                ShowMenu();
            }
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
        private void UpdatePlayersDisplay()
        {
            Player1NameText.Text = File.ReadAllText(state.p1NameFile);
            string id1 = File.ReadAllText(state.p1IconFile).Trim();
            Player1IconImage.Source = new BitmapImage(new Uri($"/icon{id1}.png", UriKind.Relative));
            Player2NameText.Text = File.ReadAllText(state.p2NameFile);
            string id2 = File.ReadAllText(state.p2IconFile).Trim();
            Player2IconImage.Source = new BitmapImage(new Uri($"/icon{id2}.png", UriKind.Relative));
        }
        private void Player1_Click(object sender, MouseButtonEventArgs e)
        {
            OpenPlayerEdit(1);
        }
        private void Player2_Click(object sender, MouseButtonEventArgs e)
        {
            OpenPlayerEdit(2);
        }
        private void OpenPlayerEdit(int playerId)
        {
            currentEditingPlayer = playerId;
            PlayerEditTitle.Text = playerId == 1 ? "EDYCJA GRACZA 1" : "EDYCJA GRACZA 2";
            PlayerNameInput.Text = File.ReadAllText(playerId == 1 ? state.p1NameFile : state.p2NameFile);
            ConfigMainView.Visibility = Visibility.Collapsed;
            PlayerEditView.Visibility = Visibility.Visible;
            UpdateIconSelectionUI();
        }
        private void UpdateIconSelectionUI()
        {
            SelectIcon1.Opacity = 1.0;
            SelectIcon2.Opacity = 1.0;
            SelectIcon3.Opacity = 1.0;
            SelectIcon4.Opacity = 1.0;
            SelectIcon5.Opacity = 1.0;
            SelectIcon6.Opacity = 1.0;
            string selectedIconId = File.ReadAllText(currentEditingPlayer == 1 ? state.p1IconFile : state.p2IconFile).Trim();
            if (FindName("SelectIcon" + selectedIconId) is Image selectedImage)
            {
                selectedImage.Opacity = 0.4;
            }
        }
        private void SelectIcon_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                string iconId = clickedImage.Name.Replace("SelectIcon", "");
                if (currentEditingPlayer == 1)
                {
                    File.WriteAllText(state.p1IconFile, iconId);
                }
                else
                {
                    File.WriteAllText(state.p2IconFile, iconId);
                }
                UpdateIconSelectionUI();
            }
        }
        private void SavePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = PlayerNameInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                return;
            }
            if (currentEditingPlayer == 1)
            {
                File.WriteAllText(state.p1NameFile, newName);
            }
            else
            {
                File.WriteAllText(state.p2NameFile, newName);
            }
            UpdatePlayersDisplay();
            PlayerEditView.Visibility = Visibility.Collapsed;
            ConfigMainView.Visibility = Visibility.Visible;
        }
        public async void MenuButton2_Click(object sender, RoutedEventArgs e)
        {
            if (whatMenu == 0) {
                MenuButton2.IsEnabled = false;
                SoundFadeTo("biahh.mp3", 0.0, 1.5);
                SoundFadeTo("mohaa.mp3", 0.0, 1.5);
                SoundFadeTo("plane.mp3", 0.0, 1.5);
                FadeTo(MenuBackground, 0, 1);
                FadeTo(LogoImage3, 0, 1);
                FadeTo(VersionText, 0, 1);
                FadeTo(MenuButton1, 0, 1);
                FadeTo(MenuButton2, 0, 1);
                FadeTo(MenuButton3, 0, 1);
                await Task.Delay(TimeSpan.FromSeconds(2));
                StopSound("biahh.mp3");
                StopSound("mohaa.mp3");
                StopSound("plane.mp3");
                goTo(SceneType.Game);
            }
        }
        private void MenuButton3_Click(object sender, RoutedEventArgs e)
        {
            if (whatMenu == 0)
            {
                whatMenu = 1;
                SettingsPanel.IsHitTestVisible = true;
                FadeTo(SettingsPanel, 1.0, 0.3);
                FadeTo(MenuBackground, 0.8, 0.3);
                FadeTo(LogoImage3, 0.0, 0.3);
                FadeTo(MenuButton1, 0.0, 0.3);
                FadeTo(MenuButton2, 0.0, 0.3);
                FadeTo(MenuButton3, 0.0, 0.3);
            }
        }
        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.IsHitTestVisible = false;
            FadeTo(SettingsPanel, 0.0, 0.3);
            FadeTo(MenuBackground, 1, 0.3);
            FadeTo(LogoImage3, 0.95, 0.3);
            FadeTo(MenuButton1, 1, 0.3);
            FadeTo(MenuButton2, 1, 0.3);
            FadeTo(MenuButton3, 1, 0.3);
            File.WriteAllText(state.globalSoundFile, state.globalSound.ToString());
            whatMenu = 0;
        }
        private async void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            CreditsOverlay.IsHitTestVisible = true;
            FadeTo(CreditsOverlay, 1.0, 0.5);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            CreditsScrollTransform.Y = 1080;
            DoubleAnimation scrollAnim = new DoubleAnimation
            {
                To = -4000,
                Duration = TimeSpan.FromSeconds(22)
            };
            scrollAnim.Completed += (s, ev) =>
            {
                if (CreditsOverlay.Opacity > 0)
                {
                    CreditsOverlay.IsHitTestVisible = false;
                    FadeTo(CreditsOverlay, 0.0, 0.5);
                }
            };
            CreditsScrollTransform.BeginAnimation(TranslateTransform.YProperty, scrollAnim);
        }
        private void CreditsOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CreditsOverlay.IsHitTestVisible = false;
            FadeTo(CreditsOverlay, 0.0, 0.5);
            CreditsScrollTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void MenuButton1_Click(object sender, RoutedEventArgs e)
        {
            if (whatMenu == 0)
            {
                whatMenu = 1;
                UpdatePlayersDisplay();
                ConfigPanel.IsHitTestVisible = true;
                FadeTo(ConfigPanel, 1.0, 0.3);
                FadeTo(MenuBackground, 0.8, 0.3);
                FadeTo(LogoImage3, 0.0, 0.3);
                FadeTo(MenuButton1, 0.0, 0.3);
                FadeTo(MenuButton2, 0.0, 0.3);
                FadeTo(MenuButton3, 0.0, 0.3);
            }
        }
        private void CloseConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigPanel.IsHitTestVisible = false;
            FadeTo(ConfigPanel, 0.0, 0.3);
            FadeTo(MenuBackground, 1, 0.3);
            FadeTo(LogoImage3, 0.95, 0.3);
            FadeTo(MenuButton1, 1, 0.3);
            FadeTo(MenuButton2, 1, 0.3);
            FadeTo(MenuButton3, 1, 0.3);
            whatMenu = 0;
        }
        public MenuScene(Action<SceneType> goToAction, GameState sharedState)
        {
            InitializeComponent();
            goTo = goToAction;
            state = sharedState;
            this.Loaded += MenuScene_Loaded;
            this.Focusable = true;
            this.KeyDown += MenuScene_KeyDown;
            VolumeSlider.Value = state.globalSound;
            VolumeText.Text = $"Głośność: {state.globalSound}%";
            if (File.Exists(state.randomStartFile))
            {
                RandomStartCheckBox.IsChecked = File.ReadAllText(state.randomStartFile).Trim() == "1";
            }
        }
        private void RandomStartCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (state != null)
            {
                File.WriteAllText(state.randomStartFile, RandomStartCheckBox.IsChecked == true ? "1" : "0");
            }
        }
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (state != null)
            {
                state.globalSound = e.NewValue;
                foreach (var kvp in activeSounds)
                {
                    string fileName = kvp.Key;
                    MediaPlayer player = kvp.Value;
                    if (activeSoundBaseVolumes.ContainsKey(fileName))
                    {
                        double baseVol = activeSoundBaseVolumes[fileName];
                        player.Volume = baseVol * (state.globalSound / 100.0);
                    }
                }
                if (VolumeText != null)
                {
                    VolumeText.Text = $"Głośność: {e.NewValue}%";
                }
            }
        }
        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            if (state != null)
            {
                File.WriteAllText(state.globalSoundFile, state.globalSound.ToString());
            }
        }
        private void VolumeSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (state != null)
            {
                File.WriteAllText(state.globalSoundFile, state.globalSound.ToString());
            }
        }
        public async void ShowMenu()
        {
            if (state.gameFinishedOnce == 0) {
                SoundFadeTo("mohaa.mp3", 1.0, 2.0);
                FadeTo(LogoImage2, 0, 1.5);
                FadeTo(LogoImage3, 0.95, 4);
                SlideIn(LogoImage3, MoveImage, 3);
                FadeTo(SpaceText, 0, 0.5);
                FadeTo(TitleBackground, 0, 1.5);
                FadeTo(MenuBackground, 1, 1.5);
                FadeTo(VersionText, 0.8, 1.5);
                await Task.Delay(TimeSpan.FromSeconds(1.5));
                FadeTo(MenuButton2, 1, 1);
                SlideIn(MenuButton2, MoveButton2, 1);
                MenuButton2.IsEnabled = true;
                await Task.Delay(TimeSpan.FromSeconds(1));
                FadeTo(MenuButton1, 1, 1);
                SlideIn(MenuButton1, MoveButton1, 1);
                MenuButton1.IsEnabled = true;
                await Task.Delay(TimeSpan.FromSeconds(1));
                FadeTo(MenuButton3, 1, 1);
                SlideIn(MenuButton3, MoveButton3, 1);
                MenuButton3.IsEnabled = true;
                await Task.Delay(TimeSpan.FromSeconds(1));
                PlaySound("plane.mp3", 1);
                await Task.Delay(TimeSpan.FromSeconds(0.3));
                MovePlane.X = -1200;
                PlaneImage.Opacity = 1;
                DoubleAnimation planeSlide = new DoubleAnimation
                {
                    From = -1200,
                    To = 1200,
                    Duration = TimeSpan.FromSeconds(2.5)
                };
                MovePlane.BeginAnimation(TranslateTransform.XProperty, planeSlide);
                await Task.Delay(TimeSpan.FromSeconds(3));
                StopSound("plane.mp3");
            } else {
                PlaySound("biahh.mp3", 0.0, true);
                SoundFadeTo("biahh.mp3", 1.0, 2.0);
                FadeTo(LogoImage2, 0, 0.5);
                FadeTo(LogoImage3, 0.95, 1);
                SlideIn(LogoImage3, MoveImage, 0.01);
                FadeTo(SpaceText, 0, 0.5);
                FadeTo(TitleBackground, 0, 1.5);
                FadeTo(MenuBackground, 1, 1.5);
                FadeTo(VersionText, 0.8, 1.5);
                MenuButton1.IsEnabled = true;
                MenuButton2.IsEnabled = true;
                MenuButton3.IsEnabled = true;
                FadeTo(MenuButton2, 1, 1);
                SlideIn(MenuButton2, MoveButton2, 0.01);
                FadeTo(MenuButton1, 1, 1);
                SlideIn(MenuButton1, MoveButton1, 0.01);
                FadeTo(MenuButton3, 1, 1);
                SlideIn(MenuButton3, MoveButton3, 0.01);
            }
        }
        public async void MenuScene_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            await RunIntroSequence();
        }
        public async Task RunIntroSequence()
        {
            if (state.gameFinishedOnce == 0) {
                await Task.Delay(TimeSpan.FromSeconds(3 * dev));
                if (File.ReadAllText(state.everStartedFile) == "0") {
                    FadeTo(ShaderText, 1, 0.2);
                    await Task.Delay(TimeSpan.FromSeconds(4 * dev));
                    FadeTo(ShaderText, 0, 0.2);
                    await Task.Delay(TimeSpan.FromSeconds(2 * dev));
                    FadeTo(ShaderText, 1, 0.2);
                    await Task.Delay(TimeSpan.FromSeconds(2 * dev));
                    FadeTo(ShaderText, 0, 0.2);
                }
                File.WriteAllText(state.everStartedFile, "1");
                await Task.Delay(TimeSpan.FromSeconds(4 * dev));
                FadeTo(EpilepsiaText1, 1, 2);
                FadeTo(EpilepsiaText2, 1, 2);
                await Task.Delay(TimeSpan.FromSeconds(3 * dev));
                FadeTo(EpilepsiaText1, 0, 1);
                FadeTo(EpilepsiaText2, 0, 1);
                await Task.Delay(TimeSpan.FromSeconds(1.8 * dev));
                FadeTo(CopyrightText, 1, 1);
                await Task.Delay(TimeSpan.FromSeconds(2 * dev));
                FadeTo(CopyrightText, 0, 1);
                PlaySound("intro.mp3", 1.0);
                await Task.Delay(TimeSpan.FromSeconds(1.5 * dev));
                ScaleElement(LogoScale, 1.5, 6.5);
                FadeTo(LogoImage, 1, 1.5);
                await Task.Delay(TimeSpan.FromSeconds(3.5 * dev));
                FadeTo(LogoImage, 0, 1.5);
                await Task.Delay(TimeSpan.FromSeconds(4 * dev));
                FadeTo(TitleBackground, 0.25, 8);
                PlaySound("mohaa.mp3", 0.0, true);
                SoundFadeTo("mohaa.mp3", 0.6, 3.0);
                await Task.Delay(TimeSpan.FromSeconds(2 * dev));
                FadeTo(LogoImage2, 0.9, 4);
                titleStarted = true;
                await Task.Delay(TimeSpan.FromSeconds(2 * dev));
                for (int i = 0; i < 60; i++)
                {
                    if (isSpacePressed) break;
                    FadeTo(SpaceText, 1.0, 2.5);
                    await Task.Delay(TimeSpan.FromSeconds(2.5 * dev));
                    if (isSpacePressed) break;
                    FadeTo(SpaceText, 0.1, 2.5);
                    await Task.Delay(TimeSpan.FromSeconds(2.5 * dev));
                }
                if (!isSpacePressed)
                {
                    isSpacePressed = true;
                    ShowMenu();
                }
            } else {
                ShowMenu();
            }
        }
    }
}
