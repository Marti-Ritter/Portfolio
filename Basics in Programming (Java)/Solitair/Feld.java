public class Feld {

	boolean[][] Pegs;
	
	public static void main(String[] args) {
		Feld Field = new Feld();
		Field.print();
	}
	
	public Feld() {
		Pegs = new boolean[11][11];
		for(int i = 2; i < 9; i++) {
			for(int j = 2; j < 9; j++) {
				if(i+j > 5 && i+j < 15 && i-j < 5 && i-j > -5) {
					Pegs[i][j] = true;
				}
			}
		}
		Pegs[3][3] = false; Pegs [3][7] = false; Pegs[7][3] = false; Pegs[7][7] = false;
	}
	
	public void print() {
		for(int i = 2; i < 9; i++) {
			for(int j = 2; j < 9; j++) {
				if (Pegs[i][j]) System.out.print("o");
				else System.out.print(" ");
			}
			System.out.println();
		}
	}
	
	public void flip (int i, int j) {
		Pegs[i][j] ^= true;
	}
	
	public boolean peg(int i, int j) {
		if (Pegs[i][j]) return true;
		return false;
	}

	public boolean nopeg(int i, int j) {
		if (!Pegs[i][j]) return true;
		return false;
	}
}