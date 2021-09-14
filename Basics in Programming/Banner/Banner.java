/**
 * @author Marti Ritter, 557247
 *
 */

public class Banner {
	public static void main(String[] args) {
		String Line1 = "";
		String Line2 = "";
		String Line3 = "";
		String Line4 = "";
		String Line5 = "";
		String Line6 = "";
		String Line7 = "";
		String Inputstring = args[0];
		String[] Splitstring = Inputstring.split("", 11);
		int Number = 0;
		for (int n = 0;n < Splitstring.length - 1;n++) {
			try {Number = Integer.parseInt(Splitstring[n]);}
			catch (NumberFormatException e) {
				System.err.println("Unter den ersten 10 Zeichen ist (mindestens) eines, welches keine Ziffer ist!");
				System.err.println("Bitte geben Sie nur Ziffern in den ersten 10 Stellen ein!");
				System.exit(-1);
			}
			switch (Number) {
			case 0:	Line1 = Line1+"  ###   "; Line2 = Line2+" #   #  "; Line3 = Line3+"#     # "; Line4 = Line4+"#     # "; Line5 = Line5+"#     # "; Line6 = Line6+" #   #  "; Line7 = Line7+"  ###   "; break;
			case 1:	Line1 = Line1+"   #   "; Line2 = Line2+"  ##   "; Line3 = Line3+" # #   "; Line4 = Line4+"   #   "; Line5 = Line5+"   #   "; Line6 = Line6+"   #   "; Line7 = Line7+" ##### "; break;
			case 2: Line1 = Line1+" #####  "; Line2 = Line2+"#     # "; Line3 = Line3+"      # "; Line4 = Line4+" #####  "; Line5 = Line5+"#       "; Line6 = Line6+"#       "; Line7 = Line7+"####### "; break;
			case 3: Line1 = Line1+" #####  "; Line2 = Line2+"#     # "; Line3 = Line3+"      # "; Line4 = Line4+" #####  "; Line5 = Line5+"      # "; Line6 = Line6+"#     # "; Line7 = Line7+" #####  "; break;
			case 4: Line1 = Line1+"#       "; Line2 = Line2+"#    #  "; Line3 = Line3+"#    #  "; Line4 = Line4+"#    #  "; Line5 = Line5+"####### "; Line6 = Line6+"     #  "; Line7 = Line7+"     #  "; break;
			case 5: Line1 = Line1+"####### "; Line2 = Line2+"#       "; Line3 = Line3+"#       "; Line4 = Line4+"######  "; Line5 = Line5+"      # "; Line6 = Line6+"#     # "; Line7 = Line7+" #####  "; break;
			case 6: Line1 = Line1+" #####  "; Line2 = Line2+"#     # "; Line3 = Line3+"#       "; Line4 = Line4+"######  "; Line5 = Line5+"#     # "; Line6 = Line6+"#     # "; Line7 = Line7+" #####  "; break;
			case 7: Line1 = Line1+"####### "; Line2 = Line2+"#    #  "; Line3 = Line3+"    #   "; Line4 = Line4+"   #    "; Line5 = Line5+"  #     "; Line6 = Line6+"  #     "; Line7 = Line7+"  #     "; break;
			case 8: Line1 = Line1+" #####  "; Line2 = Line2+"#     # "; Line3 = Line3+"#     # "; Line4 = Line4+" #####  "; Line5 = Line5+"#     # "; Line6 = Line6+"#     # "; Line7 = Line7+" #####  "; break;
			case 9: Line1 = Line1+" #####  "; Line2 = Line2+"#     # "; Line3 = Line3+"#     # "; Line4 = Line4+" ###### "; Line5 = Line5+"      # "; Line6 = Line6+"#     # "; Line7 = Line7+" #####  "; break;
			}
		}
		System.out.println(Line1);
		System.out.println(Line2);
		System.out.println(Line3);
		System.out.println(Line4);
		System.out.println(Line5);
		System.out.println(Line6);
		System.out.println(Line7);
	}
}
