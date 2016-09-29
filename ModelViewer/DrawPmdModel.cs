﻿using MmdFileLoader.Pmd;
using SlimDX;
using SlimDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace ModelViewer {
	class DrawPmdModel : Core {
		Dx11.Effect effect;
		Dx11.InputLayout vertexLayout;
		Dx11.Buffer vertexBuffer;
		Dx11.Buffer indexBuffer;
		Dx11.ShaderResourceView[] texture;
		PmdLoader pmdLoader;
		int flameCount;
		string parentDir;

		public DrawPmdModel(string Path) {
			pmdLoader = new PmdLoader(Path);
			parentDir = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory + "\\" + Path) + "\\";
			flameCount = 0;
		}

		protected override void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));
			device.ImmediateContext.ClearDepthStencilView(depthStencil, Dx11.DepthStencilClearFlags.Depth, 1, 0);

			UpdateCamera();
			InitializeInputAsselbler();
			DrawModel();

			swapChain.Present(0, Dxgi.PresentFlags.None);

			flameCount++;
		}

		private void UpdateCamera() {
			var world = Matrix.RotationY(flameCount / 20 * (float)Math.PI / 180);

			var view = Matrix.LookAtRH(
				new Vector3(0, 10, -10), new Vector3(0, 10, 0), new Vector3(0, 1, 0)
			);

			var projection = Matrix.PerspectiveFovRH(
				(float)Math.PI / 2, ClientSize.Width / ClientSize.Height, 0.1f, 1000
			);

			effect.GetVariableByName("World").AsMatrix().SetMatrix(world);
			effect.GetVariableByName("View").AsMatrix().SetMatrix(view);
			effect.GetVariableByName("Projection").AsMatrix().SetMatrix(projection);
		}

		private void InitializeInputAsselbler() {
			device.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
			device.ImmediateContext.InputAssembler.SetVertexBuffers(
				0, new Dx11.VertexBufferBinding(vertexBuffer, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData)), 0));
			device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Dxgi.Format.R16_UInt, 0);
			device.ImmediateContext.InputAssembler.PrimitiveTopology = Dx11.PrimitiveTopology.TriangleList;
		}

		private void DrawModel() {
			int startIdx = 0;
			for(int i = 0;i < pmdLoader.Material.Length;i++) {
				if(texture[i] == null) {
					effect.GetVariableByName("tex").AsScalar().Set(false);
				} else {
					effect.GetVariableByName("tex").AsScalar().Set(true);
					effect.GetVariableByName("normalTexture").AsResource().SetResource(texture[i]);
				}
				effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
				device.ImmediateContext.DrawIndexed(pmdLoader.Material[i].IndiciesCount, startIdx, 0);
				startIdx += pmdLoader.Material[i].IndiciesCount;
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
				new[] {
					new Dx11.InputElement() {
						SemanticName = "SV_Position", Format = Dxgi.Format.R32G32B32_Float,
					},
					new Dx11.InputElement() {
						SemanticName = "TEXCOORD", Format = Dxgi.Format.R32G32_Float,
						AlignedByteOffset = Dx11.InputElement.AppendAligned
					}
				}
			);
		}

		private void InitializeVertexBuffer() {
			using(var vertexStream = new DataStream(
				pmdLoader.Vertex.Select(x => new VertexData() {
					Position = x.Position, Uv = x.Uv
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
			using(var indexStream = new DataStream(pmdLoader.Index.SelectMany(x => x.Indicies).ToArray(), true, true)) {
				indexBuffer = new Dx11.Buffer(device, indexStream, new Dx11.BufferDescription() {
					SizeInBytes = (int)indexStream.Length,
					BindFlags = Dx11.BindFlags.IndexBuffer,
					StructureByteStride = sizeof(short)
				});
			}
		}

		private void InitializeTexture() {
			texture = new Dx11.ShaderResourceView[pmdLoader.Material.Length];
			for(int i = 0;i < texture.Length; i++) {
				try {
					texture[i] = Dx11.ShaderResourceView.FromFile(device, parentDir + pmdLoader.Material[i].TextureFileName, 
						new Dx11.ImageLoadInformation() {
							Format = Dxgi.Format.R32G32B32A32_Float,
							FilterFlags = Dx11.FilterFlags.Triangle,
							MipFilterFlags = Dx11.FilterFlags.Triangle,
							Usage = Dx11.ResourceUsage.Default,
							BindFlags = Dx11.BindFlags.ShaderResource,
							MipLevels = -1
						}
					);
				} catch(Dx11.Direct3D11Exception e) {
					Console.WriteLine("Texture of Material" + i + " not found");
					continue;
				}
			}
		}

		private void InitializeRasterizerState() {
			device.ImmediateContext.Rasterizer.State = Dx11.RasterizerState.FromDescription(device,
				new Dx11.RasterizerStateDescription() {
					CullMode = Dx11.CullMode.None, FillMode = Dx11.FillMode.Solid,
				}
			);
		}

		protected override void UnloadContent() {
			device.ImmediateContext.Rasterizer.State.Dispose();
			foreach(var t in texture) t?.Dispose();
			effect.Dispose();
			vertexLayout.Dispose();
			vertexBuffer.Dispose();
			indexBuffer.Dispose();
		}
	}

	struct VertexData {
		public Vector3 Position;
		public Vector2 Uv;
	}
}
