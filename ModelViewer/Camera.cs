using SlimDX;
using System;
using System.Drawing;

namespace ModelViewer {
	public class Camera {
		public Vector3 ViewEye { get; set; }
		public Vector3 ViewTarget { get; set; }
		public Matrix World { get; private set; }
		public Matrix View { get; private set; }
		public Matrix Projection { get; private set; }
		public Vector4 LightDir { get; private set; }
		public Vector3 AmbientLight { get; private set; }
		public Vector4 EyePosition { get; private set; }
		private readonly float divXY = 10.0f;
		private readonly float divZ = 0.5f;

		public Camera() {
			AmbientLight = new Vector3(0.58f);
		}

		public void Update(MovingData movingNow, Size ClientSize) {
			World = Matrix.Identity;
			View = Matrix.Multiply(
				Matrix.Multiply(
					Matrix.RotationX(-movingNow.rotY / 100.0f),
					Matrix.RotationY(movingNow.rotX / 100.0f)
				), Matrix.Multiply(
					Matrix.LookAtLH(ViewEye, ViewTarget, new Vector3(0, 1, 0)),
					Matrix.Translation(movingNow.posX / divXY, -movingNow.posY / divXY, movingNow.posZ * divZ)
				)
			);
			var newEye = Vector3.Transform(
				ViewEye,
				Matrix.Multiply(
					Matrix.Multiply(
						Matrix.RotationX(-movingNow.rotY / 100.0f),
						Matrix.RotationY(movingNow.rotX / 100.0f)
					),
					Matrix.Translation(movingNow.posX / divXY, -movingNow.posY / divXY, movingNow.posZ * divZ)
				));

			Projection = Matrix.PerspectiveFovLH(
				30 * (float)Math.PI / 180, ClientSize.Width / ClientSize.Height, 0.1f, 1000
			);

			LightDir = -newEye;
			EyePosition = newEye;
		}
	}
}
