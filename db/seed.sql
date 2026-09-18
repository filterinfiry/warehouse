-- =============================================================================
-- Datos de ejemplo para pruebas funcionales.
--
-- Uso (BD de desarrollo en el puerto 5433, ver docker-compose.yml):
--   PGPASSWORD=warehouse psql -h localhost -p 5433 -U warehouse -d warehouse -f db/seed.sql
--   o dentro del contenedor:
--   docker exec -i warehouse-db psql -U warehouse -d warehouse < db/seed.sql
--
-- Es IDEMPOTENTE: borra primero las órdenes de ejemplo (external_ref 'OC-2026-1%')
-- y sus datos derivados, así que puede relanzarse cuantas veces haga falta.
-- NO toca órdenes reales ni las importadas desde el origen externo.
-- =============================================================================

begin;

-- Limpieza de datos de ejemplo previos (y sesiones colgando de ellos) --------
delete from verification_lines vl
    using verification_sessions vs, load_orders o
    where vl.verification_session_id = vs.id and vs.load_order_id = o.id
      and o.external_ref like 'OC-2026-1%';
delete from verification_sessions vs
    using load_orders o
    where vs.load_order_id = o.id and o.external_ref like 'OC-2026-1%';

delete from picking_lines pl
    using picking_sessions ps, load_orders o
    where pl.picking_session_id = ps.id and ps.load_order_id = o.id
      and o.external_ref like 'OC-2026-1%';
delete from picking_sessions ps
    using load_orders o
    where ps.load_order_id = o.id and o.external_ref like 'OC-2026-1%';

delete from load_order_lines
    where load_order_id in (select id from load_orders where external_ref like 'OC-2026-1%');
delete from load_orders where external_ref like 'OC-2026-1%';

-- Orden 1 — Alimentación seca (6 líneas) -------------------------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1001', 'Distribuciones García S.L.', date '2026-09-18', 'Entrega puerta 3')
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-100','8410179000015','Aceite de oliva virgen extra 5L', 24::numeric, 'ud'),
    ('L2','REF-101','8410179000022','Harina de trigo 25kg',            10, 'saco'),
    ('L3','REF-102','8410179000039','Azúcar blanco 1kg',              120, 'ud'),
    ('L4','REF-103','8480000123456','Sal marina fina 1kg',             60, 'ud'),
    ('L5','REF-104','8410179000053','Arroz redondo 5kg',               40, 'ud'),
    ('L6','REF-105','8410179000060','Pasta macarrón 500g',            200, 'ud')
) as v(eref, code, barcode, descr, qty, unit);

-- Orden 2 — Bebidas (5 líneas) -----------------------------------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1002', 'Hostelería del Sur', date '2026-09-18', 'Frío: mantener cadena')
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-200','8412345000019','Pack agua mineral 1.5L x6',   200::numeric, 'pack'),
    ('L2','REF-201','8412345000026','Refresco cola 2L',             90, 'ud'),
    ('L3','REF-202','8412345000033','Cerveza lager 33cl x24',       48, 'caja'),
    ('L4','REF-203','8412345000040','Zumo naranja 1L',             120, 'ud'),
    ('L5','REF-204','8412345000057','Tónica 20cl x6',               60, 'pack')
) as v(eref, code, barcode, descr, qty, unit);

-- Orden 3 — Pedido grande mixto (8 líneas) -----------------------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1003', 'Supermercados Vera', date '2026-09-19', 'Paletizado completo')
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-300','8437000111017','Leche entera 1L x6',          300::numeric, 'pack'),
    ('L2','REF-301','8437000111024','Galletas maría 800g',         150, 'ud'),
    ('L3','REF-302','8437000111031','Café molido 250g',            180, 'ud'),
    ('L4','REF-303','8437000111048','Cacao soluble 1kg',            90, 'ud'),
    ('L5','REF-304','8437000111055','Aceite girasol 1L',           240, 'ud'),
    ('L6','REF-305','8437000111062','Tomate frito 400g x3',        120, 'pack'),
    ('L7','REF-306','8437000111079','Atún claro aceite 3x80g',     160, 'pack'),
    ('L8','REF-307','8437000111086','Papel higiénico 12 rollos',    80, 'paquete')
) as v(eref, code, barcode, descr, qty, unit);

-- Orden 4 — Pedido pequeño (3 líneas) ----------------------------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1004', 'Bar Restaurante El Puerto', date '2026-09-18', null)
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-400','8400000000017','Servilletas 100ud',   50::numeric, 'paquete'),
    ('L2','REF-401','8400000000024','Vasos plástico 250ml x50', 20, 'paquete'),
    ('L3','REF-402','8400000000031','Bolsas basura 30L x25',    30, 'rollo')
) as v(eref, code, barcode, descr, qty, unit);

-- Orden 5 — Con kg y sacos, cantidades decimales (4 líneas) ------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1005', 'Panadería La Espiga', date '2026-09-18', 'Peso a granel')
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-500','8411223300018','Harina de fuerza a granel',  125.5::numeric, 'kg'),
    ('L2','REF-501','8411223300025','Levadura fresca 500g',        24,   'ud'),
    ('L3','REF-502','8411223300032','Semillas mixtas 5kg',          8,   'saco'),
    ('L4','REF-503','8411223300049','Azúcar glas 1kg',             40,   'ud')
) as v(eref, code, barcode, descr, qty, unit);

-- Orden 6 — No alimentación / ferretería (5 líneas) --------------------------
with o as (
    insert into load_orders (id, external_ref, customer, order_date, notes)
    values (gen_random_uuid(), 'OC-2026-1006', 'Ferretería Industrial Lorca', date '2026-09-19', 'Revisar embalaje frágil')
    returning id)
insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit)
select gen_random_uuid(), o.id, v.eref, v.code, v.barcode, v.descr, v.qty, v.unit
from o, (values
    ('L1','REF-600','8433221100011','Tornillo rosca chapa 4.8x38 (500u)', 20::numeric, 'caja'),
    ('L2','REF-601','8433221100028','Taco nylon 8mm (100u)',               30, 'caja'),
    ('L3','REF-602','8433221100035','Cinta aislante 19mm negra',          150, 'ud'),
    ('L4','REF-603','8433221100042','Guantes nitrilo talla L (100u)',      25, 'caja'),
    ('L5','REF-604','8433221100059','Brida nylon 200mm (100u)',            40, 'bolsa')
) as v(eref, code, barcode, descr, qty, unit);

commit;

-- Resumen -------------------------------------------------------------------
select o.external_ref, o.customer, count(l.id) as lineas, sum(l.quantity) as unidades
from load_orders o
left join load_order_lines l on l.load_order_id = o.id
where o.external_ref like 'OC-2026-1%'
group by o.external_ref, o.customer
order by o.external_ref;
