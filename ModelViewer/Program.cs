namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new DrawPmdModel(@"cirno\cirno.pmd")) {
				core.Run();
			}
		}
	}
}
