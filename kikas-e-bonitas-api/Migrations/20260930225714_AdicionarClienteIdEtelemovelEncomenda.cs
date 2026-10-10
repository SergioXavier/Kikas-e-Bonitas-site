using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KikasEBonitas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarClienteIdEtelemovelEncomenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "Encomendas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telemovel",
                table: "Encomendas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PalavraPasseHash = table.Column<string>(type: "TEXT", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", nullable: true),
                    Morada = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoPostal = table.Column<string>(type: "TEXT", nullable: true),
                    Nif = table.Column<string>(type: "TEXT", nullable: true),
                    DataRegisto = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Encomendas_ClienteId",
                table: "Encomendas",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Encomendas_Clientes_ClienteId",
                table: "Encomendas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encomendas_Clientes_ClienteId",
                table: "Encomendas");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Encomendas_ClienteId",
                table: "Encomendas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Encomendas");

            migrationBuilder.DropColumn(
                name: "Telemovel",
                table: "Encomendas");
        }
    }
}
