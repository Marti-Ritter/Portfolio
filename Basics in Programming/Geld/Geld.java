class Geld {
	public static boolean cached;
	public static long[][] Cache;
	public static void main(String[] s) {
		int sum = 0;
		try {
			if (s[0].equals("-c")) {
				cached = true;
				sum = Integer.parseInt(s[1]);
			}
			else sum = Integer.parseInt(s[0]);
		}
		catch (Exception e) {System.out.println("Aufruf mit Geldbetrag (in Cent) als Parameter"); System.exit(0);}
		System.out.print(euro(sum));
		System.out.print(" kann auf ");
		System.out.print(pay(sum));
		System.out.println(" verschiedene Arten passend bezahlt werden");
	}

	public static long pay (int m) {
		if (cached) Cache = new long[m][8];
		long possibilities = pay(m, 7);
		return possibilities;
	}

	private static long pay (int m, int n) {
		if (m == 0 || n == 0) return 1;
		if (cached) {
			if (Cache[m-1][n] != 0) return (Cache[m-1][n]);
		}
		int[] Muenzen = new int[] {1,2,5,10,20,50,100,200};
		long result;
		if (m < Muenzen[n]) result = pay(m, n-1);
		else result = pay(m, n-1) + pay(m-Muenzen[n],n);
		if (cached) {
			Cache[m-1][n] = result;
		}
		return result;
	}

	public static String million() {
		for (int sum = 0;;sum++) {
			if (pay(sum) > 1000000) return (euro(sum));
		}
	}

	public static String billion() {
		for (int sum = 0;;sum++) {
			if (pay(sum) > 1000000000) return (euro(sum));
		}
	}

	public static String euro(int cent) {
		int euro = cent/100;
		String Eurostring;
		if ((cent%100) < 10) Eurostring = euro + "," + 0 + (cent%100) + " Euro";
		else Eurostring = euro + "," + (cent%100) + " Euro";
		return Eurostring;
	}
}