using MmdFileLoader;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ModelViewer {
	public class MotionManager {
		private MotionBone[] motionList;
		private Stopwatch stopWatch;
		private readonly int FPS = 30;
		public float NowFrame { get { return FPS * stopWatch.ElapsedMilliseconds / 1000.0f; } }

		public MotionManager(MmdBone[] bones) {
			motionList = bones.Select(x => new MotionBone(x.Name)).ToArray();
			stopWatch = new Stopwatch();
		}

		public void SetMotion(VmdMotion[] motion, bool append) {
			if(append) foreach(var m in motionList) m.MotionList = new List<MotionData>();
			foreach(var m in motion) {
				var target = motionList.FirstOrDefault(x => x.BoneName == m.BoneName);
				if(target == null) continue;
				target.MotionList.Add(new MotionData(m));
			}

			foreach(var m in motionList) {
				if(m.MotionList.Count == 0) continue;
				m.StartFrame = m.MotionList.Min(x => x.FrameCount);
				m.EndFrame = m.MotionList.Max(x => x.FrameCount);
			}
		}

		public void Start() {
			stopWatch.Start();
		}

		public ApplyedMotion[] GetMotion() {
			var tmp = new ApplyedMotion[motionList.Length];
			var nowFrame = NowFrame;

			for(int i = 0;i < tmp.Length; i++) {
				tmp[i] = new ApplyedMotion();
				var nowList = motionList[i].MotionList;
				if(nowList.Count == 0) continue;
				int startFrm = motionList[i].StartFrame;
				int endFrm = motionList[i].EndFrame;

				if(endFrm <= nowFrame) {
					var nowAt = nowList.Last();
					tmp[i].Rotate = nowAt.Rotate;
					tmp[i].Translate = nowAt.Translate;
				} else if(nowFrame < startFrm) {
					int nowIdx = 0;
					var t = (nowFrame) / (nowList[nowIdx].FrameCount);
					tmp[i].Translate = Vector3.Lerp(Vector3.Zero, nowList[nowIdx].Translate, t);
					tmp[i].Rotate = Quaternion.Lerp(Quaternion.Identity, nowList[nowIdx].Rotate, t);
				} else {
					int nowIdx = 0;
					while(nowList[nowIdx].FrameCount <= nowFrame) nowIdx++;
					if(nowIdx > 0) nowIdx--;
					var t = (nowFrame - nowList[nowIdx].FrameCount) / (nowList[nowIdx + 1].FrameCount - nowList[nowIdx].FrameCount);
					tmp[i].Translate = Vector3.Lerp(nowList[nowIdx].Translate, nowList[nowIdx + 1].Translate, t);
					tmp[i].Rotate = Quaternion.Lerp(nowList[nowIdx].Rotate, nowList[nowIdx + 1].Rotate, t);
				}
			}

			return tmp;
		}

		public void Stop() {
			stopWatch.Stop();
		}

		public void Reset() {
			stopWatch.Reset();
		}

		private float CalcBezier(float x, Interpolation interp) {
			float t = 0.5f, s = 1.0f - t;
			for(int i = 0;i < 15; i++) {
				var ft = (3 * s * s * t * interp.AX / 127.0f) + (3 * s * t * t * interp.BX / 127.0f) + (t * t * t) - x;
				if(ft == 0) break;
				if(ft > 0) t -= 1.0f / (4 << i);
				else t += 1.0f / (4 << i);
				s = 1.0f - t;
			}
			return (3 * s * s * t * interp.AY) + (3 * s * t * t * interp.BY) + (t * t * t);
		}
	}

	public class MotionBone {
		public string BoneName;
		public List<MotionData> MotionList;
		public int StartFrame;
		public int EndFrame;

		public MotionBone(string name) {
			BoneName = name;
			MotionList = new List<MotionData>();
		}
	}

	public class MotionData {
		public int FrameCount;
		public Quaternion Rotate;
		public Vector3 Translate;
		public Interpolation XInterp;
		public Interpolation YInterp;
		public Interpolation ZInterp;
		public Interpolation RotInterp;

		public MotionData(VmdMotion motion) {
			FrameCount = motion.FrameCount;
			Rotate = motion.Rotation;
			Translate = motion.Position;
			XInterp = motion.XInterp;
			YInterp = motion.YInterp;
			ZInterp = motion.ZInterp;
			RotInterp = motion.RotInterp;
		}
	}

	public class ApplyedMotion {
		public Quaternion Rotate;
		public Vector3 Translate;

		public ApplyedMotion() {
			Rotate = Quaternion.Identity;
			Translate = Vector3.Zero;
		}
	}
}
