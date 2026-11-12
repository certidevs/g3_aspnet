#!/usr/bin/env python3
"""Genera las páginas del wiki de GitHub a partir de la documentación del repositorio.

Uso: build-wiki.py <raiz_repo> <carpeta_salida>

Lo ejecuta el workflow .github/workflows/sync-wiki.yml.
Para publicar un documento nuevo, añádelo al diccionario PAGES.
"""
import os
import re
import sys

REPO_URL = "https://github.com/" + os.environ.get("GITHUB_REPOSITORY", "certidevs/g3_aspnet")
BRANCH = "main"

# fuente en el repositorio -> nombre de página del wiki
PAGES = {
    "README.md": "Home",
    "docs/guia-funcional.md": "Guía funcional",
    "docs/diagrama-er.md": "Modelo de datos",
    "docs/demo.md": "Demostración",
    "docs/problemas-frecuentes.md": "Problemas frecuentes",
    "CONTRIBUTING.md": "Guía de contribución",
    "docs/IDE-SETUP.md": "Guía de IDE",
    "docs/DEPENDENCIAS.md": "Dependencias",
}

LINK = re.compile(r"(\]\()([^)\s]+)(\))")


def wiki_slug(name: str) -> str:
    return name.replace(" ", "-")


def convert_target(target: str, src: str, root: str) -> str:
    if re.match(r"^(https?:|mailto:|#)", target):
        return target
    path, _, anchor = target.partition("#")
    resolved = os.path.normpath(os.path.join(os.path.dirname(src), path))
    suffix = f"#{anchor}" if anchor else ""
    if resolved in PAGES:
        return wiki_slug(PAGES[resolved]) + suffix
    kind = "tree" if os.path.isdir(os.path.join(root, resolved)) else "blob"
    return f"{REPO_URL}/{kind}/{BRANCH}/{resolved}{suffix}"


def convert(text: str, src: str, root: str) -> str:
    out, in_code = [], False
    for line in text.splitlines():
        if line.lstrip().startswith("```"):
            in_code = not in_code
        if not in_code:
            line = LINK.sub(
                lambda m: m.group(1) + convert_target(m.group(2), src, root) + m.group(3),
                line,
            )
        out.append(line)
    return "\n".join(out) + "\n"


def main() -> None:
    root, dest = sys.argv[1], sys.argv[2]
    os.makedirs(dest, exist_ok=True)
    for src, name in PAGES.items():
        with open(os.path.join(root, src), encoding="utf-8") as f:
            body = convert(f.read(), src, root)
        body += (
            "\n---\n\n"
            f"Esta página se genera desde [`{src}`]({REPO_URL}/blob/{BRANCH}/{src}). "
            "Los cambios hechos aquí se sobrescriben: edita el archivo del repositorio.\n"
        )
        with open(os.path.join(dest, wiki_slug(name) + ".md"), "w", encoding="utf-8") as f:
            f.write(body)

    sidebar = ["**TALLER .NET**", "", "- [Inicio](Home)"]
    for src, name in PAGES.items():
        if name != "Home":
            sidebar.append(f"- [{name}]({wiki_slug(name)})")
    sidebar += ["", f"[Repositorio]({REPO_URL})"]
    with open(os.path.join(dest, "_Sidebar.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(sidebar) + "\n")


if __name__ == "__main__":
    main()
