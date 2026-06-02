using System.Globalization;
using System.Text.RegularExpressions;

namespace VectusLibrary.Common;

public static class Helper
{
	public static string RemoveSpace(this string str) =>
		str.Replace(" ", "");

	public static string FormatIndianCurrency(this decimal rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

	public static string FormatIndianCurrency(this decimal? rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C}", rate ?? 0);

	public static string FormatIndianCurrency(this int rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

	public static string FormatIndianCurrencyShort(this decimal rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate);

	public static string FormatIndianCurrencyShort(this decimal? rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate ?? 0);

	public static string FormatIndianCurrencyShort(this int rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate);

	public static string FormatHours(this int hours) =>
		hours >= 1 ? $"{hours}h" : $"{hours * 60}m";

	public static string FormatDistanceKm(this decimal? km) =>
		km is null ? "—" : km.Value < 1m ? $"{km.Value * 1000m:N0} m" : $"{km.Value:N1} km";

	public static string FormatDecimalWithTwoDigits(this decimal value) =>
		value.ToString("0.00", CultureInfo.InvariantCulture);

	public static string FormatMonthlyTrend(decimal current, decimal previous)
	{
		if (previous == 0)
			return "vs last month";

		// Divide by the magnitude of the previous value so the ▲/▼ direction stays
		// correct even when the previous value is negative (e.g. a prior-month loss).
		var change = (double)((current - previous) / Math.Abs(previous)) * 100;
		return change switch
		{
			> 0 => $"▲ {change:0}% vs last month",
			< 0 => $"▼ {Math.Abs(change):0}% vs last month",
			_ => "same as last month"
		};
	}

	/// <summary>
	/// Formats decimal smartly: shows integer if no decimal part (2.0 -> "2"), 
	/// otherwise shows 2 decimal places (2.05 -> "2.05", 2.5666 -> "2.57")
	/// </summary>
	public static string FormatSmartDecimal(this decimal value)
	{
		// Round to 2 decimal places
		decimal rounded = Math.Round(value, 2);

		// Check if the decimal part is zero
		if (rounded == Math.Floor(rounded))
			// No decimal part, show as integer
			return rounded.ToString("0", CultureInfo.InvariantCulture);
		else
			// Has decimal part, show 2 decimal places
			return rounded.ToString("0.##", CultureInfo.InvariantCulture);
	}

	public static bool ValidatePhoneNumber(this string phoneNumber)
	{
		if (string.IsNullOrWhiteSpace(phoneNumber))
			return false;
		if (phoneNumber.Length != 10)
			return false;
		return long.TryParse(phoneNumber, out _);
	}

	public static bool ValidateEmail(this string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return false;
		try
		{
			var addr = new System.Net.Mail.MailAddress(email);
			return addr.Address == email;
		}
		catch { return false; }
	}

	// PAN: 5 letters, 4 digits, 1 letter (e.g. ABCDE1234F).
	public static bool ValidatePAN(this string pan) =>
		!string.IsNullOrWhiteSpace(pan) && Regex.IsMatch(pan.Trim(), "^[A-Za-z]{5}[0-9]{4}[A-Za-z]$");

	// Aadhaar: 12 digits, first digit 2-9 (never starts with 0 or 1).
	public static bool ValidateAadhaar(this string aadhaar) =>
		!string.IsNullOrWhiteSpace(aadhaar) && Regex.IsMatch(aadhaar.Trim().RemoveSpace(), "^[2-9][0-9]{11}$");

	// IFSC: 4 letters, 0, then 6 alphanumerics (e.g. SBIN0001234).
	public static bool ValidateIFSC(this string ifsc) =>
		!string.IsNullOrWhiteSpace(ifsc) && Regex.IsMatch(ifsc.Trim(), "^[A-Za-z]{4}0[A-Za-z0-9]{6}$");
}
