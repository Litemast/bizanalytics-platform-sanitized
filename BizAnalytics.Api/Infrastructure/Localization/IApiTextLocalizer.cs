namespace BizAnalytics.Api.Infrastructure.Localization;

public interface IApiTextLocalizer
{
    string UserAlreadyExists();
    string RegistrationCompletedSuccessfully();
    string InvalidEmailOrPassword();
    string OrganizationWasNotFoundOrIsNotAccessible();
    string OrganizationNameIsRequired();
    string OrganizationUpdatedSuccessfully();
    string OrganizationDeletedSuccessfully();
    string UnsupportedDataSourceType(string supportedTypes);
    string FileWasNotUploaded();
    string AtLeastOneFileMustBeUploaded();
    string TooManyImportFiles(int maxFileCount);
    string UnsupportedImportFileExtension(string extension);
    string FailedToReadCsvFile();
    string FailedToReadSpreadsheetFile();
    string FailedToReadWordDocument();
    string ImportedFilesDoNotContainDataRows();
    string FileDoesNotContainDataRows(string fileName);
    string TableWithSupportedHeadersWasNotFound(string fileName);
    string ImportCompletedSuccessfully(int fileCount, int recordCount);
    string CsvFileDoesNotContainHeaders();
    string CsvHeadersDoNotMatchExpectedFormat();
    string CsvFieldMustNotBeEmpty(int rowNumber, string fieldName);
    string CsvDateHasInvalidFormat(int rowNumber, string value);
    string CsvQuantityIsNotWholeNumber(int rowNumber, string value);
    string CsvQuantityMustBeGreaterThanZero(int rowNumber);
    string CsvAmountIsNotNumber(int rowNumber, string value);
    string StartDateCannotBeLaterThanEndDate();
    string AnalyticsResetSuccessfully(int deletedRecordCount);
    string ReportPayloadIsMissing();
}
