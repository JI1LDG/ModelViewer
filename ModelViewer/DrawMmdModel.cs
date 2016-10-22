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
		NormalEffect effect;
		MmdLoader mmdLoader;
		int flameCount;
		MovingData movingNow;
		Camera camera;
		VmdLoader vmdLoader;
		MotionManager motMng;
		BoneManager boneMng;

		public DrawMmdModel(string Path) {
			mmdLoader = new MmdLoader(Path);
			vmdLoader = new VmdLoader(@"motion\kl.vmd");
			flameCount = 0;
			movingNow = new MovingData();
			camera = new Camera();
			camera.ViewTarget = new Vector3(0, 10, 0);
			camera.ViewEye = new Vector3(0, 10, -45);
			motMng = new MotionManager(mmdLoader.Bone);
			boneMng = new BoneManager(mmdLoader.Bone);
			motMng.SetMotion(vmdLoader.Motion, true);
			motMng.Start();
		}

		protected override void Draw() {
			device.ImmediateContext.ClearRenderTargetView(renderTarget, new Color4(1, 1, 0, 1));
			device.ImmediateContext.ClearDepthStencilView(depthStencil, Dx11.DepthStencilClearFlags.Depth, 1, 0);

			UpdateCamera();
			boneMng.SetPose(motMng.GetMotion());
			boneMng.Update();
			effect.SetBoneMatrix(boneMng.Results);
			effect.DrawAll(camera);

			swapChain.Present(0, Dxgi.PresentFlags.None);

			flameCount++;
		}

		private void UpdateCamera() {
			camera.Update(movingNow, ClientSize);
		}

		protected override void LoadContent() {
			InitializeEffect();
		}

		private void InitializeEffect() {
			effect = new NormalEffect(device, "effect.fx", mmdLoader.Vertex.Select(x => new VertexData() {
				Position = x.Position, Normal = x.Normal, Uv = x.Uv,
				Index = new IntegerArr() { idx1 = x.Index[0], idx2 = x.Index[1], idx3 = x.Index[2], idx4 = x.Index[3] }, Weight = x.Weight
			}).ToArray(), mmdLoader.Index, mmdLoader.ParentDir, mmdLoader.Material, mmdLoader.Bone);
		}

		protected override void UnloadContent() {
			effect?.Dispose();
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

	public struct MovingData {
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
