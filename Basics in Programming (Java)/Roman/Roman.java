
public class Roman {

	static String toRoman(int n) {
		if (n == 0) return "";
		int[] Grenzen = new int[] {1,4,5,9,10,40,50,90,100,400,500,900,1000};
		String[] Buchstaben = new String[] {"I","IV","V","IX","X","XL","L","XC","C","CD","D","CM","M"};
		for (int i = 12; i >= 0; i--) {
			if (n>=Grenzen[i]) return (Buchstaben[i]+toRoman(n-Grenzen[i]));
		}
		return "";
	}
	
	public static void main(String[] args) {
		if (args.length==0) return;
		int N = Integer.parseInt(args[0]);	
		System.out.println(toRoman(N));
	}
}