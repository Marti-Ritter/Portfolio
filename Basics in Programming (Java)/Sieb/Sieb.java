
public class Sieb {

	public static void main(String[] args) {
		int N = Integer.parseInt(args[0]);
		int M = 9;
		for (;;M = M*10) {
			int[] FoundPrimes = new int[N];
			int Primeposition = 0;
			boolean[] p = new boolean[M];
			for (int i = 2; i<M; i++) {
				p[i] = true;
			}
			for (int i = 2; i<M;i++) {
				if (p[i]) {
					FoundPrimes[Primeposition] = i;
					Primeposition++;
					for (int n = 1; n*i < M; n++) {
						p[n*i] = false;
						}
					if (Primeposition == N) {
						for (int m = 0; m < N; m++) {
							System.out.println(FoundPrimes[m]);
						}
						System.exit(0);
					}
										
				}
			}
		}
	}

}
