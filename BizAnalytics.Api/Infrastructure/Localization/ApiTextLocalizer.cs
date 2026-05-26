using Microsoft.AspNetCore.Http;

namespace BizAnalytics.Api.Infrastructure.Localization;

public class ApiTextLocalizer : IApiTextLocalizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiTextLocalizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserAlreadyExists() => Translate(
        "Пользователь с таким email уже существует.",
        "A user with this email already exists.");

    public string RegistrationCompletedSuccessfully() => Translate(
        "Регистрация выполнена успешно.",
        "Registration completed successfully.");

    public string InvalidEmailOrPassword() => Translate(
        "Неверный email или пароль.",
        "Invalid email or password.");

    public string OrganizationWasNotFoundOrIsNotAccessible() => Translate(
        "Организация не найдена или недоступна.",
        "Organization was not found or is not accessible.");

    public string OrganizationNameIsRequired() => Translate(
        "Укажите название компании.",
        "A company name is required.");

    public string OrganizationUpdatedSuccessfully() => Translate(
        "Компания обновлена.",
        "The company was updated successfully.");

    public string OrganizationDeletedSuccessfully() => Translate(
        "Компания удалена.",
        "The company was deleted successfully.");

    public string UnsupportedDataSourceType(string supportedTypes) => Translate(
        $"Поддерживаются только следующие типы источников: {supportedTypes}.",
        $"Only these data source types are supported: {supportedTypes}.");

    public string FileWasNotUploaded() => Translate(
        "Файл не загружен.",
        "File was not uploaded.");

    public string AtLeastOneFileMustBeUploaded() => Translate(
        "Загрузите хотя бы один файл для анализа.",
        "Upload at least one file for analysis.");

    public string TooManyImportFiles(int maxFileCount) => Translate(
        $"Можно загрузить не более {maxFileCount} файлов за один мультианализ.",
        $"You can upload no more than {maxFileCount} files for one multi-analysis.");

    public string UnsupportedImportFileExtension(string extension) => Translate(
        $"Формат {extension} не поддерживается. Используйте CSV, XLS, XLSX или DOCX.",
        $"The {extension} format is not supported. Use CSV, XLS, XLSX, or DOCX.");

    public string FailedToReadCsvFile() => Translate(
        "Не удалось прочитать CSV-файл. Проверьте структуру и содержимое.",
        "Failed to read the CSV file. Check its structure and contents.");

    public string FailedToReadSpreadsheetFile() => Translate(
        "Не удалось прочитать Excel-файл. Проверьте таблицу и формат ячеек.",
        "Failed to read the Excel file. Check the worksheet and cell formats.");

    public string FailedToReadWordDocument() => Translate(
        "Не удалось прочитать Word-документ. Убедитесь, что в нем есть таблица.",
        "Failed to read the Word document. Make sure it contains a table.");

    public string ImportedFilesDoNotContainDataRows() => Translate(
        "Загруженные файлы не содержат строк с данными.",
        "The uploaded files do not contain any data rows.");

    public string FileDoesNotContainDataRows(string fileName) => Translate(
        $"Файл {fileName} не содержит строк с данными.",
        $"The file {fileName} does not contain any data rows.");

    public string TableWithSupportedHeadersWasNotFound(string fileName) => Translate(
        $"В файле {fileName} не найдена таблица с колонками Date, ProductName, Quantity, Amount.",
        $"No table with Date, ProductName, Quantity, Amount columns was found in {fileName}.");

    public string ImportCompletedSuccessfully(int fileCount, int recordCount) => Translate(
        $"Импорт завершен: обработано файлов {fileCount}, записей {recordCount}.",
        $"Import completed: {fileCount} files and {recordCount} records were processed.");

    public string CsvFileDoesNotContainHeaders() => Translate(
        "CSV-файл не содержит заголовков. Ожидаются колонки Date, ProductName, Quantity, Amount.",
        "The CSV file does not contain headers. Expected columns: Date, ProductName, Quantity, Amount.");

    public string CsvHeadersDoNotMatchExpectedFormat() => Translate(
        "Заголовки не соответствуют ожидаемому формату: Date, ProductName, Quantity, Amount.",
        "Headers do not match the expected format: Date, ProductName, Quantity, Amount.");

    public string CsvFieldMustNotBeEmpty(int rowNumber, string fieldName) => Translate(
        $"Строка {rowNumber}: поле {fieldName} не должно быть пустым.",
        $"Row {rowNumber}: the {fieldName} field must not be empty.");

    public string CsvDateHasInvalidFormat(int rowNumber, string value) => Translate(
        $"Строка {rowNumber}: значение '{value}' в колонке Date имеет неверный формат.",
        $"Row {rowNumber}: value '{value}' in the Date column has an invalid format.");

    public string CsvQuantityIsNotWholeNumber(int rowNumber, string value) => Translate(
        $"Строка {rowNumber}: значение '{value}' в колонке Quantity не является целым числом.",
        $"Row {rowNumber}: value '{value}' in the Quantity column is not a whole number.");

    public string CsvQuantityMustBeGreaterThanZero(int rowNumber) => Translate(
        $"Строка {rowNumber}: Quantity должно быть больше нуля.",
        $"Row {rowNumber}: Quantity must be greater than zero.");

    public string CsvAmountIsNotNumber(int rowNumber, string value) => Translate(
        $"Строка {rowNumber}: значение '{value}' в колонке Amount не является числом.",
        $"Row {rowNumber}: value '{value}' in the Amount column is not a number.");

    public string StartDateCannotBeLaterThanEndDate() => Translate(
        "Дата начала не может быть позже даты окончания.",
        "The start date cannot be later than the end date.");

    public string AnalyticsResetSuccessfully(int deletedRecordCount) => Translate(
        $"Аналитика сброшена: удалено записей {deletedRecordCount}.",
        $"Analytics was reset: {deletedRecordCount} records were removed.");

    public string ReportPayloadIsMissing() => Translate(
        "Для формирования PDF не хватает данных аналитики.",
        "Analytics data is required to generate a PDF report.");

    private string Translate(string russian, string english)
    {
        return IsEnglishRequested() ? english : russian;
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }
}
