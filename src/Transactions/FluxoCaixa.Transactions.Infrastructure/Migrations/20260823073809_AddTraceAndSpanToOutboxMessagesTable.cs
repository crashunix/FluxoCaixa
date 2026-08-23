using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxoCaixa.Transactions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceAndSpanToOutboxMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpanId",
                table: "OutboxMessages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "OutboxMessages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpanId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "OutboxMessages");
        }
    }
}
