using MmdFileLoader;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelViewer {
	public class BoneManager {
		public List<SkinBone> Bones { get; private set; }
		public Matrix[] Results { get; private set; }
		public List<SkinBone> Roots { get; private set; }
		public int MaxRank { get; private set; }

		public BoneManager(MmdBone[] bones) {
			Bones = new List<SkinBone>();
			Roots = new List<SkinBone>();
			for(int i = 0;i < bones.Length; i++) {
				Bones.Add(new SkinBone(bones, i));
				if(MaxRank < bones[i].Rank) MaxRank = bones[i].Rank;
				if(bones[i].ParentIndex == -1) Roots.Add(Bones[i]);
			}

			for(int i = 0;i < bones.Length; i++) {
				for(int j = i + 1;j < bones.Length; j++) {
					if(i == bones[j].ParentIndex) {
						Bones[i].Children.Add(Bones[j]);
					}
				}
			}

			foreach(var r in Roots) {
				SkinBone.CalcRelative(Bones[r.Id], Matrix.Identity);
			}
		}

		public void SetPose(VmdMotion[] motion) {
			foreach(var x in Bones) { 
				x.Bone = Matrix.Identity;
				x.Transpose = Matrix.Identity;
			}
			foreach(var m in motion) {
				var target = Bones.FirstOrDefault(x => x.Name == m.BoneName);
				if(target != null) {
					target.SetTranspose(m.Rotation, m.Position);
				}
			}
		}

		public void Update() {
			Results = Enumerable.Range(0, Bones.Count).Select(x => Matrix.Identity).ToArray();
			foreach(var b in Bones) {
				b.Bone = b.Init;
				b.Bone = b.Transpose * b.Bone;
			}

			foreach(var r in Roots) {
				CalcBone(Bones[r.Id], Matrix.Identity, 0);
			}
		}

		private void CalcBone(SkinBone me, Matrix parent, int rank) {
			me.Bone *= parent;
			Results[me.Id] = me.Offset * me.Bone;
			foreach(var c in me.Children) {
				CalcBone(c, me.Bone, rank);
			}
		}
	}

	public class SkinBone {
		public int Id { get; private set; }
		public string Name { get; private set; }
		public List<SkinBone> Children { get; set; }
		public int Rank { get; private set; }
		public bool IsBeforePhysic { get; private set; }

		public Matrix Init { get; private set; }
		public Matrix Offset { get; private set; }
		public Matrix Transpose { get; set; }
		public Matrix Bone { get; set; }

		public SkinBone(MmdBone[] bones, int index) {
			Id = index;
			Name = bones[index].Name;
			Children = new List<SkinBone>();
			Rank = bones[index].Rank;
			IsBeforePhysic = !bones[index].BoneFlag.HasFlag(BoneFlagEnum.TransformAfterPhysic);

			Transpose = Matrix.Identity;
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
			Init = Matrix.RotationAxis(Vector3.Cross(org, sub), (float)Math.Acos(Vector3.Dot(org, sub))) * Matrix.Translation(pos);
			Offset = Matrix.Invert(Init);
		}

		public void SetTranspose(Quaternion Rotation, Vector3 Translation) {
			Transpose = Matrix.RotationQuaternion(Rotation) * Matrix.Translation(Translation);
		}

		public static void CalcRelative(SkinBone me, Matrix parentOffset) {
			foreach(var c in me.Children) {
				CalcRelative(c, me.Offset);
			}
			me.Init *= parentOffset;
		}
	}
}
