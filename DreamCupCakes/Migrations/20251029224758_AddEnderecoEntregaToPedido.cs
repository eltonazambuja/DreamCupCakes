using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamCupCakes.Migrations
{
    /// <inheritdoc />
    public partial class AddEnderecoEntregaToPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega",
                table: "Pedidos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnderecoEntrega",
                table: "Pedidos");
        }
    }
}
