using SlimDX;
using SlimDX.Multimedia;
using SlimDX.Windows;
using System.Windows.Forms;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using Rwin = SlimDX.RawInput;

namespace ModelViewer {
	class Core : Form {
		protected Dx11.Device device;
		protected Dxgi.SwapChain swapChain;
		protected Dx11.RenderTargetView renderTarget;
		protected Dx11.DepthStencilView depthStencil;
		protected Dxgi.Factory factor;

		protected virtual void LoadContent() { }
		protected virtual void UnloadContent() { }
		protected virtual void MouseInput(object sender, Rwin.MouseInputEventArgs e) { }
		protected virtual void KeyInput(object sender, Rwin.KeyboardInputEventArgs e) { }

		public void Run() {
			this.Height = this.Width = 800;
			InitializeDevice();
			MessagePump.Run(this, Draw);
			DisposeDevice();
		}

		protected virtual void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 0, 0, 1));

			swapChain.Present(0, Dxgi.PresentFlags.None);
		}

		private int qual = 1, count = 0;

		private void InitializeDevice() {
			device = new Dx11.Device(Dx11.DriverType.Hardware, Dx11.DeviceCreationFlags.None);
			for(int i = 0;i < 8; i++) {
				qual = device.CheckMultisampleQualityLevels(Dxgi.Format.R8G8B8A8_UNorm, i);
				if(qual > 0) {
					count = i;
				}
			}
			factor = new Dxgi.Factory();
			swapChain = new Dxgi.SwapChain(factor, device,
				new Dxgi.SwapChainDescription() {
					BufferCount = 1, OutputHandle = this.Handle,
					IsWindowed = true,
					SampleDescription = new Dxgi.SampleDescription() {
						Count = count, Quality = qual - 1,
					}, ModeDescription = new Dxgi.ModeDescription() {
						Width = ClientSize.Width, Height = ClientSize.Height,
						RefreshRate = new Rational(60, 1),
						Format = Dxgi.Format.R8G8B8A8_UNorm,
					}, Usage = Dxgi.Usage.RenderTargetOutput
				});
			InitializeRenderTarget();
			InitializeDepthStencil();
			InitializeViewport();
			LoadContent();
			InitializeInputDevice();
		}

		private void InitializeRenderTarget() {
			using(var backBuffer = Dx11.Resource.FromSwapChain<Dx11.Texture2D>(swapChain, 0)) {
				renderTarget = new Dx11.RenderTargetView(device, backBuffer);
				device.ImmediateContext.OutputMerger.SetTargets(renderTarget);
			}
		}

		private void InitializeDepthStencil() {
			using(var depthBuffer = new Dx11.Texture2D(device,
				new Dx11.Texture2DDescription() {
					ArraySize = 1, BindFlags = Dx11.BindFlags.DepthStencil,
					Format = Dxgi.Format.D32_Float,
					Width = ClientSize.Width, Height = ClientSize.Height,
					MipLevels = 1, SampleDescription = new Dxgi.SampleDescription(count, qual - 1),
				})) {
				depthStencil = new Dx11.DepthStencilView(device, depthBuffer,
					new Dx11.DepthStencilViewDescription() {
						Format = Dxgi.Format.D32_Float, Dimension = Dx11.DepthStencilViewDimension.Texture2DMultisampled,
					});
			}
			device.ImmediateContext.OutputMerger.SetTargets(depthStencil, renderTarget);
		}

		private void InitializeViewport() {
			device.ImmediateContext.Rasterizer.SetViewports(
				new Dx11.Viewport() { Width = ClientSize.Width, Height = ClientSize.Height, MaxZ = 1 }
			);
		}

		private void InitializeInputDevice() {
			Rwin.Device.RegisterDevice(UsagePage.Generic, UsageId.Mouse, Rwin.DeviceFlags.None);
			Rwin.Device.MouseInput += MouseInput;
			Rwin.Device.RegisterDevice(UsagePage.Generic, UsageId.Keyboard, Rwin.DeviceFlags.None);
			Rwin.Device.KeyboardInput += KeyInput;
		}

		private void DisposeDevice() {
			UnloadContent();
			factor?.Dispose();
			device.ImmediateContext.OutputMerger.BlendState?.Dispose();
			device.ImmediateContext?.Dispose();
			depthStencil?.Dispose();
			renderTarget?.Dispose();
			device?.Dispose();
			swapChain?.Dispose();
		}
	}
}
