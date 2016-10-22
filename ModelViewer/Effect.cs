using MmdFileLoader;
using SlimDX;
using SlimDX.D3DCompiler;
using System;
using System.Linq;

using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace ModelViewer {
	public class NormalEffect : IDisposable {
		private Dx11.Device device;
		private Dx11.Effect effect;
		private Dx11.InputLayout layout;
		public Dx11.Buffer Vertex { get; set; }
		private Dx11.Buffer index;
		private MmdMaterial[] materials;
		private MmdBone[] bones;

		private Texture texture;
		private Dx11.RasterizerStateDescription cullNone, cullBack;

		public NormalEffect(Dx11.Device device, string effectName, VertexData[] vertexes, int[] indicies, string parentDir, MmdMaterial[] materials, MmdBone[] bones) {
			this.device = device;
			this.materials = materials;
			this.bones = bones;
			CompileShader(effectName);
			InitializeVertexLayout();
			InitializeVertexBuffer(vertexes);
			InitializeIndexBuffer(indicies);
			texture = new Texture(device, effect, parentDir, materials);
			InitializeRasterizer();
			InitializeBlend();
		}

		private void CompileShader(string effectName) {
			using(var shaderByteCode = ShaderBytecode.CompileFromFile(effectName, "fx_5_0", ShaderFlags.None, EffectFlags.None)) {
				effect = new Dx11.Effect(device, shaderByteCode);
			}
		}

		private void InitializeVertexLayout() {
			layout = new Dx11.InputLayout(
				device, effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
				VertexData.Elements);
		}

		private void InitializeVertexBuffer(VertexData[] vertexes) {
			using(var vtxStrm = new DataStream(
				vertexes, true, true)) {
				Vertex = new Dx11.Buffer(device, vtxStrm,
					new Dx11.BufferDescription() {
						SizeInBytes = (int)vtxStrm.Length,
						BindFlags = Dx11.BindFlags.VertexBuffer,
						StructureByteStride = sizeof(float)
					});
			}
		}

		private void InitializeIndexBuffer(int[] indicies) {
			using(var idxStrm = new DataStream(indicies, true, true)) {
				index = new Dx11.Buffer(device, idxStrm, new Dx11.BufferDescription() {
					SizeInBytes = (int)idxStrm.Length,
					BindFlags = Dx11.BindFlags.IndexBuffer,
					StructureByteStride = sizeof(int)
				});
			}
		}

		private void InitializeBlend() {
			var blend = new Dx11.BlendStateDescription() {
				AlphaToCoverageEnable = false, IndependentBlendEnable = false
			};
			for(int i = 0; i < 8; i++) {
				blend.RenderTargets[i] = new Dx11.RenderTargetBlendDescription();
				blend.RenderTargets[i].BlendEnable = true;
				blend.RenderTargets[i].BlendOperation = Dx11.BlendOperation.Add;
				blend.RenderTargets[i].BlendOperationAlpha = Dx11.BlendOperation.Add;
				blend.RenderTargets[i].DestinationBlend = Dx11.BlendOption.InverseSourceAlpha;
				blend.RenderTargets[i].DestinationBlendAlpha = Dx11.BlendOption.Zero;
				blend.RenderTargets[i].RenderTargetWriteMask = Dx11.ColorWriteMaskFlags.All;
				blend.RenderTargets[i].SourceBlend = Dx11.BlendOption.SourceAlpha;
				blend.RenderTargets[i].SourceBlendAlpha = Dx11.BlendOption.One;
			}
			
			device.ImmediateContext.OutputMerger.BlendFactor = new Color4(0, 0, 0, 0);
			device.ImmediateContext.OutputMerger.BlendSampleMask = 0xffffff;
			device.ImmediateContext.OutputMerger.BlendState = Dx11.BlendState.FromDescription(device, blend);
		}

		private void InitializeRasterizer() {
			cullNone = new Dx11.RasterizerStateDescription() {
				CullMode = Dx11.CullMode.None, FillMode = Dx11.FillMode.Solid
			};
			cullBack = new Dx11.RasterizerStateDescription() {
				CullMode = Dx11.CullMode.Back, FillMode = Dx11.FillMode.Solid
			};
		}

		private void SetCull(bool IsCullBack) {
			if(IsCullBack) device.ImmediateContext.Rasterizer.State = Dx11.RasterizerState.FromDescription(device, cullBack);
			else device.ImmediateContext.Rasterizer.State = Dx11.RasterizerState.FromDescription(device, cullNone);
		}

		private void SetMaterial(int nowCount) {
			texture.SetTexture(nowCount);
			SetLight(nowCount);
			SetCull(!(materials[nowCount].DrawFlag.HasFlag(DrawFlagEnumes.DrawBoth) || materials[nowCount].Alpha == 0.999f));
		}

		private void SetLight(int nowCount) {
			effect.GetVariableByName("matDiffuse").AsVector().Set(new Color4(materials[nowCount].Diffuse));
			effect.GetVariableByName("matAmbient").AsVector().Set(new Color4(materials[nowCount].Ambient));
			effect.GetVariableByName("matSpecular").AsVector().Set(new Color4(materials[nowCount].Specular));
			effect.GetVariableByName("matAlpha").AsScalar().Set(materials[nowCount].Alpha);
			effect.GetVariableByName("matSpecularity").AsScalar().Set(materials[nowCount].Specularity);
		}

		private void InitializeInputAssembler() {
			device.ImmediateContext.InputAssembler.InputLayout = layout;
			device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new Dx11.VertexBufferBinding(Vertex, VertexData.Size, 0));
			device.ImmediateContext.InputAssembler.SetIndexBuffer(index, Dxgi.Format.R32_UInt, 0);
			device.ImmediateContext.InputAssembler.PrimitiveTopology = Dx11.PrimitiveTopology.TriangleList;
		}

		public void SetBoneMatrix(Matrix[] bones) {
			effect.GetVariableByName("BoneMatrix").AsMatrix().SetMatrixArray(bones);
		}

		public void DrawAll(Camera camera) {
			InitializeInputAssembler();
			SetCamera(camera);

			int startIdx = 0;
			for(int i = 0; i < materials.Length; i++) {
				SetMaterial(i);
				effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
				device.ImmediateContext.DrawIndexed(materials[i].IndiciesCount, startIdx, 0);
				startIdx += materials[i].IndiciesCount;
			}
		}

		private void SetCamera(Camera camera) {
			effect.GetVariableByName("World").AsMatrix().SetMatrix(camera.World);
			effect.GetVariableByName("View").AsMatrix().SetMatrix(camera.View);
			effect.GetVariableByName("Projection").AsMatrix().SetMatrix(camera.Projection);
			effect.GetVariableByName("lightDir").AsVector().Set(camera.LightDir);
			effect.GetVariableByName("ambientLight").AsVector().Set(camera.AmbientLight);
			effect.GetVariableByName("eyePos").AsVector().Set(camera.EyePosition);
		}

		public void Dispose() {
			SetCull(true);
			device.ImmediateContext.Rasterizer.State?.Dispose();
			SetCull(false);
			device.ImmediateContext.Rasterizer.State?.Dispose();
			texture?.Dispose();
			index?.Dispose();
			Vertex?.Dispose();
			layout?.Dispose();
			effect?.Dispose();
		}
	}

	public struct VertexData {
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 Uv;
		public IntegerArr Index;
		public Vector4 Weight;

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
				},
				new Dx11.InputElement() {
					SemanticName = "BLENDINDICES", Format = Dxgi.Format.R32G32B32A32_SInt,
					AlignedByteOffset = Dx11.InputElement.AppendAligned,
				},
				new Dx11.InputElement() {
					SemanticName = "BLENDWEIGHT", Format = Dxgi.Format.R32G32B32A32_Float,
					AlignedByteOffset = Dx11.InputElement.AppendAligned,
				}
			};

		public static readonly int Size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexData));
	}

	public struct IntegerArr {
		public int idx1;
		public int idx2;
		public int idx3;
		public int idx4;
	}
}
