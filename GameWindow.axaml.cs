using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VoidTerminal
{
    public partial class GameWindow : Window
    {
        private const int GridSize = 20;
        private const int CellSize = 20;
        private readonly List<Point> snake = new();
        private Point food;
        private Point direction = new(1, 0);
        private readonly DispatcherTimer gameTimer;
        private readonly DispatcherTimer timeTimer;
        private bool gameOver = false;
        private int score = 0;
        private DateTime gameStartTime;

        public GameWindow()
        {
            InitializeComponent();
            
            snake.Add(new Point(5, 5));
            GenerateFood();

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timeTimer.Tick += UpdateTime;
            timeTimer.Start();

            gameStartTime = DateTime.Now;

            KeyDown += OnKeyDown;
            
            DrawGame();

            Activated += (s, e) => 
            {
                Focus();
                GameCanvas.Focus();
            };
            
            this.AttachedToVisualTree += (s, e) =>
            {
                Focus();
                GameCanvas.Focus();
            };
            
            Focus();
            GameCanvas.Focus();
        }

        private void GenerateFood()
        {
            var random = new Random();
            do
            {
                food = new Point(
                    random.Next(0, GridSize),
                    random.Next(0, GridSize)
                );
            } while (snake.Contains(food));
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (gameOver) return;

            var head = snake[0];
            var newHead = new Point(
                (head.X + direction.X + GridSize) % GridSize,
                (head.Y + direction.Y + GridSize) % GridSize
            );

            if (snake.Contains(newHead))
            {
                GameOver();
                return;
            }

            snake.Insert(0, newHead);

            if (newHead == food)
            {
                score += 10;
                GenerateFood();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            DrawGame();
        }

        private void UpdateTime(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - gameStartTime;
            TimeText.Text = $"TIME: {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();

            foreach (var segment in snake)
            {
                var rect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = CellSize - 1,
                    Height = CellSize - 1,
                    Fill = Brushes.Green
                };
                Canvas.SetLeft(rect, segment.X * CellSize);
                Canvas.SetTop(rect, segment.Y * CellSize);
                GameCanvas.Children.Add(rect);
            }

            var foodRect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = CellSize - 1,
                Height = CellSize - 1,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(foodRect, food.X * CellSize);
            Canvas.SetTop(foodRect, food.Y * CellSize);
            GameCanvas.Children.Add(foodRect);

            ScoreText.Text = $"SCORE: {score}";
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (gameOver)
            {
                return;
            }

            var newDirection = e.Key switch
            {
                Key.Up when direction.Y != 1 => new Point(0, -1),
                Key.Down when direction.Y != -1 => new Point(0, 1),
                Key.Left when direction.X != 1 => new Point(-1, 0),
                Key.Right when direction.X != -1 => new Point(1, 0),
                _ => direction
            };

            direction = newDirection;
            e.Handled = true;
            
            GameCanvas.Focus();
        }

        private void GameOver()
        {
            gameOver = true;
            gameTimer.Stop();
            timeTimer.Stop();
            
            var gameOverText = new TextBlock
            {
                Text = "GAME OVER",
                Foreground = Brushes.Red,
                FontSize = 40,
                FontWeight = FontWeight.Bold,
                FontFamily = new FontFamily("Courier New"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var scoreText = new TextBlock
            {
                Text = $"FINAL SCORE: {score}",
                Foreground = Brushes.White,
                FontSize = 24,
                FontFamily = new FontFamily("Courier New"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var timeText = new TextBlock
            {
                Text = $"TOTAL TIME: {(DateTime.Now - gameStartTime).Minutes:D2}:{(DateTime.Now - gameStartTime).Seconds:D2}",
                Foreground = Brushes.White,
                FontSize = 24,
                FontFamily = new FontFamily("Courier New"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var escText = new TextBlock
            {
                Text = "PRESS ESC TO EXIT",
                Foreground = Brushes.White,
                FontSize = 20,
                FontFamily = new FontFamily("Courier New"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var gameOverPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20
            };

            gameOverPanel.Children.Add(gameOverText);
            gameOverPanel.Children.Add(scoreText);
            gameOverPanel.Children.Add(timeText);
            gameOverPanel.Children.Add(escText);

            Canvas.SetLeft(gameOverPanel, (GameCanvas.Width - 300) / 2);
            Canvas.SetTop(gameOverPanel, (GameCanvas.Height - 200) / 2);
            GameCanvas.Children.Add(gameOverPanel);
        }

        protected override void OnClosed(EventArgs e)
        {
            gameTimer.Stop();
            timeTimer.Stop();
            base.OnClosed(e);
        }
    }
} 