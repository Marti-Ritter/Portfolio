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
		int DoubleMoon = 0;																		//Der Zählstand der bereits registrierten "Doppelvollmonde".
		int Mooncounter = 1;																	//Der Vollmond am 1.1.2013 wird nicht direkt berechnet. Stattdessen wird er einfach beim ersten Durchlauf als gegeben angenommen und beruecksichtigt.
		int absoluteMoonperiod = 29;															//Eine vollständige Mondphase dauert 29 Tage.
		int remainingMoonperiod = 30;															//Die verbleibende Mondphase ist die Zeit bis zum nächsten Vollmond. Im ersten Durchlauf wird sie als 30 Tage definiert um den "zusätzlichen" Vollmond am 1.1.2013 zu berücksichtigen.
		String MonthName = "Januar";															// Diese 2 Parameter werden von vornherein initialisiert um Fehlermeldungen zu vermeiden.
		int Monthlength = 1;
		for (int Year = 2013;;Year++) { 														//Jahreszähler
			for (int Month = 1;Month <= 12;Month++) { 											//Monatszähler
				switch (Month) { 																//Zuordnung von Name und Dauer zum Monatszähler.
				case 1: MonthName = "Januar";Monthlength = 31;break;
				case 2: MonthName = "Februar";
					if ((Year%4 == 0 && Year%100 != 0) || Year%400 == 0) {Monthlength = 29;}	//Berücksichtigung der verschiedenen Februarlänge in Schaltjahren. Die Kriterien wurden dem Vorlesungsskript entnommen.
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
//				int OriginalMonthlength = Monthlength;											//Optionale Variable für die Darstellung des exakten Vollmond-Datums
				while (Monthlength >= 0) {														//Solange der Monat noch "übrige" Tage hat oder gerade der letzte Tag erreicht ist, wird ihm der Vollmond zugeschrieben.
					Monthlength = Monthlength - remainingMoonperiod;							//Dem Monat wird die verstrichene Mondperiode abgezogen um zu prüfen ob der nächste Vollmond noch in diesem Monat liegt.
					if (Monthlength >= 0) {														//Der Monat ist länger oder gerade genauso lang wie die nächste Mondperiode.
//						System.out.println("Moon!: " + (OriginalMonthlength - Monthlength) + ". " + MonthName + " " + Year);	//Optionale Ausgabe des exakten Vollmond-Datums
						Mooncounter++;															//Der Vollmond wird also diesem Monat zugezählt um zu schauen, ob zwei Vollmonde in diesem Monat vorkommen.
						remainingMoonperiod = absoluteMoonperiod;								//Der Zähler der verbleibenden Mondperiode wird auf den Ursprungswert zurückgesetzt (29 Tage). Eine neue Mondperiode beginnt.
					}
					else { 																		//Der Monat ist kürzer als die nächste Mondperiode.
						remainingMoonperiod = -Monthlength;										//Die überschüssigen Tage dieser Mondperiode werden gemerkt und dem nächsten Monat abgerechnet.
						Mooncounter = 0;														//Der Monat ist zuende. Der Vollmondzähler wird für den nächsten Monat auf Null zurückgesetzt.
						break;																	//Da dieser Monat zuende ist, kann auch seine Schleife abgebrochen werden.
					}
					if (Mooncounter == 2) {														//Es wurden 2 Vollmonde in diesem Monat gefunden.
						System.out.println(Year + ", " + MonthName);							//Es wird der gefundene Monat ausgegeben.
						DoubleMoon++;															//Der Zähler aller gefundenen Doppelvollmonde wird um 1 erhöht.
						if (DoubleMoon == N) {System.exit(0);}									//Es wurden dem gegebenen Parameter N entsprechend genügend Doppelvollmonde gefunden. Das Skript wird beendet.
					}
				}
			}
		}
	}
}

