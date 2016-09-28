using MmdFileLoader.Pmd;
using SlimDX;
using SlimDX.D3DCompiler;
using System;
using System.Linq;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace ModelViewer {
	class DrawPmdModel : Core {
		Dx11.Effect effect;
		Dx11.InputLayout vertexLayout;
		Dx11.Buffer vertexBuffer;
		PmdLoader pmdLoader;
		int flameCount;

		public DrawPmdModel(string Path) {
			pmdLoader = new PmdLoader(Path);
			flameCount = 0;
		}

		protected override void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));

			UpdateCamera();
			InitializeInputAsselbler();
			DrawTriangle();

			swapChain.Present(0, Dxgi.PresentFlags.None);

			flameCount++;
		}

		private void UpdateCamera() {
			var world = Matrix.RotationY((flameCount / 20 % 360) * (float)Math.PI / 180);

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
				0, new Dx11.VertexBufferBinding(vertexBuffer, sizeof(float) * 3, 0));
			device.ImmediateContext.InputAssembler.PrimitiveTopology = Dx11.PrimitiveTopology.TriangleList;
		}

		private void DrawTriangle() {
			effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
			device.ImmediateContext.Draw(pmdLoader.Index.Length * 3, 0);
		}

		protected override void LoadContent() {
			InitializeEffect();
			InitializeVertexLayout();
			InitializeVertexBuffer();
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
						SemanticName = "SV_Position", Format = Dxgi.Format.R32G32B32_Float
					}
				}
			);
		}

		private void InitializeVertexBuffer() {
			using(var vertexStream = new DataStream(
				pmdLoader.Index.SelectMany(x => x.Indicies).Select(x => pmdLoader.Vertex[x].Position).ToArray(), true, true)) {
				vertexBuffer = new Dx11.Buffer(device, vertexStream,
					new Dx11.BufferDescription() {
						SizeInBytes = (int)vertexStream.Length, BindFlags = Dx11.BindFlags.VertexBuffer
					}
				);
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
			effect.Dispose();
			vertexLayout.Dispose();
			vertexBuffer.Dispose();
		}
	}
}
