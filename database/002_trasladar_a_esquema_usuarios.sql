-- Usar SOLO si ya se ejecutó la versión anterior de 001 en public.
-- Conserva registros, índices y relaciones. Si falta una tabla o existe
-- otra con el mismo nombre en el destino, se cancela toda la operación.
BEGIN;

CREATE SCHEMA IF NOT EXISTS usuarios;

ALTER TABLE public.personas SET SCHEMA usuarios;
ALTER TABLE public.perfiles SET SCHEMA usuarios;
ALTER TABLE public.usuarios SET SCHEMA usuarios;

COMMIT;
