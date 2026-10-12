using System;
using System.Globalization;

namespace Assignment;

public static class InputHelper
{
    public static double ReadDouble(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();
            if (TryParseFlexibleDouble(input, out double result) && result >= 0)
            {
                return result;
            }
            Console.WriteLine("Invalid input. Enter a non-negative number (e.g., 0.1, 1/10, or 1:10).");
        }
    }

    public static double ReadPositiveDouble(string prompt)
    {
        while (true)
        {
            double value = ReadDouble(prompt);
            if (value > 0)
            {
                return value;
            }

            Console.WriteLine("Value must be greater than 0.");
        }
    }

    public static bool TryParseFlexibleDouble(string? input, out double result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
            double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return double.IsFinite(result);
        }

        char separator = input.Contains('/') ? '/' : input.Contains(':') ? ':' : '\0';
        if (separator == '\0')
        {
            return false;
        }

        string[] parts = input.Split(separator, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!(double.TryParse(parts[0], NumberStyles.Float, CultureInfo.CurrentCulture, out double numerator) ||
              double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out numerator)))
        {
            return false;
        }

        if (!(double.TryParse(parts[1], NumberStyles.Float, CultureInfo.CurrentCulture, out double denominator) ||
              double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out denominator)))
        {
            return false;
        }

        if (!double.IsFinite(numerator) || !double.IsFinite(denominator) || denominator == 0)
        {
            return false;
        }

        result = numerator / denominator;
        return double.IsFinite(result);
    }

    public static int? ReadOptionalInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            if (int.TryParse(input, out int result) && result >= 0)
            {
                return result;
            }
            Console.WriteLine("Invalid input. Please enter a valid non-negative integer or leave blank.");
        }
    }
}
