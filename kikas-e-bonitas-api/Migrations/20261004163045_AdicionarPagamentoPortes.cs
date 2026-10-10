using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KikasEBonitas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPagamentoPortes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustoPortes",
                table: "Encomendas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPagamento",
                table: "Encomendas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustoPortes",
                table: "Encomendas");

            migrationBuilder.DropColumn(
                name: "MetodoPagamento",
                table: "Encomendas");
        }
    }
}
