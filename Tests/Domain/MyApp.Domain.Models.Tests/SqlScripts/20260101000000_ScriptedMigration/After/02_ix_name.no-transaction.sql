-- After/02_ix_name.no-transaction.sql
CREATE INDEX CONCURRENTLY ix_scripted_entity_name ON scripted_entity (name);
INSERT INTO script_log (name) VALUES ('After/02_ix_name.no-transaction.sql');
