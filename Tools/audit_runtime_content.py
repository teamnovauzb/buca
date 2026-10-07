#!/usr/bin/env python3
"""Inventory player-side content builders separately from saved-prefab loading.
Run from the Unity project root. Defaults model the shipped WebGL/Input System build.
"""
import json
import re
from pathlib import Path

SYMBOLS = {'UNITY_WEBGL', 'ENABLE_INPUT_SYSTEM', 'ENABLE_LEGACY_INPUT_MANAGER'}

def condition(expression):
    expression = expression.split('//')[0].strip()
    expression = re.sub(r'\b[A-Za-z_]\w*\b', lambda m: str(m[0] in SYMBOLS), expression)
    expression = expression.replace('&&', ' and ').replace('||', ' or ').replace('!', ' not ')
    return bool(eval(expression, {'__builtins__': {}}, {}))

def runtime_lines(text):
    active = True
    stack = []
    for line_number, line in enumerate(text.splitlines(), 1):
        directive = re.match(r'\s*#(if|elif|else|endif)\b\s*(.*)', line)
        if directive:
            kind, expr = directive.groups()
            if kind == 'if':
                selected = condition(expr)
                stack.append([active, selected]); active = active and selected
            elif kind == 'elif':
                parent, taken = stack[-1]; selected = not taken and condition(expr)
                stack[-1][1] |= selected; active = parent and selected
            elif kind == 'else':
                parent, taken = stack[-1]; active = parent and not taken; stack[-1][1] = True
            else:
                active = stack.pop()[0]
            continue
        if active:
            yield line_number, line

builders = re.compile(r'\bnew\s+(GameObject|Material|Mesh|Texture2D|RenderTexture)\s*\(|\bAddComponent\s*[<(]|\bCreatePrimitive\s*\(|\bSprite\.Create\s*\(|\bScriptableObject\.CreateInstance|\bOnPopulateMesh\b|\bRenderTexture\.GetTemporary|\bCaptureScreenshotAsTexture')
loads = re.compile(r'\bInstantiate\s*[(<]')
results = {'procedural_content': [], 'saved_prefab_loading': [], 'sdk_examples': []}
for path in sorted(Path('Assets').rglob('*.cs')):
    if 'Editor' in path.parts:
        continue
    for number, line in runtime_lines(path.read_text(errors='replace')):
        code = line.split('//')[0]
        if not builders.search(code) and not loads.search(code):
            continue
        group = 'sdk_examples' if 'Example' in path.parts else 'procedural_content' if builders.search(code) else 'saved_prefab_loading'
        results[group].append({'file': str(path), 'line': number, 'code': line.strip()})
print(json.dumps(results, indent=2))
