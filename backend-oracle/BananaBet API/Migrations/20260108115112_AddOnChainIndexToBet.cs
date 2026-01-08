using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BananaBet_API.Migrations
{
    /// <inheritdoc />
    public partial class AddOnChainIndexToBet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnChainIndex",
                table: "Bets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnChainIndex",
                table: "Bets");
        }
    }
}
