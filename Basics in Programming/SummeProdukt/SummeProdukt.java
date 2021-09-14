
public class SummeProdukt {

	public static void main(String[] args) {
//		long Start = System.currentTimeMillis();
		int N = Integer.parseInt(args[0]);
		boolean[] isitinteresting = new boolean[2*N];
		int InterestingCounter = 2*N-3;
		for (int n = 4; n <= 2*N; n++) isitinteresting[n-1] = true;
		for (int n = 4; n <= 2*N; n++) {
			for (int i = 2; i < n-1; i++) {
				if (!mrproductdoesntknow(i*(n-i), N)) {
					isitinteresting[n-1] = false;
					InterestingCounter--;
					break;
				}
			}
		}
		int[][] interestingsums = new int[InterestingCounter][2];
		int k = 0;
		for (int n = 4; n < 2*N; n++) {
			if (isitinteresting[n-1]) {
				interestingsums[k][0] = n;
				k++;
//				System.out.println(n);
			}
		}
		boolean[] uniqueunique = new boolean[interestingsums.length];
		for (int n = 0; n < uniqueunique.length; n++) {
			uniqueunique[n] = true;
		}
		for (int n = 0; n < interestingsums.length; n++) {
			for (int i = 0; i < interestingsums[n][0]-1; i++) {
				if (uniqueproduct(i*(interestingsums[n][0]-i), interestingsums)) {
					if ((interestingsums[n][1] != 0) && (interestingsums[n][1] != i*(interestingsums[n][0]-i))) uniqueunique[n] = false;
					interestingsums[n][1] = i*(interestingsums[n][0]-i);
				}
			}
		}
		for (int n = 0; n < interestingsums.length; n++) {
			if ((interestingsums[n][1] != 0) && uniqueunique[n]) {
//				System.out.println("Summe: " + interestingsums[n][0]);
//				System.out.println("Produkt: " + interestingsums[n][1]);
				for (int a = 2; a < N; a++) {
					if (interestingsums[n][0]-a == interestingsums[n][1]/a) {
						System.out.println(a + " " + (interestingsums[n][0]-a));
						break;
					}
				}
			}
		}
//		long End = System.currentTimeMillis();
//		System.out.println(End-Start);
	}
	
	static boolean mrproductdoesntknow(int Product, int Range) {
		int ProductCounter = 0;
		for (int x = 2; x <= Range; x++) {
			for (int y = 2; y <= Range; y++) {
				if (x*y == Product) {
					ProductCounter++;
					if (ProductCounter > 2) return true;
				}
			}
		}	
		return false;
	}
	static boolean uniqueproduct(int Product, int[][] interestingsums) {
		int ProductCounter = 0;
		for (int n = 0; n < interestingsums.length; n++) {
			for (int i = 2; i < interestingsums[n][0]-1; i++) {
				if (i*(interestingsums[n][0]-i) == Product) {
					ProductCounter++;
					if (ProductCounter > 2) return false;
				}
			}
		}
		if (ProductCounter == 2) return true;
		return false;
	}
}
