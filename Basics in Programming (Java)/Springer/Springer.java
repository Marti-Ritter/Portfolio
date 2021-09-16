
public class Springer {
	public static void main(String[] args) {
		int N = Integer.parseInt(args[0]);
		boolean[][] Springer = new boolean[N][N];
		Springer[0][0] = true;		//Der erste Springer wird in der linken oberen Ecke platziert.
		Springer = nextSpringer(Springer);
		for (int r=0; r<N; r++) {
			for (int c=0; c<N; c++) {
				if (Springer[r][c]) System.out.print("S");
				else System.out.print("*");
			}
			System.out.println();
		}
	}

	public static boolean[][] nextSpringer(boolean[][] Springer) {
		int[][] Roesselsprung = new int[][] {{-2,-1},{-2,1},{-1,2},{1,2},{2,1},{2,-1},{1,-2},{-1,-2}};
		boolean[][] beaten = new boolean[Springer.length][Springer.length];
		for (int r=0; r<Springer.length; r++) {
			for (int c=0; c<Springer.length; c++) {
				if (Springer[r][c]) {
					for (int i = 0; i < 8; i++) {
						int r2 = Roesselsprung[i][0];
						int c2 = Roesselsprung[i][1];
						if (r+r2 < Springer.length && r+r2 >= 0 && c+c2 < Springer.length && c+c2 >= 0) {
							beaten[r+r2][c+c2] = true;
						}
					}
				}
			}
		}
		for (int r=0; r<Springer.length; r++) {
			for (int c=0; c<Springer.length; c++) {
				if (!Springer[r][c] && beatsbeaten(beaten, r, c)) {		//...Backtracking: wenn die entsprechende Position keine geschlagenen Felder schlagen kann, 
					Springer[r][c] = true;								//kann sie nicht platzsparend sein und kann dementsprechend nicht Teil der Loesung sein.
					Springer = nextSpringer(Springer);					//Rekursion
					return Springer;
				}
			}
		}
		for (int r=0; r<Springer.length; r++) {
			for (int c=0; c<Springer.length; c++) {
				if (!Springer[r][c] && !beaten[r][c]) {
					Springer[r][c] = true;
					Springer = nextSpringer(Springer);					//Rekursion
					return Springer;
				}
			}
		}
		return Springer;
	}

	public static boolean beatsbeaten(boolean[][] beaten, int r, int c) {
		int[][] Roesselsprung = new int[][] {{-2,-1},{-2,1},{-1,2},{1,2},{2,1},{2,-1},{1,-2},{-1,-2}};
		for (int i = 0; i < 8; i++) {
			int r2 = Roesselsprung[i][0];
			int c2 = Roesselsprung[i][1];
			if (r+r2 < beaten.length && r+r2 >= 0 && c+c2 < beaten.length && c+c2 >= 0) {
				if (beaten[r+r2][c+c2]) return true;
			}
		}
		return false;
	}
}
