using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WheresMyImplant
{
    internal class CheckCreditCard
    {
        const string ccPattern = @"(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})";
        static Regex ccRegex = new Regex(ccPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static List<String> CheckString(string input)
        {
            List<String> ccNumbers = new List<String>();
            MatchCollection matches = ccRegex.Matches(input);
            foreach (Match match in matches)
            {
                match.ToString();
                if (CheckLuhn(match.Value))
                {
                    ccNumbers.Add(match.Value + "\n");
                }
            }
            return ccNumbers;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // https://en.wikipedia.org/wiki/Luhn_algorithm
        ////////////////////////////////////////////////////////////////////////////////
        private static bool CheckLuhn(string input)
        {
            int sum;
            if (!int.TryParse(input.Substring(input.Length - 1, 1), out sum))
            {
                return false;
            }
            int nDigits = input.Length;
            int parity = nDigits % 2;

            for (int i = 0; i < nDigits - 1; i++)
            {
                int digit;
                if (!int.TryParse(input[i].ToString(), out digit))
                {
                    return false;
                }
                if (parity == i % 2)
                {
                    digit *= 2;
                }
                if (9 < digit)
                {
                    digit -= 9;
                }
                sum += digit;
            }
            return 0 == sum % 10;
        }
    }
}