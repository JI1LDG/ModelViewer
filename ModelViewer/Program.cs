using System;
using System.Windows.Forms;

namespace ModelViewer {
	static class Program {
		static void Main() {
			using(var core = new Core()) {
				core.Run();
			}
		}
	}
}
