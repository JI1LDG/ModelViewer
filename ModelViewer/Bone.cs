using MmdFileLoader;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelViewer {
	public class BoneManager {
		public List<SkinBone> Bones { get; private set; }
		public List<SkinBone> Roots { get; private set; }
		public int MaxRank { get; private set; }

		public Matrix[] Results {
			get {
				return Bones.Select(x => x.Offset * x.Bone).ToArray();
			}
		}

		public BoneManager(MmdBone[] bones) {
			Bones = new List<SkinBone>();
			Roots = new List<SkinBone>();
			for(int i = 0;i < bones.Length; i++) {
				Bones.Add(new SkinBone(bones, i));
				if(MaxRank < bones[i].Rank) MaxRank = bones[i].Rank;
				if(bones[i].ParentIndex == -1) Roots.Add(Bones[i]);
			}

			for(int i = 0;i < bones.Length; i++) {
				if(bones[i].ParentIndex >= 0) Bones[i].Parent = Bones[bones[i].ParentIndex];
				for(int j = i + 1;j < bones.Length; j++) {
					if(i == bones[j].ParentIndex) {
						Bones[i].Children.Add(Bones[j]);
					}
				}
			}

			foreach(var r in Roots) {
				SkinBone.CalcRelative(r, Matrix.Identity);
			}
		}

		public void SetPose(ApplyedMotion[] motion) {
			foreach(var b in Bones) {
				b.MotionTranslate = Vector3.Zero;
				b.MotionRotate = Quaternion.Identity;
			}

			for(int i = 0;i < motion.Length; i++) {
				Bones[i].MotionRotate = motion[i].Rotate;
				Bones[i].MotionTranslate = motion[i].Translate;
			}
		}

		public Matrix CalcTranspose(Quaternion Rotation, Vector3 Translation) {
			return Matrix.RotationQuaternion(Rotation) * Matrix.Translation(Translation);
		}

		public void Update() {
			foreach(var b in Bones) {
				b.Translate = Vector3.Zero;
				b.Rotate = Quaternion.Identity;
			}

			UpdateAtPhysicBA(true);

			//Physic

			//UpdateAtPhysicBA(false);
		}

		private void UpdateAtPhysicBA(bool isBefore) {
			for(int i = 0; i < MaxRank + 1; i++) {
				foreach(var b in Bones) {
					if(b.IsBeforePhysic == isBefore && b.Rank == i) {
						b.Rotate *= b.MotionRotate;
						b.Translate += b.MotionTranslate;
					}
				}
				UpdateBone();

				//IK
			}
		}

		private void UpdateBone() {
			foreach(var b in Bones) {
				b.Bone = CalcTranspose(b.Rotate, b.Translate) * b.Init;
				if(b.Parent != null) b.Bone *= b.Parent.Bone;
			}
		}
	}

	public class SkinBone {
		public int Id { get; private set; }
		public string Name { get; private set; }
		public SkinBone Parent { get; set; }
		public List<SkinBone> Children { get; set; }
		public int Rank { get; private set; }
		public bool IsBeforePhysic { get; private set; }

		public Matrix Init { get; private set; }
		public Matrix Offset { get; private set; }
		public Matrix Bone { get; set; }

		public Quaternion Rotate { get; set; }
		public Vector3 Translate { get; set; }

		public Quaternion MotionRotate { get; set; }
		public Vector3 MotionTranslate { get; set; }

		public SkinBone(MmdBone[] bones, int index) {
			Id = index;
			Name = bones[index].Name;
			Children = new List<SkinBone>();
			Rank = bones[index].Rank;
			IsBeforePhysic = !bones[index].BoneFlag.HasFlag(BoneFlagEnum.TransformAfterPhysic);

			CreateMatrix(bones, index);
		}

		private void CreateMatrix(MmdBone[] bones, int index) {
			Vector3 pos = bones[index].Position;
			Vector3 tail;
			if(bones[index].TailIndex >= 0) {
				tail = bones[bones[index].TailIndex].Position;
			} else {
				tail = pos + bones[index].TailOffset;
			}

			Vector3 sub = Vector3.Normalize(tail - pos);
			if(sub.Length() == 0) {
				Init = Matrix.Translation(pos);
				Offset = Matrix.Translation(-pos);
				return;
			}

			Vector3 org = new Vector3(0, 1, 0);
			//Init = Matrix.RotationAxis(Vector3.Cross(org, sub), (float)Math.Acos(Vector3.Dot(org, sub))) * Matrix.Translation(pos);
			Init = Matrix.Translation(pos); //解せぬ
			Offset = Matrix.Invert(Init);
		}

		public static void CalcRelative(SkinBone me, Matrix parent) {
			foreach(var c in me.Children) {
				CalcRelative(c, me.Offset);
			}
			me.Init *= parent;
		}
	}
}
