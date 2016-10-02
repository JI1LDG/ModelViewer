using MmdFileLoader;
using SlimDX;
using SlimDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using Rwin = SlimDX.RawInput;

namespace ModelViewer {
	class DrawMmdModel : Core {
		Dx11.Effect effect;
		Dx11.InputLayout vertexLayout;
		Dx11.Buffer vertexBuffer, indexBuffer;
		Dx11.ShaderResourceView[] texture, toons, sphs, spas;
		MmdLoader mmdLoader;
		int flameCount;
		string parentDir;
		MovingData movingNow;

		public DrawMmdModel(string Path) {
			mmdLoader = new MmdLoader(Path);
			parentDir = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory + "\\" + Path) + "\\";
			flameCount = 0;
			movingNow = new MovingData();
		}

		protected override void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));
			device.ImmediateContext.ClearDepthStencilView(depthStencil, Dx11.DepthStencilClearFlags.Depth, 1, 0);

			UpdateCamera();
			InitializeInputAssembler();
			DrawModel();

			swapChain.Present(0, Dxgi.PresentFlags.None);

			flameCount++;
		}

		private void UpdateCamera() {
			var world = Matrix.Identity;

			float div = 10;
			var viewEye = new Vector3(0 , 10, -45);
			var viewTarget = new Vector3(0, 10, 0);
			var view = Matrix.Multiply(
				Matrix.Multiply(
					Matrix.RotationX(-movingNow.rotY / 100.0f), 
					Matrix.RotationY(movingNow.rotX / 100.0f)
				), Matrix.Multiply(
					Matrix.LookAtRH(viewEye, viewTarget, new Vector3(0, 1, 0)),
					Matrix.Translation(movingNow.posX / div, -movingNow.posY / div, movingNow.posZ * 0.5f)
				)
			);

			var projection = Matrix.PerspectiveFovRH(
				30 * (float)Math.PI / 180, ClientSize.Width / ClientSize.Height, 0.1f, 1000
			);

			effect.GetVariableByName("World").AsMatrix().SetMatrix(world);
			effect.GetVariableByName("View").AsMatrix().SetMatrix(view);
			effect.GetVariableByName("Projection").AsMatrix().SetMatrix(projection);
			effect.GetVariableByName("lightDir").AsVector().Set(new Vector3(0, 0, 0.5f));
			effect.GetVariableByName("ambientLight").AsVector().Set(new Vector3(1, 1, 1));
			effect.GetVariableByName("eyePos").AsVector().Set(new Vector3(view.M41, view.M42, view.M43));
		}

		private void InitializeInputAssembler() {
			device.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
			device.ImmediateContext.InputAssembler.SetVertexBuffers(
				0, new Dx11.VertexBufferBinding(vertexBuffer, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData)), 0));
			device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Dxgi.Format.R32_UInt, 0);
			device.ImmediateContext.InputAssembler.PrimitiveTopology = Dx11.PrimitiveTopology.TriangleList;
		}

		private void DrawModel() {
			int startIdx = 0;
			for(int i = 0;i < mmdLoader.Material.Length;i++) {
				if(texture[i] == null) {
					effect.GetVariableByName("tex").AsScalar().Set(false);
				} else {
					effect.GetVariableByName("tex").AsScalar().Set(true);
					effect.GetVariableByName("normalTexture").AsResource().SetResource(texture[i]);
				}

				if(sphs[i] == null) {
					effect.GetVariableByName("sph").AsScalar().Set(false);
				} else {
					effect.GetVariableByName("sph").AsScalar().Set(true);
					effect.GetVariableByName("sphTexture").AsResource().SetResource(sphs[i]);
				}

				if(spas[i] == null) {
					effect.GetVariableByName("spa").AsScalar().Set(false);
				} else {
					effect.GetVariableByName("spa").AsScalar().Set(true);
					effect.GetVariableByName("spaTexture").AsResource().SetResource(spas[i]);
				}

				if(toons[i] == null) {
					effect.GetVariableByName("ton").AsScalar().Set(false);
				} else {
					effect.GetVariableByName("ton").AsScalar().Set(true);
					effect.GetVariableByName("toonTexture").AsResource().SetResource(toons[i]);
				}

				effect.GetVariableByName("matDiffuse").AsVector().Set(new Color4(mmdLoader.Material[i].Diffuse));
				effect.GetVariableByName("matAmbient").AsVector().Set(new Color4(mmdLoader.Material[i].Ambient));
				effect.GetVariableByName("matSpecular").AsVector().Set(new Color4(mmdLoader.Material[i].Specular));
				effect.GetVariableByName("matAlpha").AsScalar().Set(mmdLoader.Material[i].Alpha);
				effect.GetVariableByName("matSpecularity").AsScalar().Set(mmdLoader.Material[i].Specularity);
				effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
				device.ImmediateContext.DrawIndexed(mmdLoader.Material[i].IndiciesCount, startIdx, 0);

				startIdx += mmdLoader.Material[i].IndiciesCount;
			}
		}

		protected override void LoadContent() {
			InitializeEffect();
			InitializeVertexLayout();
			InitializeVertexBuffer();
			InitializeIndexBuffer();
			InitializeTexture();
			InitializeRasterizerState();
		}

		private void InitializeEffect() {
			using(var shaderByteCode = ShaderBytecode.CompileFromFile("effect.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None)) {
				effect = new Dx11.Effect(device, shaderByteCode);
			}
		}

		private void InitializeVertexLayout() {
			vertexLayout = new Dx11.InputLayout(
				device, effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
				VertexData.Elements);
		}

		private void InitializeVertexBuffer() {
			using(var vertexStream = new DataStream(
				mmdLoader.Vertex.Select(x => new VertexData() {
					Position = x.Position, Normal = x.Normal, Uv = x.Uv
				}).ToArray(), true, true)) {
				vertexBuffer = new Dx11.Buffer(device, vertexStream,
					new Dx11.BufferDescription() {
						SizeInBytes = (int)vertexStream.Length,
						BindFlags = Dx11.BindFlags.VertexBuffer,
						StructureByteStride = sizeof(float),
					}
				);
			}
		}

		private void InitializeIndexBuffer() {
			using(var indexStream = new DataStream(mmdLoader.Index, true, true)) {
				indexBuffer = new Dx11.Buffer(device, indexStream, new Dx11.BufferDescription() {
					SizeInBytes = (int)indexStream.Length,
					BindFlags = Dx11.BindFlags.IndexBuffer,
					StructureByteStride = sizeof(int)
				});
			}
		}

		private void InitializeTexture() {
			texture = new Dx11.ShaderResourceView[mmdLoader.Material.Length];
			for(int i = 0;i < texture.Length; i++) {
				try {
					if(mmdLoader.Material[i].NormalTexture != null) {
						texture[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + mmdLoader.Material[i].NormalTexture);
					}
				} catch(Exception e) {
					Console.WriteLine(e.Message + " Normal: " + mmdLoader.Material[i].NormalTexture);
					continue;
				}
			}

			sphs = new Dx11.ShaderResourceView[mmdLoader.Material.Length];
			spas = new Dx11.ShaderResourceView[mmdLoader.Material.Length];
			for(int i = 0;i < sphs.Length; i++) {
				try {
					if(mmdLoader.Material[i].AddSphereTexture != null) {
						spas[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + mmdLoader.Material[i].AddSphereTexture);
					} else if(mmdLoader.Material[i].MultiplySphereTexture != null) {
						sphs[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + mmdLoader.Material[i].MultiplySphereTexture);
					}
				} catch(Exception e) {
					Console.WriteLine(e.Message + " Sphere #" + i);
					continue;
				}
			}

			toons = new Dx11.ShaderResourceView[mmdLoader.Material.Length];
			for(int i = 0;i < toons.Length; i++) {
				try {
					if(mmdLoader.Material[i].ToonTexture != null) {
						if(mmdLoader.Material[i].ToonTexture.Contains(@"toon\")) {
							toons[i] = Dx11.ShaderResourceView.FromFile(device, mmdLoader.Material[i].ToonTexture);
						} else {
							toons[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + mmdLoader.Material[i].ToonTexture);
						}
					}
				} catch(Dx11.Direct3D11Exception e) {
					Console.WriteLine(e.Message + " Toon: " + mmdLoader.Material[i].ToonTexture);
					continue;
				}
			}
		}

		private void InitializeRasterizerState() {
			device.ImmediateContext.Rasterizer.State = Dx11.RasterizerState.FromDescription(device,
				new Dx11.RasterizerStateDescription() {
					CullMode = Dx11.CullMode.None, FillMode = Dx11.FillMode.Solid,
					IsDepthClipEnabled = false, IsMultisampleEnabled = false,
					DepthBiasClamp = 0, SlopeScaledDepthBias = 0
				}
			);
		}

		protected override void UnloadContent() {
			device.ImmediateContext.Rasterizer.State.Dispose();
			foreach(var t in texture) t?.Dispose();
			foreach(var t in toons) t?.Dispose();
			foreach(var t in sphs) t?.Dispose();
			foreach(var t in spas) t?.Dispose();
			effect.Dispose();
			vertexLayout.Dispose();
			vertexBuffer.Dispose();
			indexBuffer.Dispose();
		}

		protected override void MouseInput(object sender, Rwin.MouseInputEventArgs e) {
			switch(e.ButtonFlags) {
				case Rwin.MouseButtonFlags.MiddleDown:
					movingNow.isMiddleMoving = true;
					break;
				case Rwin.MouseButtonFlags.MiddleUp:
					movingNow.isMiddleMoving = false;
					break;
				case Rwin.MouseButtonFlags.RightDown:
					movingNow.isRightMoving = true;
					break;
				case Rwin.MouseButtonFlags.RightUp:
					movingNow.isRightMoving = false;
					break;
			}

			if(movingNow.isMiddleMoving) {
				movingNow.posX += e.X;
				movingNow.posY += e.Y;
			}
			if(e.WheelDelta > 0) {
				movingNow.posZ++;
			} else if(e.WheelDelta < 0) {
				movingNow.posZ--;
			}
			if(movingNow.isRightMoving) {
				movingNow.rotX += e.X;
				movingNow.rotY += e.Y;
			}
		}

		protected override void KeyInput(object sender, Rwin.KeyboardInputEventArgs e) {
		}
	}

	struct VertexData {
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 Uv;

		public static readonly Dx11.InputElement[] Elements =
			new[] {
				new Dx11.InputElement() {
					SemanticName = "SV_Position", Format = Dxgi.Format.R32G32B32_Float,
				},
				new Dx11.InputElement() {
					SemanticName = "NORMAL", Format = Dxgi.Format.R32G32B32_Float,
					AlignedByteOffset = Dx11.InputElement.AppendAligned
				},
				new Dx11.InputElement() {
					SemanticName = "TEXCOORD", Format = Dxgi.Format.R32G32_Float,
					AlignedByteOffset = Dx11.InputElement.AppendAligned
			}
		};
	}

	struct MovingData {
		public int posX;
		public int posY;
		public int posZ;
		public int rotX;
		public int rotY;
		public bool isMiddleMoving;
		public bool isRightMoving;
		public void ResetPosXY() { posX = posY = 0; }
		public void ResetPosZ() { posZ = 0; }
		public void ResetRotXY() { rotX = rotY = 0; }
		public void ResetAll() { ResetPosXY(); ResetPosZ(); ResetRotXY(); }
	}
}
