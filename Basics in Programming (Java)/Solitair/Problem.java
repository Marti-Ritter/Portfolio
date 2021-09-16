class Problem {
	
		Feld feld;    // das Brett 
		int n;        // Anzahl der verbleibenden Steine
		String[] zuege;
		
		// ... was sonst noch gebraucht wird ...
		public Problem(int N) {
			feld = new Feld();
			zuege = new String[31];
			int Position = 0;
			for(int i = 2; i < 9; i++) {
				for(int j = 2; j < 9; j++) {
					if (feld.peg(i,j)) Position++;
					if (Position==N) {
						feld.flip(i,j);
						n = 32;
						return;
					}
				}
			}
		}
			
		public void print() {
			feld.print();
		}
		public boolean solve() {
			feld.print();
			if (n == 1) {
				for (String Zug: zuege) {
					System.out.println(Zug);
				}
				return true;
			}
			else {
				int Position = 0;
				for(int i = 2; i < 9; i++) {
					for(int j = 2; j < 9; j++) {
						if (feld.peg(i,j)) {
							Position++;
							if (feld.nopeg(i-2,j) && feld.peg(i-1,j)) {
								feld.flip(i,j);
								feld.flip(i-1,j);
								feld.flip(i-2,j);
								zuege[32-n] = Position + " links";
								n--;
								if (this.solve()) return true;
								return false;
							}
							else if (feld.nopeg(i+2,j) && feld.nopeg(i+1,j)) {
								feld.flip(i,j);
								feld.flip(i+1,j);
								feld.flip(i+2,j);
								zuege[32-n] = Position + " rechts";
								n--;
								if (this.solve()) return true;
								return false;
							}
							else if (feld.nopeg(i,j-2) && feld.nopeg(i,j-1)) {
								feld.flip(i,j);
								feld.flip(i,j-1);
								feld.flip(i,j-2);
								zuege[32-n] = Position + " hoch";
								n--;
								if (this.solve()) return true;
								return false;
							}
							else if (feld.nopeg(i,j+2) && feld.nopeg(i,j+1)) {
								feld.flip(i,j);
								feld.flip(i,j+1);
								feld.flip(i,j+2);
								zuege[32-n] = Position + " runter";
								n--;
								if (this.solve()) return true;
								return false;
							}
						}
					}
				}
			}
			return false;
		}
}