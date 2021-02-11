﻿#region (c) 2019 Gilles Macabies All right reserved

// Author     : Gilles Macabies
// Solution   : WpfCodeProject
// Projet     : WpfCodeProject
// File       : StringFormatConverter.cs
// Created    : 26/01/2021
//

#endregion (c) 2019 Gilles Macabies All right reserved

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

// ReSharper disable CheckNamespace

namespace FilterDataGrid
{
    public class StringFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // values[0] contains format
                if (values[0] == DependencyProperty.UnsetValue || string.IsNullOrEmpty(values[0]?.ToString())) return "";

                var stringFormat = values[0].ToString();

                switch (values.Length)
                {
                    case 2:
                        return string.Format(stringFormat, values[1]);

                    case 3:
                        return string.Format(stringFormat, values[1], values[2]);

                    default:
                        return "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StringFormatConverter error: {ex.Message}");
                throw;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}