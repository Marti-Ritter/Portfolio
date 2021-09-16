public class Solitair {

	public static void main(String[] args) {

		for (String arg: args) { // das ist eine sog. for-each-loop, die Java ab Version 1.5 kennt
			int N = Integer.parseInt(arg);
			if (N<1 || N>33) continue; // falsche Angaben werden uebergangen

			Problem p = new Problem(N);

			p.print();
			if (p.solve()) 
				System.out.println("geloest");
			else
				System.out.println("nicht loesbar");
		}
	}
}