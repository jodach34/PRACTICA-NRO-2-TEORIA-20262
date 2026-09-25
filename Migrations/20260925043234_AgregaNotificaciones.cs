using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRACTICA_NRO_2_TEORIA_20262.Migrations
{
    /// <inheritdoc />
    public partial class AgregaNotificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    FechaProcesamientoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-analista-id",
                column: "ConcurrencyStamp",
                value: "6716467b-df06-470e-b0b3-82c2dced0c27");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-analista-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "af3b2c74-7005-4135-bd58-cb69d8e4e409", "AQAAAAIAAYagAAAAEAY4IPvVnWCrbOwnlAYDKlG0AMs0amyhr2TGnCP3SM31nnHSgI0yhT+WOrkj75YwCQ==", "146c83bb-e120-4941-8cad-5ee083f0dc57" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-cliente1-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ac80ea8f-5a61-4c4c-919e-08b1b2520ad0", "AQAAAAIAAYagAAAAEGo3QVy35gzMxNZvKIsI/YButCgWGwybWWm9FN/nPBveY+OUPy90xOKgVR1LpBOkvA==", "b19f579d-4ec0-4429-9314-81bc1144863d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-cliente2-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "70daac08-74c5-43d5-badf-369644f55d60", "AQAAAAIAAYagAAAAEG/LCdvHa3XjEOErvM5JX0wRmZ+szwVPikZvLhsHmA02fMO9OOg7pkMrIWisFRqAYA==", "99ad1475-a13f-4dbe-ad86-f27c06e15aec" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "rol-analista-id",
                column: "ConcurrencyStamp",
                value: "16952edc-1908-4978-96e9-0839b7c20558");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-analista-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ffab50b0-8e1b-4de0-9547-8c729192b988", "AQAAAAIAAYagAAAAEHInnejtP5rUaBb9o0siBZpohY9zve0yASAQqRqOOnSII1U4A7i4k5JztGq58jBrBQ==", "2fb6fd2e-ec37-42dd-9f47-7e1103444351" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-cliente1-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e39a2e11-f8d2-4e6e-8e97-580e0ea0fa20", "AQAAAAIAAYagAAAAECKGWHxkjecf8ZiKWnDV1d9Tjre5Enve5qMTn8sYE6jAeDWEdXwfk/6ZSgSfGalx2Q==", "06b6fa18-bd5d-4df8-b6e1-727f0b99f4fc" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "user-cliente2-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "666fe367-859f-4af1-88c7-ce62f4d76c03", "AQAAAAIAAYagAAAAEPCDk1ihqCabU9LTdJQpTKLVC5rVyoWzLKt3ENDWr9TjpGzQi5DC68b49jsNJgXSJw==", "183a344d-f021-4e2e-b987-c679b53157a1" });
        }
    }
}
