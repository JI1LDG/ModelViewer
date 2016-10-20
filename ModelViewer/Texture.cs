using MmdFileLoader;
using System;

using Dx11 = SlimDX.Direct3D11;

namespace ModelViewer {
	public class Texture : IDisposable {
		Dx11.Device device;
		Dx11.Effect effect;
		MmdMaterial[] materials;
		string parentDir;

		Dx11.ShaderResourceView[] normalTex;
		Dx11.ShaderResourceView[] sphTex;
		Dx11.ShaderResourceView[] spaTex;
		Dx11.ShaderResourceView[] toonTex;

		public Texture(Dx11.Device device, Dx11.Effect effect, string parentDir, MmdMaterial[] materials) {
			this.device = device;
			this.effect = effect;
			this.materials = materials;
			this.parentDir = parentDir;
			normalTex = new Dx11.ShaderResourceView[materials.Length];
			sphTex = new Dx11.ShaderResourceView[materials.Length];
			spaTex = new Dx11.ShaderResourceView[materials.Length];
			toonTex = new Dx11.ShaderResourceView[materials.Length];

			InitializeTexture();
		}

		public void InitializeTexture() {
			for(int i = 0; i < materials.Length; i++) {
				try {
					if(materials[i].NormalTexture != null) {
						normalTex[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + materials[i].NormalTexture);
					}
				} catch(Exception e) {
					Console.WriteLine(e.Message + " Normal: " + materials[i].NormalTexture);
				}

				try {
					if(materials[i].AddSphereTexture != null) {
						spaTex[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + materials[i].AddSphereTexture);
					} else if(materials[i].MultiplySphereTexture != null) {
						sphTex[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + materials[i].MultiplySphereTexture);
					}
				} catch(Exception e) {
					Console.WriteLine(e.Message + " Sphere #" + i);
				}

				try {
					if(materials[i].ToonTexture != null) {
						if(materials[i].ToonTexture.Contains(@"toon\")) {
							if(materials[i].ToonTexture.Contains("00")) {
								toonTex[i] = Dx11.ShaderResourceView.FromFile(device, materials[i].ToonTexture.Replace("00", "0"));
							} else {
								toonTex[i] = Dx11.ShaderResourceView.FromFile(device, materials[i].ToonTexture);
							}
						} else {
							toonTex[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + materials[i].ToonTexture);
						}
					}
				} catch(Dx11.Direct3D11Exception e) {
					Console.WriteLine(e.Message + " Toon: " + materials[i].ToonTexture);
				}
			}
		}

		public void SetTexture(int num) {
			SetTexture("tex", "normal", normalTex[num]);
			SetTexture("sph", "sph", sphTex[num]);
			SetTexture("spa", "spa", spaTex[num]);
			SetTexture("ton", "toon", toonTex[num]);
		}

		private void SetBoolean(string name, bool onoff) {
			effect.GetVariableByName(name).AsScalar().Set(onoff);
		}

		private void SetTexture(string name, Dx11.ShaderResourceView texture) {
			effect.GetVariableByName(name + "Texture").AsResource().SetResource(texture);
		}

		private void SetTexture(string boolName, string texName, Dx11.ShaderResourceView data) {
			if(data == null) {
				SetBoolean(boolName, false);
			} else {
				SetBoolean(boolName, true);
				SetTexture(texName, data);
			}
		}

		public void Dispose() {
			foreach(var tt in toonTex) tt?.Dispose();
			foreach(var at in spaTex) at?.Dispose();
			foreach(var ht in sphTex) ht?.Dispose();
			foreach(var nt in normalTex) nt?.Dispose();
		}
	}
}
