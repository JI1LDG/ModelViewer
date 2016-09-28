namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new DrawPmdModel(@"cirno\cirno0.05.pmd")) {
				core.Run();
			}
		}
	}
}
