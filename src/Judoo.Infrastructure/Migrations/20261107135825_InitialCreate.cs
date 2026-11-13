using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Judoo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sigla = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campeonatos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "RASCUNHO"),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campeonatos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "atletas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    registro_federacao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    data_nascimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atletas", x => x.id);
                    table.ForeignKey(
                        name: "FK_atletas_academias_academia_id",
                        column: x => x.academia_id,
                        principalTable: "academias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campeonato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    divisao_peso = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    peso_limite_kg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tempo_luta_segundos = table.Column<int>(type: "integer", nullable: false, defaultValue: 240),
                    tempo_osaekomi_wazaari = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    tempo_osaekomi_ippon = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_campeonatos_campeonato_id",
                        column: x => x.campeonato_id,
                        principalTable: "campeonatos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chaves",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campeonato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ELIMINATORIA_SIMPLES"),
                    total_fases = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    gerada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chaves", x => x.id);
                    table.ForeignKey(
                        name: "FK_chaves_campeonatos_campeonato_id",
                        column: x => x.campeonato_id,
                        principalTable: "campeonatos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chaves_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pesagens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campeonato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peso_aferido_kg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE"),
                    data_pesagem = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    aferido_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pesagens", x => x.id);
                    table.ForeignKey(
                        name: "FK_pesagens_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pesagens_campeonatos_campeonato_id",
                        column: x => x.campeonato_id,
                        principalTable: "campeonatos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pesagens_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pesagens_usuarios_aferido_por_usuario_id",
                        column: x => x.aferido_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "eventos_luta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    luta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atleta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cor_judogi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tempo_luta_segundos = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    registrado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_luta", x => x.id);
                    table.ForeignKey(
                        name: "FK_eventos_luta_atletas_atleta_id",
                        column: x => x.atleta_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_eventos_luta_usuarios_registrado_por_usuario_id",
                        column: x => x.registrado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lutas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tatame_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atleta_branco_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atleta_azul_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vencedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fase = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ordem_na_chave = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ordem_no_tatame = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Agendada"),
                    tipo_vitoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    em_golden_score = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tempo_luta_segundos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    hora_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hora_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lutas", x => x.id);
                    table.ForeignKey(
                        name: "FK_lutas_atletas_atleta_azul_id",
                        column: x => x.atleta_azul_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lutas_atletas_atleta_branco_id",
                        column: x => x.atleta_branco_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lutas_atletas_vencedor_id",
                        column: x => x.vencedor_id,
                        principalTable: "atletas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_lutas_chaves_chave_id",
                        column: x => x.chave_id,
                        principalTable: "chaves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "placar_lutas",
                columns: table => new
                {
                    luta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ippon_branco = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    wazaari_branco = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    shido_branco = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ippon_azul = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    wazaari_azul = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    shido_azul = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    osaekomi_ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    osaekomi_cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    osaekomi_tempo_segundos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cronometro_segundos = table.Column<int>(type: "integer", nullable: false, defaultValue: 240),
                    em_golden_score = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cronometro_rodando = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sono_mama = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ultima_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placar_lutas", x => x.luta_id);
                    table.ForeignKey(
                        name: "FK_placar_lutas_lutas_luta_id",
                        column: x => x.luta_id,
                        principalTable: "lutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tatames",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campeonato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Disponivel"),
                    luta_atual_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mesario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tatames", x => x.id);
                    table.ForeignKey(
                        name: "FK_tatames_campeonatos_campeonato_id",
                        column: x => x.campeonato_id,
                        principalTable: "campeonatos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tatames_lutas_luta_atual_id",
                        column: x => x.luta_atual_id,
                        principalTable: "lutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tatames_usuarios_mesario_id",
                        column: x => x.mesario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_atletas_academia_id",
                table: "atletas",
                column: "academia_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_campeonato_id",
                table: "categorias",
                column: "campeonato_id");

            migrationBuilder.CreateIndex(
                name: "IX_chaves_campeonato_id",
                table: "chaves",
                column: "campeonato_id");

            migrationBuilder.CreateIndex(
                name: "IX_chaves_categoria_id",
                table: "chaves",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_luta_atleta_id",
                table: "eventos_luta",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_luta_luta_id_timestamp_utc",
                table: "eventos_luta",
                columns: new[] { "luta_id", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_luta_registrado_por_usuario_id",
                table: "eventos_luta",
                column: "registrado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_lutas_atleta_azul_id",
                table: "lutas",
                column: "atleta_azul_id");

            migrationBuilder.CreateIndex(
                name: "IX_lutas_atleta_branco_id",
                table: "lutas",
                column: "atleta_branco_id");

            migrationBuilder.CreateIndex(
                name: "IX_lutas_chave_id",
                table: "lutas",
                column: "chave_id");

            migrationBuilder.CreateIndex(
                name: "IX_lutas_tatame_id_status",
                table: "lutas",
                columns: new[] { "tatame_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_lutas_vencedor_id",
                table: "lutas",
                column: "vencedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesagens_aferido_por_usuario_id",
                table: "pesagens",
                column: "aferido_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesagens_atleta_id",
                table: "pesagens",
                column: "atleta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesagens_campeonato_id_atleta_id",
                table: "pesagens",
                columns: new[] { "campeonato_id", "atleta_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pesagens_categoria_id",
                table: "pesagens",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_tatames_campeonato_id_numero",
                table: "tatames",
                columns: new[] { "campeonato_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tatames_luta_atual_id",
                table: "tatames",
                column: "luta_atual_id");

            migrationBuilder.CreateIndex(
                name: "IX_tatames_mesario_id",
                table: "tatames",
                column: "mesario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_luta_lutas_luta_id",
                table: "eventos_luta",
                column: "luta_id",
                principalTable: "lutas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lutas_tatames_tatame_id",
                table: "lutas",
                column: "tatame_id",
                principalTable: "tatames",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_atletas_academias_academia_id",
                table: "atletas");

            migrationBuilder.DropForeignKey(
                name: "FK_categorias_campeonatos_campeonato_id",
                table: "categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_chaves_campeonatos_campeonato_id",
                table: "chaves");

            migrationBuilder.DropForeignKey(
                name: "FK_tatames_campeonatos_campeonato_id",
                table: "tatames");

            migrationBuilder.DropForeignKey(
                name: "FK_chaves_categorias_categoria_id",
                table: "chaves");

            migrationBuilder.DropForeignKey(
                name: "FK_lutas_atletas_atleta_azul_id",
                table: "lutas");

            migrationBuilder.DropForeignKey(
                name: "FK_lutas_atletas_atleta_branco_id",
                table: "lutas");

            migrationBuilder.DropForeignKey(
                name: "FK_lutas_atletas_vencedor_id",
                table: "lutas");

            migrationBuilder.DropForeignKey(
                name: "FK_tatames_lutas_luta_atual_id",
                table: "tatames");

            migrationBuilder.DropTable(
                name: "eventos_luta");

            migrationBuilder.DropTable(
                name: "pesagens");

            migrationBuilder.DropTable(
                name: "placar_lutas");

            migrationBuilder.DropTable(
                name: "academias");

            migrationBuilder.DropTable(
                name: "campeonatos");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "atletas");

            migrationBuilder.DropTable(
                name: "lutas");

            migrationBuilder.DropTable(
                name: "chaves");

            migrationBuilder.DropTable(
                name: "tatames");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
