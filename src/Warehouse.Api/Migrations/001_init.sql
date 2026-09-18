-- Esquema inicial de gestión de almacén.

create table if not exists load_orders (
    id           uuid primary key,
    external_ref text        not null,
    customer     text,
    order_date   date,
    notes        text,
    created_at   timestamptz not null default now(),
    unique (external_ref)
);

create table if not exists load_order_lines (
    id            uuid primary key,
    load_order_id uuid          not null references load_orders(id) on delete cascade,
    external_ref  text,
    product_code  text,
    barcode       text,
    description   text          not null default '',
    quantity      numeric(14,3) not null default 0,
    unit          text
);
create index if not exists ix_load_order_lines_order on load_order_lines(load_order_id);
create index if not exists ix_load_order_lines_barcode on load_order_lines(barcode);

-- Flujo 1: verificación de recepción --------------------------------------

create table if not exists verification_sessions (
    id                   uuid primary key,
    load_order_id        uuid        not null references load_orders(id),
    status               int         not null default 0,
    final_observation    text,
    supervisor_name      text,
    supervisor_signature text,
    carrier_name         text,
    carrier_signature    text,
    created_at           timestamptz not null default now(),
    completed_at         timestamptz
);
create index if not exists ix_verification_sessions_order on verification_sessions(load_order_id);

create table if not exists verification_lines (
    id                      uuid primary key,
    verification_session_id uuid          not null references verification_sessions(id) on delete cascade,
    load_order_line_id      uuid          not null references load_order_lines(id),
    result                  int           not null default 0,
    observation             text,
    product_code            text,
    barcode                 text,
    description             text          not null default '',
    quantity                numeric(14,3) not null default 0,
    unit                    text
);
create index if not exists ix_verification_lines_session on verification_lines(verification_session_id);

-- Flujo 2: preparación de pedido -------------------------------------------

create table if not exists picking_sessions (
    id                 uuid primary key,
    load_order_id      uuid        not null references load_orders(id),
    status             int         not null default 0,
    observation        text,
    preparer_name      text,
    preparer_signature text,
    created_at         timestamptz not null default now(),
    completed_at       timestamptz
);
create index if not exists ix_picking_sessions_order on picking_sessions(load_order_id);

create table if not exists picking_lines (
    id                 uuid primary key,
    picking_session_id uuid          not null references picking_sessions(id) on delete cascade,
    load_order_line_id uuid          not null references load_order_lines(id),
    barcode            text,
    required_quantity  numeric(14,3) not null default 0,
    loaded_quantity    numeric(14,3) not null default 0,
    status             int           not null default 0,
    observation        text,
    product_code       text,
    description        text          not null default '',
    unit               text
);
create index if not exists ix_picking_lines_session on picking_lines(picking_session_id);
