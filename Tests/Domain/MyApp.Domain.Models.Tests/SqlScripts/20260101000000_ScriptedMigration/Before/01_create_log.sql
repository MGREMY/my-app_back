-- Before/01_create_log.sql
CREATE TABLE script_log (seq serial PRIMARY KEY, name text NOT NULL);
INSERT INTO script_log (name) VALUES ('Before/01_create_log.sql');
