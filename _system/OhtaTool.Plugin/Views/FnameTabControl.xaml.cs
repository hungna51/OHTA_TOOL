using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.ViewModels;

namespace OhtaTool.Plugin.Views
{
    public partial class FnameTabControl : UserControl, IOhtaScrollableTabView
    {
        private readonly FnameTabViewModel _viewModel;
        private bool _userTypedM;
        private bool _userTypedT;
        private bool _isFilteringM;
        private bool _isFilteringT;

        public FnameTabControl(FnameTabViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            MouseEnter += (s, e) => _viewModel?.SyncActiveDrawing();
            GotFocus += (s, e) => _viewModel?.SyncActiveDrawing();
            Loaded += (s, e) => _viewModel?.SyncActiveDrawing();
            IsVisibleChanged += (s, e) => { if (IsVisible) _viewModel?.SyncActiveDrawing(); };

            SetupSearchableComboBoxes();
        }

        private void SetupSearchableComboBoxes()
        {
            if (CmbValM != null)
            {
                CmbValM.PreviewTextInput += (s, e) => _userTypedM = true;
                CmbValM.PreviewKeyDown += OnCmbMPreviewKeyDown;
                CmbValM.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnCmbMTextChanged));
                CmbValM.DropDownOpened += OnCmbMDropDownOpened;
                CmbValM.DropDownClosed += (s, e) =>
                {
                    if (!_isFilteringM && CmbValM.SelectedItem is string pickedM && _viewModel != null)
                        _viewModel.ValM = pickedM;
                };
            }

