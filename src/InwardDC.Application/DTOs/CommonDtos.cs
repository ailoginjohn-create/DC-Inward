using InwardDC.Domain.Criteria;

namespace InwardDC.Application.DTOs;

/// <summary>Standard wrapper for operations that either succeed or produce friendly errors.</summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static OperationResult Ok(string message = "", object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static OperationResult Fail(string message) => new() { Success = false, Message = message };
}

/// <summary>Paged envelope used across all list screens.</summary>
public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages { get; set; }
    public bool HasMore => Page < TotalPages;

    public static PagedResponse<T> From(PagedResult<T> source) => new()
    {
        Items = source.Items,
        TotalCount = source.TotalCount,
        Page = source.Page,
        PageSize = source.PageSize,
        TotalPages = source.TotalPages
    };
}

/// <summary>Lightweight dropdown item shared by all master pickers.</summary>
public record DropdownItemDto(Guid Id, string Code, string Name, string Detail = "");

/// <summary>Outcome of an Excel bulk import (rows accepted + per-row validation errors).</summary>
public class FileImportResult
{
    public bool Success => Errors.Count == 0;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int CreatedEntries { get; set; }
    public IReadOnlyList<ImportRowError> Errors { get; set; } = Array.Empty<ImportRowError>();

    public string Summary =>
        $"Total rows: {TotalRows}, Imported: {ImportedRows}, Duplicates skipped: {DuplicatesSkipped}, Entries created: {CreatedEntries}, Errors: {Errors.Count}";
}

public class ImportRowError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Value { get; set; }
}
