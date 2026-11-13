---
marp: true
theme: default
paginate: true
title: TALLER .NET — Presentación
description: Grupo 3 — Sesión empresas y Fundación Adecco
---

# TALLER .NET

Gestión de taller mecánico en la web

**Grupo 3** · Formación .NET

---

## El reto en el taller

- Información repartida entre recepción, mecánico y almacén
- Poca visibilidad para el cliente
- Errores de stock y falta de trazabilidad

---

## Nuestra respuesta

- Un solo sistema web
- Roles: administrador, mecánico, cliente
- Reparaciones con coste y piezas trazables

---

## Quién está detrás

- Grupo 3 — equipo de desarrollo
- Formación práctica en producto real
- Repositorio abierto en GitHub

---

## Competencias que trabajamos

- .NET 10 y ASP.NET Core MVC
- Datos con Entity Framework Core
- Identity, pruebas, Docker y CI

---

## Arquitectura en una vista

- Interfaz web (Bootstrap + Razor)
- Lógica de negocio en el servidor
- Base de datos relacional (SQLite en la demo)

---

## Actores del sistema

| Perfil | Rol |
|--------|-----|
| Admin | Usuarios y operación completa |
| Mechanic | Taller día a día |
| Customer | Solo sus datos |
| User | Hasta asignación de rol |

---

## Modelo de datos

Usuario → vehículos → reparaciones → piezas → recambios

*(Ver `docs/img/modelo_entidad_relacion.png` en el repositorio)*

---

## Demo en vivo

- Aplicación en entorno de desarrollo
- Cuentas de ejemplo — ver `docs/demo.md`
- Recorrido ~20 minutos

---

## El cliente informado

- Solo sus vehículos
- Detalle con coste y piezas
- Transparencia sin ver datos ajenos

---

## Mecánico y almacén

- Reparación nueva con horas y tarifa
- Pieza consumida → stock actualizado
- Aviso si no hay stock suficiente

---

## Precio histórico

- El catálogo puede cambiar de precio
- Las reparaciones pasadas conservan el importe de entonces

---

## ¿Preguntas del recorrido?

Pausa breve antes de reglas de negocio

---

## Integridad de los datos

- No se borra un vehículo con historial de reparaciones
- No se borra un recambio ya usado en una reparación

---

## Cómo se calcula el coste

**Coste = (horas × tarifa) + suma de piezas**

Se calcula al consultar cada reparación

---

## Stock automático

- Añadir, editar o quitar piezas mueve el almacén
- Validación de cantidades y stock disponible

---

## Calidad y método

- Tests automatizados (xUnit)
- Integración continua en GitHub Actions
- Pull requests y documentación en el wiki

---

## Madurez hoy

- Prototipo funcional **avanzado**
- Ideal para demo y evaluación formativa
- Persistencia de producción: siguiente fase del roadmap

---

## Roadmap hacia un piloto

1. Migraciones y datos persistentes
2. Seed solo en desarrollo
3. Autorización por rol en módulos de taller
4. Alertas de stock mínimo
5. Licencia y piloto con empresa colaboradora

---

## Colaboración con empresas

- Usuarios de prueba: recepción, mecánico, cliente
- Feedback en 4–6 semanas
- Privacidad y hosting definidos en fase piloto

---

## Resumen

- Digitalización concreta del taller
- Equipo con stack actual de mercado
- Siguiente paso: piloto controlado

---

## Gracias

**github.com/certidevs/g3_aspnet**

Documentación: `docs/presentacion-adecco.md`

---

<!-- _class: lead -->

## Exportar estas diapositivas

1. Instala la extensión **Marp for VS Code** (o abre el archivo en **Marp CLI**).
2. Abre este archivo: `docs/diapositivas-presentacion-adecco.marp.md`.
3. Exporta a **PDF** o **PPTX** desde la paleta de comandos de Marp (`Marp: Export slide deck...`).
4. Notas del orador: usa `docs/diapositivas-presentacion-adecco.md` y `docs/guion-presentacion-adecco.md`.
