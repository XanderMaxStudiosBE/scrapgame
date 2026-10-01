#!/usr/bin/env python3
"""Direct texture references and future material authoring regression; not Unity import/shader execution."""
from pathlib import Path
import re,sys,unittest,subprocess,tempfile,shutil
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'ArtSource'))
from material_compatibility import migration_safe
ASSETS=ROOT/'Assets/Scrapshift'

def texture(source,property):
    match=re.search(r'    - '+re.escape(property)+r':\n((?:        .+\n){3})',source)
    if not match:raise ValueError('Missing texture property '+property)
    return match[1]

def refs(source,index):
    entries=re.split(r'  - resource: ',source)[1:]
    if len(entries)!=12:raise ValueError('Expected twelve original materials')
    seen=set()
    for block in entries:
        resource=block.splitlines()[0]
        if resource in seen:raise ValueError('Duplicate material resource')
        seen.add(resource)
        fields={prop:guid for prop,guid in re.findall(r'^    (\w+): \{fileID: \d+, guid: (\w+), type: \d+\}',block,re.M)}
        if not {'material','albedo'}<=fields.keys():raise ValueError('Incomplete binding '+resource)
        material=index.get(fields['material']);albedo=index.get(fields['albedo'])
        expected=ASSETS/'Resources'/(resource+'.mat')
        if material!=expected or albedo is None or albedo.suffix!='.png':raise ValueError('Unresolved binding '+resource)
        if resource.endswith('WorldProps') and not {'metallicGloss','emission'}<=fields.keys():raise ValueError('Missing mask/glow')
        for prop in ['metallicGloss','emission']:
            if prop in fields and (fields[prop] not in index or index[fields[prop]].suffix!='.png'):raise ValueError('Invalid mask/glow')
        straight=re.search(r'    straightAlpha: (\d)',block)
        if not straight or int(straight[1])!=int(resource.endswith('/GroundWear') or resource.endswith('/RoughPuddles')):raise ValueError('Wrong alpha policy')
    return seen

class MaterialAuthoring(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.index={re.search(r'^guid: (\w+)$',p.read_text(),re.M)[1]:Path(str(p)[:-5]) for p in ASSETS.rglob('*.meta')}
        cls.catalog=(ASSETS/'Resources/ScrapshiftRendering/Materials.asset').read_text()
        cls.prop=(ASSETS/'Resources/ScrapshiftMaterials/PropAtlas.mat').read_text()
    def test_catalog_is_independent_of_material_saved_texture_fields(self):
        self.assertEqual(12,len(refs(self.catalog,self.index)))
        # Changing a source material's serialized BaseMap is irrelevant to catalog resolution.
        changed=re.sub(r'guid: \w+', 'guid: '+'0'*32,texture(self.prop,'_BaseMap'))
        self.assertNotEqual(changed,texture(self.prop,'_BaseMap'))
        self.assertEqual(12,len(refs(self.catalog,self.index)))
    def test_catalog_detects_broken_albedo_reference(self):
        corrupt=re.sub(r'(    albedo: \{fileID: \d+, guid: )\w+',lambda m:m[1]+'0'*32,self.catalog,count=1)
        with self.assertRaises(ValueError):refs(corrupt,self.index)
    def test_aliases_keep_texture_uv_color_and_smoothness(self):
        source=self.prop.replace('_Smoothness: 0.08','_Smoothness: 0.21').replace('m_Scale: {x: 1, y: 1}','m_Scale: {x: 2, y: 3}').replace('_BaseColor: {r: 1, g: 1, b: 1, a: 1}','_BaseColor: {r: 0.4, g: 0.5, b: 0.6, a: 0.7}')
        safe=migration_safe(source)
        self.assertEqual(texture(source,'_BaseMap'),texture(safe,'_MainTex'))
        self.assertIn('_Color: {r: 0.4, g: 0.5, b: 0.6, a: 0.7}',safe)
        self.assertIn('_Glossiness: 0.21',safe);self.assertIn('_GlossMapScale: 0.21',safe);self.assertIn('_GlossyReflections: 1',safe)
        self.assertEqual(safe,migration_safe(safe))
    def test_ground_aliases_preserve_straight_alpha_fade(self):
        for name in ['GroundWear','RoughPuddles']:
            safe=migration_safe((ASSETS/('Resources/ScrapshiftWorld/'+name+'.mat')).read_text())
            for state in ['_BlendModePreserveSpecular: 0','_SrcBlendAlpha: 1','_DstBlendAlpha: 10','_ZWrite: 0']:
                self.assertIn(state,safe)
    def test_missing_source_fields_fail_authoring(self):
        with self.assertRaises(ValueError):migration_safe(self.prop.replace('_BaseMap:','_Unknown:'))
    def test_first_time_world_pack_serializes_upgrader_aliases(self):
        # Run the real authoring script in a fresh miniature checkout; production .mat files are not touched.
        with tempfile.TemporaryDirectory(prefix='scrapshift-materials-') as directory:
            root=Path(directory);(root/'ArtSource').mkdir();folder=root/'Assets/Scrapshift/Resources/ScrapshiftMaterials';folder.mkdir(parents=True)
            shutil.copy(ASSETS/'Resources/ScrapshiftMaterials/PropAtlas.mat',folder/'PropAtlas.mat')
            for name in ['build_world_surfaces.py','material_compatibility.py']:shutil.copy(ROOT/'ArtSource'/name,root/'ArtSource'/name)
            subprocess.run([sys.executable,str(root/'ArtSource/build_world_surfaces.py')],check=True,stdout=subprocess.DEVNULL)
            for name in ['WorldProps','GroundWear','RoughPuddles','WireFence']:
                source=(root/('Assets/Scrapshift/Resources/ScrapshiftWorld/'+name+'.mat')).read_text()
                self.assertEqual(texture(source,'_BaseMap'),texture(source,'_MainTex'))
                self.assertEqual(source,migration_safe(source))
                self.assertNotIn('AssetVersion',source,'Do not stamp package-private upgrade metadata')

if __name__=='__main__':unittest.main()
