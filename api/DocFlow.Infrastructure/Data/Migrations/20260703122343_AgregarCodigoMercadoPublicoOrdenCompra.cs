using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigoMercadoPublicoOrdenCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_mercado_publico",
                schema: "docflow",
                table: "ordenes_compra",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "codigo_mercado_publico",
                schema: "docflow",
                table: "ordenes_compra");
        }
    }
}
