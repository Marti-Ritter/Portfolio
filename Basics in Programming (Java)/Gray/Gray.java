public class Gray {
	public static int toGray (int n) {
		int Binary = 0;
		for (int i = 0; i < 31; i++) {
			int Schrittweite = (int) Math.pow(2, i+1);
			int Schrittabschnitt = n/Schrittweite;
			if (Schrittabschnitt%2 == 0) {
				if (n%Schrittweite >= Schrittweite/2) Binary += (int) Math.pow(2, i);
			}
			else {
				if (n%Schrittweite < Schrittweite/2) Binary += (int) Math.pow(2, i);
			}
		}
		return Binary;
	}
	public static int fromGray (int n) {
		int[] Code = new int[31];
		for (int i = 0; i < 31; i++) {
			if (n >= Math.pow(2, (30-i))) {
				Code[i] = 1;
				n -= Math.pow(2, (30-i));
			}
		}
		int Grayzahl = 0;
		long Abschnitt = (long) Math.pow(2, 31);
		for (int i = 0; i < 31; i++) {
			long Schrittweite = (long)Math.pow(2, 31-i);
			if ((Abschnitt/Schrittweite)%2 != 0) {
				if (Code[i] == 0) Abschnitt -= Schrittweite/2;
				else Grayzahl += Schrittweite/2;
			}
			else {
				if (Code[i] == 0) Grayzahl += Schrittweite/2;
				else Abschnitt -= Schrittweite/2;
			}
		}
		return Grayzahl;
	}
}