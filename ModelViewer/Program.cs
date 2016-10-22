namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new DrawMmdModel(@"cirno\cirno.pmd")) {
				core.Run();
			}
		}
	}
}
