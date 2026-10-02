"""Serialize public legacy aliases consumed by URP's material upgrader.
This does not stamp Unity's private AssetVersion data or perform an engine upgrade.
"""
import re

def migration_safe(source):
    base=re.search(r'    - _BaseMap:\n(?:        .+\n){3}',source)
    color=re.search(r'    - _BaseColor: (.+)',source)
    smooth=re.search(r'    - _Smoothness: ([\d.]+)',source)
    if not all((base,color,smooth)):raise ValueError('URP material lacks albedo/color/smoothness')
    alias=base[0].replace('_BaseMap:','_MainTex:')
    if '    - _MainTex:' in source:
        source=re.sub(r'    - _MainTex:\n(?:        .+\n){3}',lambda m:alias,source,count=1)
    else:source=source.replace('    m_Ints:',alias+'    m_Ints:',1)
    if '    - _Color:' in source:
        source=re.sub(r'^    - _Color: .+$',lambda m:'    - _Color: '+color[1],source,count=1,flags=re.M)
    else:source=source.replace('    m_Colors:\n','    m_Colors:\n    - _Color: '+color[1]+'\n',1)
    env=re.search(r'    - _EnvironmentReflections: ([\d.]+)',source)
    for prop,value in [('_Glossiness',smooth[1]),('_GlossMapScale',smooth[1]),('_GlossyReflections',env[1] if env else '1')]:
        if '    - '+prop+':' in source:
            source=re.sub(r'^    - '+re.escape(prop)+r': .+$',lambda m:'    - '+prop+': '+value,source,count=1,flags=re.M)
        else:source=source.replace('    m_Floats:\n','    m_Floats:\n    - '+prop+': '+value+'\n',1)
    if '    - _Surface: 1' in source and '    - _Blend: 0' in source:
        for prop,value in [('_BlendModePreserveSpecular','0'),('_SrcBlendAlpha','1'),('_DstBlendAlpha','10')]:
            if '    - '+prop+':' not in source:source=source.replace('    m_Floats:\n','    m_Floats:\n    - '+prop+': '+value+'\n',1)
    return source
