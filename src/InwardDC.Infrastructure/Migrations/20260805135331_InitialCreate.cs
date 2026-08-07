using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwardDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Pincode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GSTIN = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PAN = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenceCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    LastNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    Group = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginIp = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Pincode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GSTIN = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PAN = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Make = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    HsnCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IsSerialTracked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InwardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InwardNo = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    InwardDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InwardType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReferenceInvoiceNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReferenceInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ChallanNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransportDetails = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InwardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InwardEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InwardEntries_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SerialNo = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    EventedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    EventedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemEvents_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchChallans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DcNo = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DcDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceInwardEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReferenceChallanNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransportDetails = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchChallans_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchChallans_InwardEntries_SourceInwardEntryId",
                        column: x => x.SourceInwardEntryId,
                        principalTable: "InwardEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InwardItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InwardEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ItemMake = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ItemModel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    HsnCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DispatchedQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InwardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InwardItems_InwardEntries_InwardEntryId",
                        column: x => x.InwardEntryId,
                        principalTable: "InwardEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InwardItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UploadedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DispatchChallanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InwardEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_DispatchChallans_DispatchChallanId",
                        column: x => x.DispatchChallanId,
                        principalTable: "DispatchChallans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_InwardEntries_InwardEntryId",
                        column: x => x.InwardEntryId,
                        principalTable: "InwardEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DispatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DispatchChallanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceInwardItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ItemMake = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ItemModel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    HsnCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchItems_DispatchChallans_DispatchChallanId",
                        column: x => x.DispatchChallanId,
                        principalTable: "DispatchChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchItems_InwardItems_SourceInwardItemId",
                        column: x => x.SourceInwardItemId,
                        principalTable: "InwardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SerialNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SerialNo = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InwardEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InwardItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DispatchChallanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DispatchItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DispatchedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_DispatchChallans_DispatchChallanId",
                        column: x => x.DispatchChallanId,
                        principalTable: "DispatchChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_DispatchItems_DispatchItemId",
                        column: x => x.DispatchItemId,
                        principalTable: "DispatchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_InwardEntries_InwardEntryId",
                        column: x => x.InwardEntryId,
                        principalTable: "InwardEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_InwardItems_InwardItemId",
                        column: x => x.InwardItemId,
                        principalTable: "InwardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_DispatchChallanId",
                table: "Attachments",
                column: "DispatchChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EntityType_EntityId",
                table: "Attachments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_InwardEntryId",
                table: "Attachments",
                column: "InwardEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchChallans_CustomerId",
                table: "DispatchChallans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchChallans_DcDate",
                table: "DispatchChallans",
                column: "DcDate");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchChallans_DcNo",
                table: "DispatchChallans",
                column: "DcNo");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchChallans_SourceInwardEntryId",
                table: "DispatchChallans",
                column: "SourceInwardEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchItems_DispatchChallanId",
                table: "DispatchItems",
                column: "DispatchChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchItems_ItemId",
                table: "DispatchItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchItems_SourceInwardItemId",
                table: "DispatchItems",
                column: "SourceInwardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_CustomerId",
                table: "InwardEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_InwardDate",
                table: "InwardEntries",
                column: "InwardDate");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_InwardNo",
                table: "InwardEntries",
                column: "InwardNo");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_ReferenceInvoiceNo",
                table: "InwardEntries",
                column: "ReferenceInvoiceNo");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_VendorId",
                table: "InwardEntries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardItems_InwardEntryId",
                table: "InwardItems",
                column: "InwardEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardItems_ItemId",
                table: "InwardItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_Code",
                table: "ItemCategories",
                column: "Code",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEvents_EventedOn",
                table: "ItemEvents",
                column: "EventedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEvents_ItemId",
                table: "ItemEvents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEvents_SerialNo",
                table: "ItemEvents",
                column: "SerialNo");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Code",
                table: "Items",
                column: "Code",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Make_Model",
                table: "Items",
                columns: new[] { "Make", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceCounters_EntityName_Year",
                table: "SequenceCounters",
                columns: new[] { "EntityName", "Year" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_DispatchChallanId",
                table: "SerialNumbers",
                column: "DispatchChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_DispatchItemId",
                table: "SerialNumbers",
                column: "DispatchItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_InwardEntryId",
                table: "SerialNumbers",
                column: "InwardEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_InwardItemId",
                table: "SerialNumbers",
                column: "InwardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ItemId",
                table: "SerialNumbers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SerialNo",
                table: "SerialNumbers",
                column: "SerialNo",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive",
                table: "Users",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Code",
                table: "Vendors",
                column: "Code",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ItemEvents");

            migrationBuilder.DropTable(
                name: "SequenceCounters");

            migrationBuilder.DropTable(
                name: "SerialNumbers");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "DispatchItems");

            migrationBuilder.DropTable(
                name: "DispatchChallans");

            migrationBuilder.DropTable(
                name: "InwardItems");

            migrationBuilder.DropTable(
                name: "InwardEntries");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "ItemCategories");
        }
    }
}
