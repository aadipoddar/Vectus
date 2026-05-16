namespace VectusLibrary.Operations.Models;

public enum CodeType
{
	FinancialAccounting,
	Ledger,

	Driver,

	VehicleType,
	VehicleDocumentType
}

public class DecodeTransactionNoModel
{
	public object TransactionModel { get; set; }
	public CodeType CodeType { get; set; }
	public string PageRouteName { get; set; }
	public (MemoryStream stream, string fileName) PDFStream { get; set; }
	public (MemoryStream stream, string fileName) ExcelStream { get; set; }
}