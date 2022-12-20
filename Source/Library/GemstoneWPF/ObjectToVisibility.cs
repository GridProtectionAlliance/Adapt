using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace GemstoneWPF
{
    /// <summary>
    /// Represents an <see cref="IValueConverter"/> class, which takes object value and converts to <see cref="System.Windows.Visibility"/> enumeration.
    /// </summary>
    public class ObjectToVisibility : IValueConverter
    {
        #region [ IValueConverter Members ]

        /// <summary>
        /// Converts object value to <see cref="System.Windows.Visibility"/> enumeration.
        /// </summary>
        /// <param name="value">Object value to be converted.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">true if a true value should indicate visibility.Hidden.</param>
        /// <param name="culture">The culture to use in conversion.</param>
        /// <returns><see cref="System.Windows.Visibility"/> enumeration.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((object)value != null)
            {
                // if value is int, then if it is greater than zero then, set it to true otherwise false.
                // Once we set value to boolean instead of integer, next code will take care of visibility.
                if (value is int)
                {
                    int temp;
                    if (int.TryParse(value.ToString(), out temp))
                    {
                        if (temp > 0)
                            value = true;
                        else
                            value = false;
                    }
                }

                // Handle boolean to visibility conversion here.
                if (value is bool)
                {
                    // if boolean parameter is provided and is true then invert original boolean value.
                    if (parameter != null && parameter is bool)
                    {
                        if ((bool)parameter)
                            value = !(bool)value;
                    }

                    if (!(bool)value)
                        return Visibility.Collapsed;
                }
            }

            return Visibility.Visible;
        }

        /// <summary>
        /// Converts <see cref="System.Windows.Visibility"/> to object.
        /// </summary>
        /// <param name="value"><see cref="System.Windows.Visibility"/> value to be converted.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use in conversion.</param>
        /// <param name="culture">The culture to use in conversion.</param>
        /// <returns>object value.</returns>
        /// <remarks>This method has not been implemented.</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion

    }
}
