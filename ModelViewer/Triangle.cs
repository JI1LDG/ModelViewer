using SlimDX;
using SlimDX.D3DCompiler;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace ModelViewer {
	class Triangle : Core {
		Dx11.Effect effect;
		Dx11.InputLayout vertexLayout;
		Dx11.Buffer vertexBuffer;

		protected override void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));

			InitializeInputAsselbler();
			DrawTriangle();

			swapChain.Present(0, Dxgi.PresentFlags.None);
		}

		private void InitializeInputAsselbler() {
			device.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
			device.ImmediateContext.InputAssembler.SetVertexBuffers(
				0, new Dx11.VertexBufferBinding(vertexBuffer, sizeof(float) * 3, 0));
			device.ImmediateContext.InputAssembler.PrimitiveTopology = Dx11.PrimitiveTopology.TriangleList;
		}

		private void DrawTriangle() {
			effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
			device.ImmediateContext.Draw(3, 0);
		}

		protected override void LoadContent() {
			InitializeEffect();
			InitializeVertexLayout();
			InitializeVertexBuffer();
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
				new[] {
					new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 0, 0)
				}, true, true)) {
				vertexBuffer = new Dx11.Buffer(device, vertexStream,
					new Dx11.BufferDescription() {
						SizeInBytes = (int)vertexStream.Length, BindFlags = Dx11.BindFlags.VertexBuffer
					}
				);
			}
		}

		protected override void UnloadContent() {
			effect.Dispose();
			vertexLayout.Dispose();
			vertexBuffer.Dispose();
		}
	}
}
