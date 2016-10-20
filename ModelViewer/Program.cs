namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new DrawMmdModel(@"latmiku\latmiku.pmd")) {
				core.Run();
			}
		}
	}
}
