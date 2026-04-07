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


        // kontent



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
            // Opcjonalny reset przezroczystości wszystkich ikon
            SelectIcon1.Opacity = 1.0;
            SelectIcon2.Opacity = 1.0;
            SelectIcon3.Opacity = 1.0;
            SelectIcon4.Opacity = 1.0;
            SelectIcon5.Opacity = 1.0;
            SelectIcon6.Opacity = 1.0;

            // Odczytanie wybranej ikony z pliku (wybieramy plik w zależności od tego, którego gracza edytujemy)
            string selectedIconId = File.ReadAllText(currentEditingPlayer == 1 ? state.p1IconFile : state.p2IconFile).Trim();

            // Odnalezienie kontrolki Image po jej nazwie (np. "SelectIcon1") i zmiana Opacity
            if (FindName("SelectIcon" + selectedIconId) is Image selectedImage)
            {
                selectedImage.Opacity = 0.4;
            }
        }

        private void SelectIcon_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                // Wyciąga sam numer z nazwy kontrolki, np. "SelectIcon4" -> "4"
                string iconId = clickedImage.Name.Replace("SelectIcon", ""); 
                
                if (currentEditingPlayer == 1)
                {
                    File.WriteAllText(state.p1IconFile, iconId);
                }
                else
                {
                    File.WriteAllText(state.p2IconFile, iconId);
                }

                // Odświeżenie wyglądu ikon, aby nowo kliknięta miała Opacity 0.5
                UpdateIconSelectionUI();
            }
        }
        private void SavePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            // Usunięcie spacji z początku i końca wpisanej nazwy
            string newName = PlayerNameInput.Text.Trim();

            // Blokada - jeśli po usunięciu spacji nazwa jest pusta (mniej niż 1 znak) to przerywamy funkcję
            if (string.IsNullOrEmpty(newName))
            {
                return; 
            }

            // Zapis do odpowiedniego pliku
            if (currentEditingPlayer == 1)
            {
                File.WriteAllText(state.p1NameFile, newName);
                // TODO: zapisać wybraną ikonę do state.p1IconFile
            }
            else
            {
                File.WriteAllText(state.p2NameFile, newName);
                // TODO: zapisać wybraną ikonę do state.p2IconFile
            }
            UpdatePlayersDisplay();
            // Powrót do głównego widoku wyboru graczy
            PlayerEditView.Visibility = Visibility.Collapsed;
            ConfigMainView.Visibility = Visibility.Visible;
        }
        public async void MenuButton2_Click(object sender, RoutedEventArgs e)
        {
            if (whatMenu == 0) {
                MenuButton2.IsEnabled = false;
                FadeTo(MenuBackground, 0, 1);
                FadeTo(LogoImage3, 0, 1);
                FadeTo(VersionText, 0, 1);
                FadeTo(MenuButton1, 0, 1);
                FadeTo(MenuButton2, 0, 1);
                FadeTo(MenuButton3, 0, 1);
                await Task.Delay(TimeSpan.FromSeconds(2));
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

        private void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
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
            
            // Ustawienie wartości suwaka ORAZ początkowego tekstu
            VolumeSlider.Value = state.globalSound;
            VolumeText.Text = $"Głośność: {state.globalSound}%";
        }
        
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (state != null)
            {
                state.globalSound = e.NewValue;
                
                
                // Zabezpieczenie przed błędem, gdy kontrolki jeszcze się nie zbudowały
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
            FadeTo(LogoImage2, 0, 1.5);
            FadeTo(LogoImage3, 0.95, 5);
            SlideIn(LogoImage3, MoveImage, 3);
            FadeTo(SpaceText, 0, 0.5);
            FadeTo(TitleBackground, 0, 1.5);
            FadeTo(MenuBackground, 1, 1.5);
            FadeTo(VersionText, 0.8, 1.5);
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            FadeTo(MenuButton2, 1, 1);
            SlideIn(MenuButton2, MoveButton2, 1);
            await Task.Delay(TimeSpan.FromSeconds(1));
            FadeTo(MenuButton1, 1, 1);
            SlideIn(MenuButton1, MoveButton1, 1);
            await Task.Delay(TimeSpan.FromSeconds(1));
            FadeTo(MenuButton3, 1, 1);
            SlideIn(MenuButton3, MoveButton3, 1);
        }
        public async void MenuScene_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            await RunIntroSequence();
        }
        public async Task RunIntroSequence()
        {
            await Task.Delay(TimeSpan.FromSeconds(8));
            if (File.ReadAllText(state.everStartedFile) == "0") {
                FadeTo(ShaderText, 1, 0.2);
                await Task.Delay(TimeSpan.FromSeconds(4));
                FadeTo(ShaderText, 0, 0.2);
                await Task.Delay(TimeSpan.FromSeconds(2));
                FadeTo(ShaderText, 1, 0.2);
                await Task.Delay(TimeSpan.FromSeconds(2));
                FadeTo(ShaderText, 0, 0.2);
            }
            File.WriteAllText(state.everStartedFile, "1");
            await Task.Delay(TimeSpan.FromSeconds(4));
            FadeTo(EpilepsiaText, 1, 2);
            await Task.Delay(TimeSpan.FromSeconds(2));
            FadeTo(EpilepsiaText, 0, 1);
            await Task.Delay(TimeSpan.FromSeconds(1.8));
            FadeTo(CopyrightText, 1, 1);
            await Task.Delay(TimeSpan.FromSeconds(2));
            FadeTo(CopyrightText, 0, 1);
            await Task.Delay(TimeSpan.FromSeconds(1));
            ScaleElement(LogoScale, 1.28, 6.0);
            FadeTo(LogoImage, 1, 1.5);
            await Task.Delay(TimeSpan.FromSeconds(3));
            FadeTo(LogoImage, 0, 1.5);
            await Task.Delay(TimeSpan.FromSeconds(4));
            FadeTo(TitleBackground, 0.28, 8);
            await Task.Delay(TimeSpan.FromSeconds(2));
            FadeTo(LogoImage2, 0.9, 4);
            titleStarted = true;
            await Task.Delay(TimeSpan.FromSeconds(2));
            for (int i = 0; i < 30; i++)
            {
                if (isSpacePressed) break;
                FadeTo(SpaceText, 1.0, 2.5);
                await Task.Delay(TimeSpan.FromSeconds(2.5));
                if (isSpacePressed) break;
                FadeTo(SpaceText, 0.1, 2.5);
                await Task.Delay(TimeSpan.FromSeconds(2.5));
            }
            if (!isSpacePressed)
            {
                isSpacePressed = true;
                ShowMenu();
            }
        }
    }
}
