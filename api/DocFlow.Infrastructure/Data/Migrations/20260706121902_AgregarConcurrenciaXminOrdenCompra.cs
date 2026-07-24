using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConcurrenciaXminOrdenCompra : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Intentionally empty: xmin is a PostgreSQL system column that already exists
        /// on every table. This migration only aligns the EF model snapshot so the
        /// entity maps it as an optimistic concurrency token.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: xmin is a system column; nothing to create.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: never drop the xmin system column.
        }
    }
}
