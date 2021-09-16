
/**
 * @author Marti Ritter, 557247
 */
/* Im folgenden Programm werden nur Zahlen betrachtet, welche folgende Regeln erfüllen:
 * 	1. Sie dürfen keine 0 enthalten. Dadurch wird das Querprodukt im nächsten Schritt 0 und die kleinste 1-schrittige Zahl ist 10, weshalb alle folgenden Zahlen mit Nullen egal sind.
 * 	2. Sie dürfen keine 1 enthalten. In einem Produkt ist eine 1 irrelevant und verlängert als Ziffer lediglich die Zahl, wodurch diese auch nicht mehr die kleinste Zahl sein kann.
 * 	3. Alle Ziffern müssen von links nach rechts nach Größe geordnet sein, da sonst immer eine kleinere Permutation derselben Ziffern existiert und die entsprechende Zahl nicht mehr die kleinste Zahl mit diesem Querproduktsverlauf sein kann.
 * Wegen dieser Regeln bestehen alle Zahlen in der Berechnung zuerst nur aus Zweien und werden dann nach folgendem Schema hochgezählt:
 * 22
 * 23
 * ...
 * 29
 * 33
 * 34
 * ...
 * 39
 * 44
 * ...
 * 55
 * ...
 * 22222229
 * 22222233
 * ...
 * 89999999
 * 99999999
 * 222222222
 * Und so weiter...
 */
public class QuerV2 {
		public static void main(String[] args) {
			long Start = System.currentTimeMillis();
	 		int N = Integer.parseInt(args[0]);
			boolean[] kList = new boolean[N];
			for (int i = 0;i < kList.length;i++){
				kList[i] = true;
			}
			System.out.println("1 Schritte: 10");
			kList[0] = false;
			int k;
			long Querprodukt;
			long QuerNumber;
			for (int Numberlength = 2;; Numberlength++){
				int[] Number = new int[Numberlength];
				for (int n = 0;n<Numberlength;n++) {
					Number[n] = 2;
				}
				for (;Number[0]<9;Number[Numberlength-1]++) {
					if (Number[Numberlength-1] > 9) {
						for (int n = Numberlength-1;n>0;n--) {
							if (Number[n] > 9) {
								Number[n-1]++;
								for (int t = n; t<Numberlength; t++) {
									Number[t] = Number[n-1];
								}
							}
						}
					}
					String Numberstring = "";
					for (int n = 0; n< Numberlength; n++) {
						Numberstring= Numberstring + Number[n];
					}
					k = k(Number);
					if (kList[k-1] == true) {
						System.out.println(k + " Schritte: " + Numberstring);
						kList[k-1] = false;
						if (k == N){
							long End = System.currentTimeMillis();
							System.out.println(End-Start);
							System.exit(0);
						}
					}
//					System.out.println(Numberstring);
				}
			}		
		}
		public static int k(int[] Number) {
			int k = 1;
			int[] Querprodukt = new int[Number.length];
			Querprodukt [Number.length-1] = 1;
			int Numberlength = Number.length;
			for (int n = 0; n< Numberlength; n++) {
				for (int m = 0; n< Numberlength; n++) {
					Querprodukt[m] = Querprodukt[m] * Number[n];
				}
				for (int m = 0; n< Numberlength; n++) {
					if (Querprodukt[m] > 9) {
						Querprodukt[m-1] = Querprodukt[m]/10;
						Querprodukt[m] = Querprodukt[m]%10;
					}
				}
			}
			int Querproduktlength = Querprodukt.length;
			for (int Length = 2;Length>1;) {
				Length = Querproduktlength;
				for (int n = 0; n < Querproduktlength; n++) {
					if (Querprodukt[n] != 0) break;
					Length--;
				}
				
			}
			return k;
		}
	}

/* 	1 Schritte: 10
	2 Schritte: 25
	3 Schritte: 39
	4 Schritte: 77
	5 Schritte: 679
	6 Schritte: 6788
	7 Schritte: 68889
	8 Schritte: 2677889
	9 Schritte: 26888999
	10 Schritte: 3778888999
	11 Schritte: 277777788888899 --> 2,5 Sekunden
	("12 Schritte: 555555555555555677777777777777888888888888888899" --> Long-Overflow nach rund 1 Stunde, fehlerhaftes Ergebnis) */