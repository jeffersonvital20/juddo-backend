-- ============================================================================
-- JUDOO - SISTEMA CORPORATIVO DE GERENCIAMENTO DE CAMPEONATOS DE JUDÔ
-- MODELAGEM DE BANCO DE DADOS POSTGRESQL 16 (HIGH PERFORMANCE & CONCURRENCY)
-- REGRAS FIJ (FEDERAÇÃO INTERNACIONAL DE JUDÔ)
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- ENUMS DO DOMÍNIO
-- ============================================================================
DO $$ BEGIN
    CREATE TYPE perfil_usuario AS ENUM ('ADMIN', 'SECRETARIA', 'MESARIO', 'PUBLICO');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE status_campeonato AS ENUM ('RASCUNHO', 'EM_ANDAMENTO', 'FINALIZADO', 'CANCELADO');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE sexo_atleta AS ENUM ('MASCULINO', 'FEMININO');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE status_tatame AS ENUM ('DISPONIVEL', 'EM_LUTA', 'PAUSADO', 'MANUTENCAO');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE status_pesagem AS ENUM ('PENDENTE', 'APROVADO', 'REPROVADO');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE tipo_chave AS ENUM ('ELIMINATORIA_SIMPLES', 'REPESCAGEM_FIJ', 'ROUND_ROBIN');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE status_luta AS ENUM ('AGENDADA', 'CHAMADA', 'EM_ANDAMENTO', 'PAUSADA', 'FINALIZADA', 'CANCELADA');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE cor_judogi AS ENUM ('BRANCO', 'AZUL');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE tipo_pontuacao AS ENUM (
        'WAZA_ARI', 
        'IPPON', 
        'SHIDO', 
        'CANCELAR_WAZA_ARI', 
        'CANCELAR_SHIDO'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE tipo_vitoria AS ENUM (
        'IPPON', 
        'WAZA_ARI_AWASETE_IPPON', 
        'WAZA_ARI', 
        'HANSOKU_MAKE', 
        'DECISAO', 
        'WO', 
        'DESISTENCIA'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

-- ============================================================================
-- TABELAS
-- ============================================================================

-- 1. Usuários e Perfis
CREATE TABLE IF NOT EXISTS usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    role perfil_usuario NOT NULL DEFAULT 'PUBLICO',
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Campeonatos
CREATE TABLE IF NOT EXISTS campeonatos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(200) NOT NULL,
    local VARCHAR(255) NOT NULL,
    data_inicio DATE NOT NULL,
    data_fim DATE NOT NULL,
    status status_campeonato NOT NULL DEFAULT 'RASCUNHO',
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. Tatames (Áreas de Combate)
CREATE TABLE IF NOT EXISTS tatames (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campeonato_id UUID NOT NULL REFERENCES campeonatos(id) ON DELETE CASCADE,
    numero INT NOT NULL,
    nome VARCHAR(100) NOT NULL,
    status status_tatame NOT NULL DEFAULT 'DISPONIVEL',
    luta_atual_id UUID,
    mesario_id UUID REFERENCES usuarios(id),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_campeonato_tatame_numero UNIQUE (campeonato_id, numero)
);

-- 4. Equipes / Academias
CREATE TABLE IF NOT EXISTS academias (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(150) NOT NULL,
    sigla VARCHAR(10) NOT NULL,
    cidade VARCHAR(100),
    estado VARCHAR(2),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 5. Categorias
CREATE TABLE IF NOT EXISTS categorias (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campeonato_id UUID NOT NULL REFERENCES campeonatos(id) ON DELETE CASCADE,
    classe VARCHAR(50) NOT NULL,          -- Sub-13, Sub-15, Sub-18, Sub-21, Sênior, Master
    divisao_peso VARCHAR(30) NOT NULL,     -- Ex: -60kg, -66kg, -73kg, +100kg
    peso_limite_kg NUMERIC(5,2) NOT NULL,
    sexo sexo_atleta NOT NULL,
    tempo_luta_segundos INT NOT NULL DEFAULT 240, -- 4 minutos regulamentar FIJ
    tempo_osaekomi_wazaari INT NOT NULL DEFAULT 10,
    tempo_osaekomi_ippon INT NOT NULL DEFAULT 20,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 6. Atletas
CREATE TABLE IF NOT EXISTS atletas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    academia_id UUID NOT NULL REFERENCES academias(id),
    nome VARCHAR(150) NOT NULL,
    registro_federacao VARCHAR(50),
    data_nascimento DATE NOT NULL,
    sexo sexo_atleta NOT NULL,
    foto_url VARCHAR(500),
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 7. Pesagens
CREATE TABLE IF NOT EXISTS pesagens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campeonato_id UUID NOT NULL REFERENCES campeonatos(id) ON DELETE CASCADE,
    atleta_id UUID NOT NULL REFERENCES atletas(id) ON DELETE CASCADE,
    categoria_id UUID NOT NULL REFERENCES categorias(id),
    peso_aferido_kg NUMERIC(5,2) NOT NULL,
    status status_pesagem NOT NULL DEFAULT 'PENDENTE',
    data_pesagem TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    aferido_por_usuario_id UUID REFERENCES usuarios(id),
    observacoes TEXT,
    CONSTRAINT uq_campeonato_atleta_pesagem UNIQUE (campeonato_id, atleta_id)
);

-- 8. Chaves / Brackets
CREATE TABLE IF NOT EXISTS chaves (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campeonato_id UUID NOT NULL REFERENCES campeonatos(id) ON DELETE CASCADE,
    categoria_id UUID NOT NULL REFERENCES categorias(id) ON DELETE CASCADE,
    tipo tipo_chave NOT NULL DEFAULT 'ELIMINATORIA_SIMPLES',
    total_fases INT NOT NULL DEFAULT 1,
    gerada BOOLEAN NOT NULL DEFAULT FALSE,
    criada_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 9. Lutas (Shiai)
CREATE TABLE IF NOT EXISTS lutas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chave_id UUID NOT NULL REFERENCES chaves(id) ON DELETE CASCADE,
    tatame_id UUID REFERENCES tatames(id),
    atleta_branco_id UUID REFERENCES atletas(id),
    atleta_azul_id UUID REFERENCES atletas(id),
    vencedor_id UUID REFERENCES atletas(id),
    fase INT NOT NULL DEFAULT 1,            -- 1: Final, 2: Semifinal, 4: Quartas, etc.
    ordem_na_chave INT NOT NULL DEFAULT 1,
    ordem_no_tatame INT,
    status status_luta NOT NULL DEFAULT 'AGENDADA',
    tipo_vitoria tipo_vitoria,
    em_golden_score BOOLEAN NOT NULL DEFAULT FALSE,
    tempo_luta_segundos INT NOT NULL DEFAULT 0,
    hora_inicio TIMESTAMPTZ,
    hora_fim TIMESTAMPTZ,
    criada_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- FK circular tatames -> lutas
ALTER TABLE tatames DROP CONSTRAINT IF EXISTS fk_tatames_luta_atual;
ALTER TABLE tatames ADD CONSTRAINT fk_tatames_luta_atual 
    FOREIGN KEY (luta_atual_id) REFERENCES lutas(id) ON DELETE SET NULL;

-- 10. Placar Atual da Luta (Leitura de baixa latência em memória/banco)
CREATE TABLE IF NOT EXISTS placar_lutas (
    luta_id UUID PRIMARY KEY REFERENCES lutas(id) ON DELETE CASCADE,
    ippon_branco INT NOT NULL DEFAULT 0,
    wazaari_branco INT NOT NULL DEFAULT 0,
    shido_branco INT NOT NULL DEFAULT 0,
    ippon_azul INT NOT NULL DEFAULT 0,
    wazaari_azul INT NOT NULL DEFAULT 0,
    shido_azul INT NOT NULL DEFAULT 0,
    osaekomi_ativo BOOLEAN NOT NULL DEFAULT FALSE,
    osaekomi_cor cor_judogi,
    osaekomi_tempo_segundos INT NOT NULL DEFAULT 0,
    cronometro_segundos INT NOT NULL DEFAULT 240, -- Regressivo normal ou progressivo Golden Score
    em_golden_score BOOLEAN NOT NULL DEFAULT FALSE,
    cronometro_rodando BOOLEAN NOT NULL DEFAULT FALSE,
    sono_mama BOOLEAN NOT NULL DEFAULT FALSE,
    ultima_atualizacao TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 11. Eventos da Luta (Histórico Imutável / Event Sourcing para auditoria)
CREATE TABLE IF NOT EXISTS eventos_luta (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    luta_id UUID NOT NULL REFERENCES lutas(id) ON DELETE CASCADE,
    atleta_id UUID REFERENCES atletas(id),
    cor_judogi cor_judogi NOT NULL,
    tipo tipo_pontuacao NOT NULL,
    tempo_luta_segundos INT NOT NULL,
    descricao VARCHAR(255),
    registrado_por_usuario_id UUID REFERENCES usuarios(id),
    timestamp_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- ÍNDICES ESTRATÉGICOS (ALTA PERFORMANCE)
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_tatames_campeonato ON tatames(campeonato_id);
CREATE INDEX IF NOT EXISTS idx_lutas_tatame_status ON lutas(tatame_id, status);
CREATE INDEX IF NOT EXISTS idx_lutas_chave ON lutas(chave_id);
CREATE INDEX IF NOT EXISTS idx_placar_lutas_luta_id ON placar_lutas(luta_id);
CREATE INDEX IF NOT EXISTS idx_eventos_luta_luta_tempo ON eventos_luta(luta_id, timestamp_utc);
CREATE INDEX IF NOT EXISTS idx_atletas_academia ON atletas(academia_id);
CREATE INDEX IF NOT EXISTS idx_pesagens_campeonato_categoria ON pesagens(campeonato_id, categoria_id);

-- ============================================================================
-- DADOS INICIAIS (SEED)
-- ============================================================================
INSERT INTO usuarios (id, nome, email, senha_hash, role) VALUES
    ('00000000-0000-0000-0000-000000000001', 'Coordenador Geral (Admin)', 'admin@judoo.com', '$2a$11$samplehashAdminJudoo123456789', 'ADMIN'),
    ('00000000-0000-0000-0000-000000000002', 'Secretaria Oficial', 'secretaria@judoo.com', '$2a$11$samplehashSecretariaJudoo1234', 'SECRETARIA'),
    ('00000000-0000-0000-0000-000000000003', 'Mesário Tatame 1', 'mesario1@judoo.com', '$2a$11$samplehashMesario1Judoo123456', 'MESARIO'),
    ('00000000-0000-0000-0000-000000000004', 'Público / Telão', 'publico@judoo.com', '$2a$11$samplehashPublicoJudoo1234567', 'PUBLICO')
ON CONFLICT (id) DO NOTHING;

INSERT INTO campeonatos (id, nome, local, data_inicio, data_fim, status) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Grand Prix Nacional de Judô 2026', 'Centro Pan-Americano de Judô - Lauro de Freitas/BA', CURRENT_DATE, CURRENT_DATE + INTERVAL '2 days', 'EM_ANDAMENTO')
ON CONFLICT (id) DO NOTHING;

INSERT INTO academias (id, nome, sigla, cidade, estado) VALUES
    ('20000000-0000-0000-0000-000000000001', 'Esporte Clube Pinheiros', 'ECP', 'São Paulo', 'SP'),
    ('20000000-0000-0000-0000-000000000002', 'Minas Tênis Clube', 'MTC', 'Belo Horizonte', 'MG'),
    ('20000000-0000-0000-0000-000000000003', 'Sogipa Judô', 'SOG', 'Porto Alegre', 'RS'),
    ('20000000-0000-0000-0000-000000000004', 'Instituto Reação', 'IRC', 'Rio de Janeiro', 'RJ')
ON CONFLICT (id) DO NOTHING;

INSERT INTO categorias (id, campeonato_id, classe, divisao_peso, peso_limite_kg, sexo, tempo_luta_segundos) VALUES
    ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Sênior', '-66kg Meio-Leve', 66.00, 'MASCULINO', 240),
    ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'Sênior', '-73kg Leve', 73.00, 'MASCULINO', 240),
    ('30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'Sênior', '-52kg Meio-Leve', 52.00, 'FEMININO', 240)
ON CONFLICT (id) DO NOTHING;

INSERT INTO atletas (id, academia_id, nome, registro_federacao, data_nascimento, sexo) VALUES
    ('40000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Gabriel Santos', 'CBJ-98214', '1999-05-14', 'MASCULINO'),
    ('40000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', 'Lucas Oliveira', 'CBJ-84192', '2001-08-22', 'MASCULINO'),
    ('40000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000003', 'Matheus Takaki', 'CBJ-73129', '2000-01-10', 'MASCULINO'),
    ('40000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000004', 'Rafael Silva', 'CBJ-61204', '1998-11-30', 'MASCULINO')
ON CONFLICT (id) DO NOTHING;

INSERT INTO chaves (id, campeonato_id, categoria_id, tipo, total_fases, gerada) VALUES
    ('50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'ELIMINATORIA_SIMPLES', 2, TRUE)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tatames (id, campeonato_id, numero, nome, status, mesario_id) VALUES
    ('60000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 1, 'Tatame 1 - Shiai-Jo A', 'EM_LUTA', '00000000-0000-0000-0000-000000000003'),
    ('60000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 2, 'Tatame 2 - Shiai-Jo B', 'DISPONIVEL', NULL)
ON CONFLICT (id) DO NOTHING;

-- Luta 1 (Final Sub-66kg)
INSERT INTO lutas (id, chave_id, tatame_id, atleta_branco_id, atleta_azul_id, fase, ordem_na_chave, ordem_no_tatame, status, em_golden_score, tempo_luta_segundos, hora_inicio) VALUES
    ('70000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', 1, 1, 1, 'EM_LUTA', FALSE, 240, NOW())
ON CONFLICT (id) DO NOTHING;

-- Atualiza luta atual do Tatame 1
UPDATE tatames SET luta_atual_id = '70000000-0000-0000-0000-000000000001' WHERE id = '60000000-0000-0000-0000-000000000001';

-- Placar inicial da Luta 1
INSERT INTO placar_lutas (luta_id, ippon_branco, wazaari_branco, shido_branco, ippon_azul, wazaari_azul, shido_azul, osaekomi_ativo, osaekomi_cor, osaekomi_tempo_segundos, cronometro_segundos, em_golden_score, cronometro_rodando, sono_mama) VALUES
    ('70000000-0000-0000-0000-000000000001', 0, 1, 1, 0, 0, 2, FALSE, NULL, 0, 184, FALSE, TRUE, FALSE)
ON CONFLICT (luta_id) DO NOTHING;
