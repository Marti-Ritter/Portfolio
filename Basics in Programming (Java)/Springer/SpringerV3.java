
public class SpringerV3 {

	public static void main(String[] args) {
		long Start = System.currentTimeMillis();
		int N = Integer.parseInt(args[0]);
		boolean[][] Springer = new boolean[N][N];
		Springer = nterSpringer(Springer, 1, N);
		for (int r=0; r<N; r++) {
			for (int c=0; c<N; c++) {
				if (Springer[r][c]) System.out.print("S");
				else System.out.print("*");
			}
			System.out.println();
		}
		long End = System.currentTimeMillis();
		System.out.println(End-Start);
	}

	public static boolean[][] nterSpringer(boolean[][] Springer, int n, int N) {
		if (n == 1) {
			Springer[0][0] = true;
			Springer = nterSpringer(Springer, n+1, N);
			return Springer;
		}
		int[][] Roesselsprung = new int[][] {{-2,-1},{-2,1},{-1,2},{1,2},{2,1},{2,-1},{1,-2},{-1,-2}};
		boolean[][] beaten = new boolean[N][N];
		for (int r=0; r<N; r++) {
			for (int c=0; c<N; c++) {
				if (Springer[r][c]) {
					for (int i = 0; i < 8; i++) {
						int r2 = Roesselsprung[i][0];
						int c2 = Roesselsprung[i][1];
						if (r+r2 < N && r+r2 >= 0 && c+c2 < N && c+c2 >= 0) {
							beaten[r+r2][c+c2] = true;
						}
					}
				}
			}
		}
		for (int r=0; r<N; r++) {
			for (int c=0; c<N; c++) {
				if (!Springer[r][c] && beatsbeaten(beaten, r, c, N)) {	//...Backtracking: wenn die entsprechende Position keine geschlagenen Felder schlagen kann, 
					Springer[r][c] = true;								//kann sie nicht platzsparend sein und kann dementsprechend nicht Teil der Loesung sein.
					Springer = nterSpringer(Springer, n+1, N);	//Rekursion
					return Springer;
				}
			}
		}
		for (int r=0; r<N; r++) {
			for (int c=0; c<N; c++) {
				if (!Springer[r][c] && !beaten[r][c]) {
					Springer[r][c] = true;
					Springer = nterSpringer(Springer, n+1, N);	//Rekursion
					return Springer;
				}
			}
		}
		return Springer;
	}

	public static boolean beatsbeaten(boolean[][] beaten, int r, int c, int N) {
		int[][] Roesselsprung = new int[][] {{-2,-1},{-2,1},{-1,2},{1,2},{2,1},{2,-1},{1,-2},{-1,-2}};
		for (int i = 0; i < 8; i++) {
			int r2 = Roesselsprung[i][0];
			int c2 = Roesselsprung[i][1];
			if (r+r2 < N && r+r2 >= 0 && c+c2 < N && c+c2 >= 0) {
				if (beaten[r+r2][c+c2]) return true;
			}
		}
		return false;
	}
}
