using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;

namespace InwardDC.App.ViewModels;

public partial class SerialOption : ObservableObject
{
    public SerialOption(string serialNo)
    {
        SerialNo = serialNo;
    }

    public string SerialNo { get; }

    [ObservableProperty]
    private bool _isSelected;
}

public partial class DispatchLineRow : ObservableObject
{
    public Guid SourceInwardItemId { get; set; }
    public Guid? ItemId { get; set; }
    public string InwardNo { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemMake { get; set; } = string.Empty;
    public string ItemModel { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount => Quantity * Rate;
    public List<string> Serials { get; set; } = new();
    public string SerialDisplay => string.Join(", ", Serials);
}

public partial class DispatchEditorViewModel : ViewModelBase
{
    private readonly IDispatchService _dispatch;
    private readonly ICustomerService _customers;
    private readonly IPurposeService _purposes;

    private bool _isSerialTracked;

    public DispatchEditorViewModel(ICurrentUserService currentUser, IDispatchService dispatch,
        ICustomerService customers, IPurposeService purposes) : base(currentUser)
    {
        _dispatch = dispatch;
        _customers = customers;
        _purposes = purposes;
        Title = "Dispatch Challan";
    }

    public event Action<bool>? RequestClose;

    public ObservableCollection<DropdownItemDto> Customers { get; } = new();
    public ObservableCollection<DropdownItemDto> Purposes { get; } = new();
    public ObservableCollection<AvailableStockDto> Stock { get; } = new();
    public ObservableCollection<SerialOption> SerialOptions { get; } = new();
    public ObservableCollection<DispatchLineRow> Lines { get; } = new();

    [ObservableProperty] private string _dcNo = string.Empty;
    [ObservableProperty] private DateTime _dcDate = DateTime.Today;
    [ObservableProperty] private DropdownItemDto? _customer;
    [ObservableProperty] private DropdownItemDto? _purpose;
    [ObservableProperty] private string _referenceChallanNo = string.Empty;
    [ObservableProperty] private string _transportDetails = string.Empty;
    [ObservableProperty] private string _remarks = string.Empty;
    [ObservableProperty] private AvailableStockDto? _selectedStock;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private string _stockSearch = string.Empty;
    [ObservableProperty] private decimal _totalAmount;

    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            Customers.Clear();
            foreach (var c in await _customers.GetDropdownAsync())
                Customers.Add(c);

            Purposes.Clear();
            foreach (var p in await _purposes.GetDropdownAsync())
                Purposes.Add(p);

            DcNo = await _dispatch.PreviewNextNumberAsync();
            await RefreshStockAsync();
        }, "Loading...");
    }

    partial void OnSelectedStockChanged(AvailableStockDto? value)
    {
        SerialOptions.Clear();
        _isSerialTracked = false;

        if (value is null)
            return;

        Quantity = value.AvailableQuantity;
        _isSerialTracked = value.AvailableSerials.Count > 0;

        if (_isSerialTracked)
        {
            foreach (var serial in value.AvailableSerials)
                SerialOptions.Add(new SerialOption(serial));
        }
    }

    [RelayCommand]
    private async Task RefreshStockAsync()
    {
        await RunAsync(async () =>
        {
            Stock.Clear();
            var stock = await _dispatch.GetAvailableStockAsync(search: string.IsNullOrWhiteSpace(StockSearch) ? null : StockSearch);
            foreach (var s in stock)
                Stock.Add(s);
        }, "Loading available stock...");
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedStock is null)
        {
            ShowError(new Domain.Exceptions.ValidationException("Select an item from the available stock."));
            return;
        }

        if (Quantity <= 0 || Quantity > SelectedStock.AvailableQuantity)
        {
            ShowError(new Domain.Exceptions.BusinessRuleException(
                $"Quantity must be between 1 and {SelectedStock.AvailableQuantity:0.###}."));
            return;
        }

        var selectedSerials = SerialOptions.Where(s => s.IsSelected).Select(s => s.SerialNo).ToList();

        if (_isSerialTracked)
        {
            if (selectedSerials.Count != (int)Quantity)
            {
                ShowError(new Domain.Exceptions.ValidationException(
                    $"Select exactly {Quantity:0.###} serial(s) for {SelectedStock.ItemName}."));
                return;
            }
        }

        Lines.Add(new DispatchLineRow
        {
            SourceInwardItemId = SelectedStock.InwardItemId,
            ItemId = SelectedStock.ItemId,
            InwardNo = SelectedStock.InwardNo,
            ItemName = SelectedStock.ItemName,
            ItemMake = SelectedStock.Make,
            ItemModel = SelectedStock.Model,
            Unit = SelectedStock.Unit,
            Quantity = Quantity,
            Rate = SelectedStock.Rate,
            Serials = selectedSerials
        });

        TotalAmount = Lines.Sum(l => l.Amount);

        SelectedStock = null;
        SerialOptions.Clear();
        Quantity = 1;
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void RemoveLine(DispatchLineRow? row)
    {
        if (row is null || !Lines.Contains(row))
            return;

        Lines.Remove(row);
        TotalAmount = Lines.Sum(l => l.Amount);
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    public bool CanSave => Lines.Count > 0;

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var request = new DispatchSaveRequest
            {
                DcDate = DcDate,
                CustomerId = Customer?.Id ?? Guid.Empty,
                PurposeId = Purpose?.Id,
                ReferenceChallanNo = ReferenceChallanNo,
                TransportDetails = TransportDetails,
                Remarks = Remarks
            };

            foreach (var line in Lines)
            {
                request.Items.Add(new DispatchLineRequest
                {
                    SourceInwardItemId = line.SourceInwardItemId,
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    ItemMake = line.ItemMake,
                    ItemModel = line.ItemModel,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Amount = line.Amount,
                    Remarks = string.Empty,
                    Serials = line.Serials
                });
            }

            var result = await _dispatch.SaveAsync(request);
            if (result.Success)
            {
                SetSuccess(result.Message);
                RequestClose?.Invoke(true);
            }
            else
            {
                ShowError(new Domain.Exceptions.DomainException(result.Message));
            }
        }, "Generating dispatch challan...");
    }
}
