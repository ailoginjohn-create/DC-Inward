using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Enums;

namespace InwardDC.App.ViewModels;

public partial class InwardLineRow : ObservableObject
{
    [ObservableProperty] private Guid? _itemId;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private string _itemMake = string.Empty;
    [ObservableProperty] private string _itemModel = string.Empty;
    [ObservableProperty] private string _hsnCode = string.Empty;
    [ObservableProperty] private string _unit = "Nos";
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _rate;
    [ObservableProperty] private string _remarks = string.Empty;
    [ObservableProperty] private string _serialsText = string.Empty;
    [ObservableProperty] private bool _isSerialTracked;

    public decimal Amount => Quantity * Rate;

    public List<string> Serials => SerialsText
        .Split(new[] { '\n', ',', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}

public partial class InwardEditorViewModel : ViewModelBase
{
    private readonly IInwardService _inward;
    private readonly ICustomerService _customers;
    private readonly IVendorService _vendors;
    private readonly IItemService _items;
    private readonly IPurposeService _purposes;

    private readonly Dictionary<Guid, ItemDto> _itemLookup = new();

    private Guid? _id;

    public InwardEditorViewModel(ICurrentUserService currentUser, IInwardService inward,
        ICustomerService customers, IVendorService vendors, IItemService items,
        IPurposeService purposes) : base(currentUser)
    {
        _inward = inward;
        _customers = customers;
        _vendors = vendors;
        _items = items;
        _purposes = purposes;
        Title = "Inward Entry";
        Lines = new ObservableCollection<InwardLineRow>();
    }

    public event Action<bool>? RequestClose;

    public ObservableCollection<DropdownItemDto> Customers { get; } = new();
    public ObservableCollection<DropdownItemDto> Vendors { get; } = new();
    public ObservableCollection<DropdownItemDto> Purposes { get; } = new();
    public ObservableCollection<ItemDto> MasterItems { get; } = new();
    public ObservableCollection<InwardLineRow> Lines { get; }

    public IReadOnlyList<InwardType> Types => Enum.GetValues<InwardType>();
    public IReadOnlyList<InwardStatus> Statuses => Enum.GetValues<InwardStatus>();

    [ObservableProperty] private string _inwardNo = string.Empty;
    [ObservableProperty] private DateTime _inwardDate = DateTime.Today;
    [ObservableProperty] private InwardType _inwardType = InwardType.CustomerReturn;
    [ObservableProperty] private DropdownItemDto? _customer;
    [ObservableProperty] private DropdownItemDto? _vendor;
    [ObservableProperty] private DropdownItemDto? _purpose;
    [ObservableProperty] private string _referenceInvoiceNo = string.Empty;
    [ObservableProperty] private DateTime? _referenceInvoiceDate;
    [ObservableProperty] private string _challanNo = string.Empty;
    [ObservableProperty] private string _transportDetails = string.Empty;
    [ObservableProperty] private string _remarks = string.Empty;
    [ObservableProperty] private InwardStatus _status = InwardStatus.Received;
    [ObservableProperty] private bool _isEditMode;

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;
        await RunAsync(async () =>
        {
            await LoadLookupsAsync();

            if (id.HasValue)
            {
                var dto = await _inward.GetByIdAsync(id.Value);
                if (dto is null)
                {
                    ShowError(new Domain.Exceptions.NotFoundException("Inward entry not found."));
                    return;
                }

                InwardNo = dto.InwardNo;
                InwardDate = dto.InwardDate;
                InwardType = dto.InwardType;
                Customer = Customers.FirstOrDefault(c => c.Id == dto.CustomerId);
                Vendor = Vendors.FirstOrDefault(v => v.Id == dto.VendorId);
                Purpose = Purposes.FirstOrDefault(p => p.Id == dto.PurposeId);
                ReferenceInvoiceNo = dto.ReferenceInvoiceNo;
                ReferenceInvoiceDate = dto.ReferenceInvoiceDate;
                ChallanNo = dto.ChallanNo;
                TransportDetails = dto.TransportDetails;
                Remarks = dto.Remarks;
                Status = dto.Status;
                IsEditMode = true;

                Lines.Clear();
                foreach (var line in dto.Items)
                {
                    Lines.Add(new InwardLineRow
                    {
                        ItemId = line.ItemId,
                        ItemName = line.ItemName,
                        ItemMake = line.ItemMake,
                        ItemModel = line.ItemModel,
                        HsnCode = line.HsnCode,
                        Unit = line.Unit,
                        Quantity = line.Quantity,
                        Rate = line.Rate,
                        Remarks = line.Remarks,
                        SerialsText = string.Join(Environment.NewLine, line.Serials),
                        IsSerialTracked = line.ItemId.HasValue && _itemLookup.TryGetValue(line.ItemId.Value, out var it) && it.IsSerialTracked
                    });
                }
            }
            else
            {
                IsEditMode = false;
                Lines.Clear();
                Lines.Add(new InwardLineRow());
                InwardNo = await _inward.PreviewNextNumberAsync();
            }
        }, "Loading...");
    }

    private async Task LoadLookupsAsync()
    {
        Customers.Clear();
        foreach (var c in await _customers.GetDropdownAsync())
            Customers.Add(c);

        Vendors.Clear();
        foreach (var v in await _vendors.GetDropdownAsync())
            Vendors.Add(v);

        Purposes.Clear();
        foreach (var p in await _purposes.GetDropdownAsync())
            Purposes.Add(p);

        MasterItems.Clear();
        var page = await _items.GetPagedAsync(new Domain.Criteria.ItemSearchFilter { PageSize = 10000, IsActive = true });
        foreach (var item in page.Items)
        {
            MasterItems.Add(item);
            _itemLookup[item.Id] = item;
        }
    }

    public void ApplySelectedMasterItem(InwardLineRow row)
    {
        if (!row.ItemId.HasValue || !_itemLookup.TryGetValue(row.ItemId.Value, out var item))
            return;

        if (string.IsNullOrWhiteSpace(row.ItemName))
            row.ItemName = item.Name;
        if (string.IsNullOrWhiteSpace(row.ItemMake))
            row.ItemMake = item.Make;
        if (string.IsNullOrWhiteSpace(row.ItemModel))
            row.ItemModel = item.Model;
        if (string.IsNullOrWhiteSpace(row.HsnCode))
            row.HsnCode = item.HsnCode;
        if (string.IsNullOrWhiteSpace(row.Unit) || row.Unit == "Nos")
            row.Unit = item.Unit;
        row.IsSerialTracked = item.IsSerialTracked;
    }

    [RelayCommand]
    private void AddLine()
    {
        var row = new InwardLineRow();
        Lines.Add(row);
    }

    [RelayCommand]
    private void RemoveLine(InwardLineRow? row)
    {
        if (row is not null && Lines.Contains(row))
            Lines.Remove(row);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var request = new InwardSaveRequest
            {
                Id = _id,
                InwardDate = InwardDate,
                InwardType = InwardType,
                CustomerId = Customer?.Id,
                VendorId = Vendor?.Id,
                PurposeId = Purpose?.Id,
                ReferenceInvoiceNo = ReferenceInvoiceNo,
                ReferenceInvoiceDate = ReferenceInvoiceDate,
                ChallanNo = ChallanNo,
                TransportDetails = TransportDetails,
                Remarks = Remarks,
                Status = Status
            };

            foreach (var line in Lines)
            {
                ApplySelectedMasterItem(line);
                request.Items.Add(new InwardItemLineRequest
                {
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    ItemMake = line.ItemMake,
                    ItemModel = line.ItemModel,
                    HsnCode = line.HsnCode,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Amount = line.Amount,
                    Remarks = line.Remarks,
                    Serials = line.Serials
                });
            }

            var result = await _inward.SaveAsync(request);
            if (result.Success)
            {
                SetSuccess(result.Message);
                RequestClose?.Invoke(true);
            }
            else
            {
                ShowError(new Domain.Exceptions.DomainException(result.Message));
            }
        }, "Saving inward...");
    }
}
