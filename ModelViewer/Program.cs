using System;
using System.Windows.Forms;

namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new Triangle()) {
				core.Run();
			}
		}
	}
}
