namespace ModelViewer {
	static class Program {
		static void Main() {
			using(Core core = new DrawMmdModel(@"fubuki\isonami.pmx")) {
				core.Run();
			}
		}
	}
}
