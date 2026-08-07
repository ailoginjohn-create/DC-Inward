using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;

namespace InwardDC.App.ViewModels;

public partial class SearchViewModel : ViewModelBase
{
    private readonly ISearchService _search;

    public SearchViewModel(ICurrentUserService currentUser, ISearchService search) : base(currentUser)
    {
        _search = search;
        Title = "Search";
    }

    public ObservableCollection<SearchHitDto> Customers { get; } = new();
    public ObservableCollection<SearchHitDto> Items { get; } = new();
    public ObservableCollection<SearchHitDto> Inwards { get; } = new();
    public ObservableCollection<SearchHitDto> Dispatches { get; } = new();
    public ObservableCollection<ItemStockDto> SerialLookup { get; } = new();
    public ObservableCollection<ItemHistoryDto> History { get; } = new();

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string _serialNo = string.Empty;
    [ObservableProperty] private int _totalHits;
    [ObservableProperty] private bool _hasResults;

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            HasError = true;
            ErrorMessage = "Enter a search term.";
            return;
        }

        await RunAsync(async () =>
        {
            var filter = new GlobalSearchFilter { Query = Query.Trim(), PageSize = 100 };
            var result = await _search.GlobalSearchAsync(filter);

            Fill(Customers, result.Customers);
            Fill(Items, result.Items);
            Fill(Inwards, result.Inwards);
            Fill(Dispatches, result.Dispatches);

            TotalHits = result.Total;
            HasResults = true;
            SetSuccess($"{TotalHits} hit(s) for \"{Query.Trim()}\".");
        }, "Searching...");
    }

    [RelayCommand]
    private async Task SerialLookupAsync()
    {
        if (string.IsNullOrWhiteSpace(SerialNo))
        {
            HasError = true;
            ErrorMessage = "Enter a serial number.";
            return;
        }

        await RunAsync(async () =>
        {
            var rows = await _search.GetSerialLookupAsync(SerialNo.Trim());
            SerialLookup.Clear();
            foreach (var row in rows)
                SerialLookup.Add(row);

            var history = await _search.GetSerialHistoryAsync(SerialNo.Trim());
            History.Clear();
            foreach (var row in history)
                History.Add(row);

            SetSuccess($"{rows.Count} stock row(s) for serial {SerialNo.Trim()}.");
        }, "Looking up serial...");
    }

    private static void Fill<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }
}
