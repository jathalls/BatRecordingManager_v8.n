using System;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for QualityControl.xaml
    /// </summary>
    public partial class QualityControl : UserControl
    {
        public QualityControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event EventHandler SaveClicked;

        [Flags] public enum FUNCTION { UNKNOWN = 0, ECHO = 1, SOCIAL = 2, OTHER = 4 }

        [Flags] public enum MODE { NORMAL = 1, WIDE = 2, NARROW = 4, ODD = 8 }

        public enum QUALITY { UNKNOWN, GOOD, MODERATE, POOR }

        public enum QUANTITY { NONE, ONE, MANY }

        public bool? isChanged
        {
            get { return (_isChanged); }
            set
            {
                if (value != _isChanged)
                {
                    _isChanged = value;
                    SaveButton.IsEnabled = value ?? false;
                }
            }
        }

        public bool? isEcho
        {
            get
            {
                return ((Function & FUNCTION.ECHO) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isEcho ?? false) isChanged = true;
                    Function |= FUNCTION.ECHO;
                }
                else
                {
                    if (isEcho ?? false) isChanged = true;
                    Function &= ~FUNCTION.ECHO;
                }
            }
        }

        public bool? isGood
        {
            get
            {
                return (Quality == QUALITY.GOOD);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isGood ?? false) isChanged = true;
                    Quality = QUALITY.GOOD;
                }
            }
        }

        public bool? isMany
        {
            get
            {
                return (Quantity == QUANTITY.MANY);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isMany ?? false) isChanged = true;
                    Quantity = QUANTITY.MANY;
                }
            }
        }

        public bool? isModerate
        {
            get
            {
                return (Quality == QUALITY.MODERATE);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isModerate ?? false) isChanged = true;
                    Quality = QUALITY.MODERATE;
                }
            }
        }

        public bool? isNarrow
        {
            get
            {
                return ((Mode & MODE.NARROW) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isNarrow ?? false) isChanged = true;
                    Mode |= MODE.NARROW;
                }
                else
                {
                    if (isNarrow ?? false) isChanged = true;
                    Mode &= ~MODE.NARROW;
                }
            }
        }

        public bool? isNone
        {
            get
            {
                return (Quantity == QUANTITY.NONE);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isNone ?? false) isChanged = true;
                    Quantity = QUANTITY.NONE;
                }
            }
        }

        public bool? isNormal
        {
            get
            {
                return ((Mode & MODE.NORMAL) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isNormal ?? false) isChanged = true;
                    Mode &= ~MODE.NORMAL;
                }
            }
        }

        public bool? isOdd
        {
            get
            {
                return ((Mode & MODE.ODD) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isOdd ?? false) isChanged = true;
                    Mode |= MODE.ODD;
                }
                else
                {
                    if (isOdd ?? false) isChanged = true;
                    Mode &= ~MODE.ODD;
                }
            }
        }

        public bool? isOne
        {
            get
            {
                return (Quantity == QUANTITY.ONE);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isOne ?? false) isChanged = true;
                    Quantity = QUANTITY.ONE;
                }
            }
        }

        public bool? isOther
        {
            get
            {
                return ((Function & FUNCTION.OTHER) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isOther ?? false) isChanged = true;
                    Function |= FUNCTION.OTHER;
                }
                else
                {
                    if (isOther ?? false) isChanged = true;
                    Function &= ~FUNCTION.OTHER;
                }
            }
        }

        public bool? isPoor
        {
            get
            {
                return (Quality == QUALITY.POOR);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isPoor ?? false) isChanged = true;
                    Quality = QUALITY.POOR;
                }
            }
        }

        public bool? isSocial
        {
            get
            {
                return ((Function & FUNCTION.SOCIAL) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isSocial ?? false) isChanged = true;
                    Function |= FUNCTION.SOCIAL;
                }
                else
                {
                    if (isSocial ?? false) isChanged = true;
                    Function &= ~FUNCTION.SOCIAL;
                }
            }
        }

        public bool? isWide
        {
            get
            {
                return ((Mode & MODE.WIDE) != 0);
            }
            set
            {
                if (value ?? false)
                {
                    if (!isWide ?? false) isChanged = true;
                    Mode |= MODE.WIDE;
                }
                else
                {
                    if (isWide ?? false) isChanged = true;
                    Mode &= ~MODE.WIDE;
                }
            }
        }

        /// <summary>
        /// Returns a string incorporating the quality parameters
        /// </summary>
        /// <returns></returns>
        public string AsString()
        {
            string result = "";

            result += $"[Function] {Function.ToString()}\n";
            result += $"[Mode] {Mode.ToString()}\n";
            result += $"[Quality] {Quality.ToString()}\n";
            result += $"[Quantity] {Quantity.ToString()}\n";

            return (result);
        }

        /// <summary>
        /// Parses a multi-line string to extract the quality parameters encoded in the format
        /// [param] setting
        /// placed one on each line for Function, Mode, Quantity and Quality in any order
        /// </summary>
        /// <param name="text"></param>
        public void SetFromString(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var lines = text.Trim().Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("[")) parseLine(line);
            }
        }

        protected virtual void OnSaveClicked(EventArgs e) => SaveClicked?.Invoke(this, e);

        private bool? _isChanged = false;

        private FUNCTION Function = FUNCTION.ECHO;

        private MODE Mode = MODE.NORMAL;

        private QUALITY Quality = QUALITY.MODERATE;

        private QUANTITY Quantity = QUANTITY.ONE;

        private void parseLine(string line)
        {
            var parts = line.Trim().Split(']');
            if (parts.Length > 1)
            {
                if (line.StartsWith("[Function]"))
                {
                    if (line.Contains(FUNCTION.ECHO.ToString()))
                    {
                        isEcho = true;
                    }
                    else isEcho = false;
                    if (line.Contains(FUNCTION.OTHER.ToString())) isOther = true;
                    else isOther = false;

                    if (line.Contains(FUNCTION.SOCIAL.ToString())) isSocial = true;
                    else isSocial = false;
                }
                else if (line.StartsWith("[Mode"))
                {
                    if (line.Contains(MODE.NARROW.ToString())) isNarrow = true;
                    else isNarrow = false;

                    isWide = (line.Contains(MODE.WIDE.ToString()));

                    isOdd = (line.Contains(MODE.ODD.ToString()));

                    isNormal = (line.Contains(MODE.NORMAL.ToString()));
                }
                else if (line.StartsWith("[Quality]"))
                {
                    isGood = (line.Contains(QUALITY.GOOD.ToString()));
                    isModerate = (line.Contains(QUALITY.MODERATE.ToString()));
                    isPoor = (line.Contains(QUALITY.POOR.ToString()));
                }
                else if (line.StartsWith("[Quantity]"))
                {
                    isOne = (line.Contains(QUANTITY.ONE.ToString()));
                    isMany = (line.Contains(QUANTITY.MANY.ToString()));
                    isNone = (line.Contains(QUANTITY.NONE.ToString()));
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            OnSaveClicked(EventArgs.Empty);
            isChanged = false;
        }
    }
}