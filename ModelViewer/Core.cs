using SlimDX;
using SlimDX.Windows;
using System.Windows.Forms;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace ModelViewer {
	class Core : Form {
		protected Dx11.Device device;
		protected Dxgi.SwapChain swapChain;
		protected Dx11.RenderTargetView renderTarget;

		protected virtual void LoadContent() { }
		protected virtual void UnloadContent() { }

		public void Run() {
			InitializeDevice();
			MessagePump.Run(this, Draw);
			DisposeDevice();
		}

		protected virtual void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));

			swapChain.Present(0, Dxgi.PresentFlags.None);
		}

		private void InitializeDevice() {
			Dx11.Device.CreateWithSwapChain(
				Dx11.DriverType.Hardware, Dx11.DeviceCreationFlags.None,
				new Dxgi.SwapChainDescription() {
					BufferCount = 1, OutputHandle = this.Handle,
					IsWindowed = true,
					SampleDescription = new Dxgi.SampleDescription() {
						Count = 1, Quality = 0,
					}, ModeDescription = new Dxgi.ModeDescription() {
						Width = ClientSize.Width, Height = ClientSize.Height,
						RefreshRate = new Rational(60, 1),
						Format = Dxgi.Format.R8G8B8A8_UNorm
					}, Usage = Dxgi.Usage.RenderTargetOutput
				}, out device, out swapChain
			);

			InitializeRenderTarget();
			InitializeViewport();
			LoadContent();
		}

		private void InitializeRenderTarget() {
			using(var backBuffer = Dx11.Resource.FromSwapChain<Dx11.Texture2D>(swapChain, 0)) {
				renderTarget = new Dx11.RenderTargetView(device, backBuffer);
				device.ImmediateContext.OutputMerger.SetTargets(renderTarget);
			}
		}

		private void InitializeViewport() {
			device.ImmediateContext.Rasterizer.SetViewports(
				new Dx11.Viewport() { Width = ClientSize.Width, Height = ClientSize.Height }
			);
		}

		private void DisposeDevice() {
			UnloadContent();
			renderTarget.Dispose();
			device.Dispose();
			swapChain.Dispose();
		}
	}
}
