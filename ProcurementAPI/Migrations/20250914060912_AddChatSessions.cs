using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProcurementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Messages = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    standard_cost = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    min_order_quantity = table.Column<int>(type: "integer", nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotes",
                columns: table => new
                {
                    rfq_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rfq_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    award_date = table.Column<DateOnly>(type: "date", nullable: true),
                    total_estimated_value = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_for_quotes", x => x.rfq_id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_terms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.supplier_id);
                });

            migrationBuilder.CreateTable(
                name: "item_specifications",
                columns: table => new
                {
                    spec_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    spec_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    spec_value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_specifications", x => x.spec_id);
                    table.ForeignKey(
                        name: "FK_item_specifications_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq_line_items",
                columns: table => new
                {
                    line_item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rfq_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    quantity_required = table.Column<int>(type: "integer", nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    technical_specifications = table.Column<string>(type: "text", nullable: true),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    estimated_unit_cost = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_line_items", x => x.line_item_id);
                    table.ForeignKey(
                        name: "FK_rfq_line_items_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_rfq_line_items_request_for_quotes_rfq_id",
                        column: x => x.rfq_id,
                        principalTable: "request_for_quotes",
                        principalColumn: "rfq_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    po_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    po_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    rfq_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_terms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_address = table.Column<string>(type: "text", nullable: true),
                    billing_address = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.po_id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_request_for_quotes_rfq_id",
                        column: x => x.rfq_id,
                        principalTable: "request_for_quotes",
                        principalColumn: "rfq_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rfq_suppliers",
                columns: table => new
                {
                    rfq_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    invited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    response_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_responded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_suppliers", x => new { x.rfq_id, x.supplier_id });
                    table.ForeignKey(
                        name: "FK_rfq_suppliers_request_for_quotes_rfq_id",
                        column: x => x.rfq_id,
                        principalTable: "request_for_quotes",
                        principalColumn: "rfq_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rfq_suppliers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_capabilities",
                columns: table => new
                {
                    capability_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    capability_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capability_value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_capabilities", x => x.capability_id);
                    table.ForeignKey(
                        name: "FK_supplier_capabilities_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    quote_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rfq_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    line_item_id = table.Column<int>(type: "integer", nullable: false),
                    quote_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    quantity_offered = table.Column<int>(type: "integer", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_terms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    warranty_period_months = table.Column<int>(type: "integer", nullable: true),
                    technical_compliance_notes = table.Column<string>(type: "text", nullable: true),
                    submitted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.quote_id);
                    table.ForeignKey(
                        name: "FK_quotes_request_for_quotes_rfq_id",
                        column: x => x.rfq_id,
                        principalTable: "request_for_quotes",
                        principalColumn: "rfq_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quotes_rfq_line_items_line_item_id",
                        column: x => x.line_item_id,
                        principalTable: "rfq_line_items",
                        principalColumn: "line_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quotes_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    po_line_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    po_id = table.Column<int>(type: "integer", nullable: false),
                    quote_id = table.Column<int>(type: "integer", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    quantity_ordered = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.po_line_id);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "purchase_orders",
                        principalColumn: "po_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "quote_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_specifications_item_id_spec_name_spec_value",
                table: "item_specifications",
                columns: new[] { "item_id", "spec_name", "spec_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_item_code",
                table: "items",
                column: "item_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_item_id",
                table: "purchase_order_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_po_id",
                table: "purchase_order_lines",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_quote_id",
                table: "purchase_order_lines",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_po_number",
                table: "purchase_orders",
                column: "po_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_rfq_id",
                table: "purchase_orders",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_line_item_id",
                table: "quotes",
                column: "line_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_rfq_id",
                table: "quotes",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_supplier_id",
                table: "quotes",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotes_rfq_number",
                table: "request_for_quotes",
                column: "rfq_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_line_items_item_id",
                table: "rfq_line_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_line_items_rfq_id",
                table: "rfq_line_items",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_suppliers_supplier_id",
                table: "rfq_suppliers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_capabilities_supplier_id_capability_type_capabilit~",
                table: "supplier_capabilities",
                columns: new[] { "supplier_id", "capability_type", "capability_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_supplier_code",
                table: "suppliers",
                column: "supplier_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropTable(
                name: "item_specifications");

            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "rfq_suppliers");

            migrationBuilder.DropTable(
                name: "supplier_capabilities");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "rfq_line_items");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "request_for_quotes");
        }
    }
}