            if (CmbValT != null)
            {
                CmbValT.PreviewTextInput += (s, e) => _userTypedT = true;
                CmbValT.PreviewKeyDown += OnCmbTPreviewKeyDown;
                CmbValT.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnCmbTTextChanged));
                CmbValT.DropDownOpened += OnCmbTDropDownOpened;
                CmbValT.DropDownClosed += (s, e) =>
                {
                    if (!_isFilteringT && CmbValT.SelectedItem is string pickedT && _viewModel != null)
                        _viewModel.ValT = pickedT;
                };
            }
        }

        private void OnCmbMPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Space ||
                (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                _userTypedM = true;
            }
            else if (e.Key == Key.Down && !CmbValM.IsDropDownOpen)
            {
                CmbValM.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void OnCmbTPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Space ||
                (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                _userTypedT = true;
            }
            else if (e.Key == Key.Down && !CmbValT.IsDropDownOpen)
            {
                CmbValT.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void OnCmbMTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringM || !_userTypedM || _viewModel == null)
                return;

            _userTypedM = false;
            var tb = e.OriginalSource as TextBox;
            string typed = tb != null ? tb.Text : (CmbValM.Text ?? string.Empty);
            int caret = tb != null ? tb.CaretIndex : typed.Length;

            _isFilteringM = true;
            _viewModel.SuppressInputSync = true;
            try
            {
                CmbValM.SelectedIndex = -1;
                _viewModel.FilterMaterialOptions(typed);
                CmbValM.Text = typed;
                if (tb != null && tb.Text != typed)
                    tb.Text = typed;
            }
            finally
            {
                _viewModel.SuppressInputSync = false;
            }

            _viewModel.ValM = typed;

            bool hasCandidates = _viewModel.MaterialOptions.Count > 0;
            if (CmbValM.IsDropDownOpen != hasCandidates)
                CmbValM.IsDropDownOpen = hasCandidates;

            if (tb != null)
            {
                tb.SelectionStart = Math.Min(caret, tb.Text.Length);
                tb.SelectionLength = 0;
            }
            _isFilteringM = false;
        }

        private void OnCmbTTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringT || !_userTypedT || _viewModel == null)
                return;

            _userTypedT = false;
            var tb = e.OriginalSource as TextBox;
            string typed = tb != null ? tb.Text : (CmbValT.Text ?? string.Empty);
            int caret = tb != null ? tb.CaretIndex : typed.Length;

            _isFilteringT = true;
            _viewModel.SuppressInputSync = true;
            try
            {
                CmbValT.SelectedIndex = -1;
                _viewModel.FilterThicknessOptions(typed);
                CmbValT.Text = typed;
                if (tb != null && tb.Text != typed)
                    tb.Text = typed;
            }
            finally
            {
                _viewModel.SuppressInputSync = false;
            }

            _viewModel.ValT = typed;

            bool hasCandidates = _viewModel.ThicknessOptions.Count > 0;
            if (CmbValT.IsDropDownOpen != hasCandidates)
                CmbValT.IsDropDownOpen = hasCandidates;

            if (tb != null)
            {
                tb.SelectionStart = Math.Min(caret, tb.Text.Length);
                tb.SelectionLength = 0;
            }
            _isFilteringT = false;
        }

        private void OnCmbMDropDownOpened(object sender, EventArgs e)
        {
            if (_isFilteringM || _viewModel == null)
                return;

            string currentM = _viewModel.ValM ?? string.Empty;
            var tb = CmbValM.Template?.FindName("PART_EditableTextBox", CmbValM) as TextBox;
            int caret = tb != null ? tb.CaretIndex : currentM.Length;

            _isFilteringM = true;
            _viewModel.SuppressInputSync = true;
            try
            {
                _viewModel.RestoreAllMaterialOptions();
                CmbValM.Text = currentM;
                if (tb != null && tb.Text != currentM)
                    tb.Text = currentM;
            }
            finally
            {
                _viewModel.SuppressInputSync = false;
                _isFilteringM = false;
            }

            if (tb != null)
            {
                tb.SelectionStart = Math.Min(caret, tb.Text.Length);
                tb.SelectionLength = 0;
            }
        }

        private void OnCmbTDropDownOpened(object sender, EventArgs e)
        {
            if (_isFilteringT || _viewModel == null)
                return;

            string currentT = _viewModel.ValT ?? string.Empty;
            var tb = CmbValT.Template?.FindName("PART_EditableTextBox", CmbValT) as TextBox;
            int caret = tb != null ? tb.CaretIndex : currentT.Length;

            _isFilteringT = true;
            _viewModel.SuppressInputSync = true;
            try
            {
                _viewModel.RestoreAllThicknessOptions();
                CmbValT.Text = currentT;
                if (tb != null && tb.Text != currentT)
                    tb.Text = currentT;
            }
            finally
            {
                _viewModel.SuppressInputSync = false;
                _isFilteringT = false;
            }

            if (tb != null)
            {
                tb.SelectionStart = Math.Min(caret, tb.Text.Length);
                tb.SelectionLength = 0;
            }
        }

        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (MainScroller == null || wheelDelta == 0)
                return;

            double step = (wheelDelta / 120.0) * 48.0;
            MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset - step);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var dep = e.OriginalSource as DependencyObject;
                bool isInputField = false;
                ComboBox parentCombo = null;
                while (dep != null)
                {
                    if (dep is ComboBox cb)
                    {
                        isInputField = true;
                        parentCombo = cb;
                        break;
                    }
                    if (dep is TextBox)
                    {
                        isInputField = true;
                    }
                    if (dep is Button)
                    {
                        // Nếu đang focus trên Button thì không can thiệp để phím Enter kích hoạt nút
                        return;
                    }
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (isInputField)
                {
                    if (parentCombo == CmbValM && _viewModel != null)
                    {
                        if (CmbValM.SelectedItem is string selM)
                        {
                            _viewModel.ValM = selM;
                            CmbValM.Text = selM;
                        }
                        else if (_viewModel.MaterialOptions.Count == 1)
                        {
                            string onlyM = _viewModel.MaterialOptions[0];
                            _viewModel.ValM = onlyM;
                            CmbValM.Text = onlyM;
                        }
                        else if (_viewModel.Catalog?.FindMaterial(_viewModel.ValM) is FnameMaterialSpec exactSpec)
                        {
                            _viewModel.ValM = exactSpec.M;
                            CmbValM.Text = exactSpec.M;
                        }
                        CmbValM.IsDropDownOpen = false;
                    }
                    else if (parentCombo == CmbValT && _viewModel != null)
                    {
                        if (CmbValT.SelectedItem is string selT)
                        {
                            _viewModel.ValT = selT;
                            CmbValT.Text = selT;
                        }
                        else if (_viewModel.ThicknessOptions.Count == 1)
                        {
                            string onlyT = _viewModel.ThicknessOptions[0];
                            _viewModel.ValT = onlyT;
                            CmbValT.Text = onlyT;
                        }
                        else if (_viewModel.Catalog?.FindMaterial(_viewModel.ValM) is FnameMaterialSpec spec)
                        {
                            string canonT = FnameCatalogData.FindCanonicalThickness(spec.Thicknesses, _viewModel.ValT);
                            if (canonT != null)
                            {
                                _viewModel.ValT = canonT;
                                CmbValT.Text = canonT;
                            }
                        }
                        CmbValT.IsDropDownOpen = false;
                    }

                    e.Handled = true;
                    var focused = Keyboard.FocusedElement as UIElement ?? e.OriginalSource as UIElement;
                    if (focused != null)
                    {
                        focused.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        if (Keyboard.FocusedElement is TextBox nextTb)
                        {
                            nextTb.SelectAll();
                        }
                    }
                }
            }
        }
    }
}
