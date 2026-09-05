// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerLauncher;

namespace Wox.Test
{
    [STATestClass]
    [DoNotParallelize]
    public class LauncherControlTest
    {
        private const string LongQuery = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa a";
        private static PowerLauncher.App application;

        [TestMethod]
        public void LongQueryScrollsAutocompleteWithinClippedViewport()
        {
            using var host = CreateHost(220, FlowDirection.LeftToRight);
            var launcherControl = host.LauncherControl;
            var query = LongQuery;
            launcherControl.AutoCompleteTextBlock.Text = query + " result";
            launcherControl.QueryTextBox.Text = query;
            launcherControl.QueryTextBox.CaretIndex = query.Length;
            UpdateLayout(launcherControl);

            launcherControl.QueryTextBox.AppendText("a");
            launcherControl.QueryTextBox.CaretIndex = launcherControl.QueryTextBox.Text.Length;
            UpdateLayout(launcherControl);

            Assert.AreEqual(query + "a", launcherControl.QueryTextBox.Text);
            Assert.IsTrue(launcherControl.QueryTextBox.HorizontalOffset > 0);
            AssertAutocompleteTracksQuery(launcherControl, FlowDirection.LeftToRight);
            AssertAutocompleteIsClippedToQueryColumn(launcherControl);
        }

        [TestMethod]
        public void ShortQueryKeepsAutocompleteAligned()
        {
            using var host = CreateHost(220, FlowDirection.LeftToRight);
            var launcherControl = host.LauncherControl;
            launcherControl.AutoCompleteTextBlock.Text = "power toys";
            launcherControl.QueryTextBox.Text = "power";
            launcherControl.QueryTextBox.CaretIndex = launcherControl.QueryTextBox.Text.Length;
            UpdateLayout(launcherControl);

            Assert.AreEqual(0.0, launcherControl.QueryTextBox.HorizontalOffset);
            AssertAutocompleteTracksQuery(launcherControl, FlowDirection.LeftToRight);
        }

        [TestMethod]
        public void CaretAndWidthChangesKeepAutocompleteSynchronizedWithoutChangingQuery()
        {
            using var host = CreateHost(280, FlowDirection.LeftToRight);
            var launcherControl = host.LauncherControl;
            var query = LongQuery + LongQuery;
            launcherControl.AutoCompleteTextBlock.Text = query + " result";
            launcherControl.QueryTextBox.Text = query;
            launcherControl.QueryTextBox.CaretIndex = query.Length;
            UpdateLayout(launcherControl);

            var wideOffset = launcherControl.QueryTextBox.HorizontalOffset;
            Assert.IsTrue(wideOffset > 0);
            AssertAutocompleteTracksQuery(launcherControl, FlowDirection.LeftToRight);

            launcherControl.QueryTextBox.CaretIndex = 0;
            UpdateLayout(launcherControl);

            Assert.AreNotEqual(wideOffset, launcherControl.QueryTextBox.HorizontalOffset);
            AssertAutocompleteTracksQuery(launcherControl, FlowDirection.LeftToRight);

            launcherControl.QueryTextBox.CaretIndex = query.Length;
            host.Resize(160);
            UpdateLayout(launcherControl);

            Assert.IsTrue(launcherControl.QueryTextBox.HorizontalOffset > wideOffset);
            AssertAutocompleteTracksQuery(launcherControl, FlowDirection.LeftToRight);
            Assert.AreEqual(query, launcherControl.QueryTextBox.Text);
        }

        [DataTestMethod]
        [DataRow(FlowDirection.LeftToRight, -1)]
        [DataRow(FlowDirection.RightToLeft, 1)]
        public void AutocompleteTranslationFollowsFlowDirection(FlowDirection flowDirection, int translationDirection)
        {
            using var host = CreateHost(220, flowDirection);
            var launcherControl = host.LauncherControl;
            launcherControl.AutoCompleteTextBlock.Text = LongQuery + " result";
            launcherControl.QueryTextBox.Text = LongQuery;
            UpdateLayout(launcherControl);

            var contentHost = (ScrollViewer)launcherControl.QueryTextBox.Template.FindName("PART_ContentHost", launcherControl.QueryTextBox);
            contentHost.ScrollToHorizontalOffset(contentHost.ScrollableWidth / 2);
            UpdateLayout(launcherControl);

            Assert.IsTrue(launcherControl.QueryTextBox.HorizontalOffset > 0);
            var transform = (TranslateTransform)launcherControl.AutoCompleteTextBlock.RenderTransform;
            Assert.AreEqual(translationDirection * launcherControl.QueryTextBox.HorizontalOffset, transform.X, 0.01);
            AssertAutocompleteIsClippedToQueryColumn(launcherControl);
        }

        private static LauncherControlHost CreateHost(double width, FlowDirection flowDirection)
        {
            EnsureApplicationResources();

            var launcherControl = new LauncherControl
            {
                Width = width,
            };
            launcherControl.QueryTextBox.FlowDirection = flowDirection;
            launcherControl.AutoCompleteTextBlock.FlowDirection = flowDirection;

            var window = new Window
            {
                Width = width,
                Height = 80,
                Content = launcherControl,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };
            window.Show();
            launcherControl.QueryTextBox.Focus();
            UpdateLayout(launcherControl);

            return new LauncherControlHost(window, launcherControl);
        }

        private static void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                application = new PowerLauncher.App();
                application.InitializeComponent();
                application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }

        private static void UpdateLayout(FrameworkElement element)
        {
            element.UpdateLayout();
            element.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            element.UpdateLayout();
        }

        private static void AssertAutocompleteTracksQuery(LauncherControl launcherControl, FlowDirection flowDirection)
        {
            var transform = (TranslateTransform)launcherControl.AutoCompleteTextBlock.RenderTransform;
            var translationDirection = flowDirection == FlowDirection.LeftToRight ? -1 : 1;
            Assert.AreEqual(translationDirection * launcherControl.QueryTextBox.HorizontalOffset, transform.X, 0.01);
        }

        private static void AssertAutocompleteIsClippedToQueryColumn(LauncherControl launcherControl)
        {
            var viewport = (Grid)launcherControl.FindName("AutoCompleteViewport");
            Assert.IsTrue(viewport.ClipToBounds);
            Assert.AreEqual(1, Grid.GetColumn(viewport));
            Assert.AreEqual(launcherControl.QueryTextBox.ActualWidth, viewport.ActualWidth, 0.01);
            Assert.AreSame(viewport, VisualTreeHelper.GetParent(launcherControl.AutoCompleteTextBlock));
        }

        private sealed class LauncherControlHost : IDisposable
        {
            private readonly Window _window;

            public LauncherControlHost(Window window, LauncherControl launcherControl)
            {
                _window = window;
                LauncherControl = launcherControl;
            }

            public LauncherControl LauncherControl { get; }

            public void Resize(double width)
            {
                _window.Width = width;
                LauncherControl.Width = width;
            }

            public void Dispose()
            {
                _window.Close();
            }
        }
    }
}
