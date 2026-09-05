// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace PowerLauncher
{
    /// <summary>
    /// Interaction logic for LauncherControl.xaml
    /// </summary>
    public partial class LauncherControl : UserControl
    {
        public LauncherControl()
        {
            InitializeComponent();
        }

        private void QueryTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (AutoCompleteTextTransform == null)
            {
                return;
            }

            var translationDirection = QueryTextBox.FlowDirection == FlowDirection.LeftToRight ? -1 : 1;
            AutoCompleteTextTransform.X = translationDirection * e.HorizontalOffset;
        }
    }
}
