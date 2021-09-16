/**
 * 
 */

/**
 * @author Marti
 *
 */
public class DoubleMoon {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
		int N = Integer.parseInt(args[0]);
		int DoubleMoon = 0;																		//Der Z�hlstand der bereits registrierten "Doppelvollmonde".
		int Mooncounter = 1;																	//Der Vollmond am 1.1.2013 wird nicht direkt berechnet. Stattdessen wird er einfach beim ersten Durchlauf als gegeben angenommen und beruecksichtigt.
		int absoluteMoonperiod = 29;															//Eine vollst�ndige Mondphase dauert 29 Tage.
		int remainingMoonperiod = 30;															//Die verbleibende Mondphase ist die Zeit bis zum n�chsten Vollmond. Im ersten Durchlauf wird sie als 30 Tage definiert um den "zus�tzlichen" Vollmond am 1.1.2013 zu ber�cksichtigen.
		String MonthName = "Januar";															// Diese 2 Parameter werden von vornherein initialisiert um Fehlermeldungen zu vermeiden.
		int Monthlength = 1;
		for (int Year = 2013;;Year++) { 														//Jahresz�hler
			for (int Month = 1;Month <= 12;Month++) { 											//Monatsz�hler
				switch (Month) { 																//Zuordnung von Name und Dauer zum Monatsz�hler.
				case 1: MonthName = "Januar";Monthlength = 31;break;
				case 2: MonthName = "Februar";
					if ((Year%4 == 0 && Year%100 != 0) || Year%400 == 0) {Monthlength = 29;}	//Ber�cksichtigung der verschiedenen Februarl�nge in Schaltjahren. Die Kriterien wurden dem Vorlesungsskript entnommen.
					else {Monthlength = 28;}
					break;
				case 3: MonthName = "Maerz";Monthlength = 31;break;
				case 4: MonthName = "April";Monthlength = 30;break;
				case 5: MonthName = "Mai";Monthlength = 31;break;
				case 6: MonthName = "Juni";Monthlength = 30;break;
				case 7: MonthName = "Juli";Monthlength = 31;break;
				case 8: MonthName = "August";Monthlength = 31;break;
				case 9: MonthName = "September";Monthlength = 30;break;
				case 10: MonthName = "Oktober";Monthlength = 31;break;
				case 11: MonthName = "November";Monthlength = 30;break;
				case 12: MonthName = "Dezember";Monthlength = 31;break;
				}
//				int OriginalMonthlength = Monthlength;											//Optionale Variable f�r die Darstellung des exakten Vollmond-Datums
				while (Monthlength >= 0) {														//Solange der Monat noch "�brige" Tage hat oder gerade der letzte Tag erreicht ist, wird ihm der Vollmond zugeschrieben.
					Monthlength = Monthlength - remainingMoonperiod;							//Dem Monat wird die verstrichene Mondperiode abgezogen um zu pr�fen ob der n�chste Vollmond noch in diesem Monat liegt.
					if (Monthlength >= 0) {														//Der Monat ist l�nger oder gerade genauso lang wie die n�chste Mondperiode.
//						System.out.println("Moon!: " + (OriginalMonthlength - Monthlength) + ". " + MonthName + " " + Year);	//Optionale Ausgabe des exakten Vollmond-Datums
						Mooncounter++;															//Der Vollmond wird also diesem Monat zugez�hlt um zu schauen, ob zwei Vollmonde in diesem Monat vorkommen.
						remainingMoonperiod = absoluteMoonperiod;								//Der Z�hler der verbleibenden Mondperiode wird auf den Ursprungswert zur�ckgesetzt (29 Tage). Eine neue Mondperiode beginnt.
					}
					else { 																		//Der Monat ist k�rzer als die n�chste Mondperiode.
						remainingMoonperiod = -Monthlength;										//Die �bersch�ssigen Tage dieser Mondperiode werden gemerkt und dem n�chsten Monat abgerechnet.
						Mooncounter = 0;														//Der Monat ist zuende. Der Vollmondz�hler wird f�r den n�chsten Monat auf Null zur�ckgesetzt.
						break;																	//Da dieser Monat zuende ist, kann auch seine Schleife abgebrochen werden.
					}
					if (Mooncounter == 2) {														//Es wurden 2 Vollmonde in diesem Monat gefunden.
						System.out.println(Year + ", " + MonthName);							//Es wird der gefundene Monat ausgegeben.
						DoubleMoon++;															//Der Z�hler aller gefundenen Doppelvollmonde wird um 1 erh�ht.
						if (DoubleMoon == N) {System.exit(0);}									//Es wurden dem gegebenen Parameter N entsprechend gen�gend Doppelvollmonde gefunden. Das Skript wird beendet.
					}
				}
			}
		}
	}
}

