import gdp.stdlib.StdDraw;
public class Tree {			//@Marti Ritter, @v1.1
							//StdDraw-nutzendes Programm, welches mit einem gegebenen Parameter n einen zufaelligen Pythagoras-Baum mit n Verzweigungen erzeugt.

	public static void main(String[] args) {	//main-Funktion und Startpunkt des Programmes
		int n = Integer.parseInt(args[0]);		//Kommandozeilenparameter wird ausgelesen
		double[][] points = new double[][] {{-0.5,0.},{0.5,0.}};	//die obere Kante des ersten Quadrats wird ueber 2 Punkte definiert
		StdDraw.setXscale(-4, 4);	//Die 8x8 grosse Leinwand wurde in diesen Masstaeben gewaehlt, damit die auf der in der x-Achse liegende
		StdDraw.setYscale(-1, 7);	//Grundlinie einen moeglichst gut zentrierten und unverzerrten Baum erzeugt.
		StdDraw.line(points[0][0],points[0][1],points[1][0],points[1][1]);	//Im folgenden wird das Quadrat UNTER der Grundlinie aufgebaut.
		StdDraw.line(points[0][0],points[0][1],points[0][0],points[0][1]-1);
		StdDraw.line(points[1][0],points[1][1],points[1][0],points[1][1]-1);
		StdDraw.line(points[0][0],points[0][1]-1,points[1][0],points[1][1]-1);
		DrawTriangle(n, points);	//Das erste Dreieck wird auf dem ersten Quadrat aufgerufen. Dazu werden der Dreiecksfunktion die Grundlinie
	}								//und die Anzahl der Ebenen (n) uebergeben.
	
	static void DrawBox(int n, double[][] points) {	//Quadratsfunktion, welche auf einer gegebenen Grundlinie eine Box aufbaut und, falls noetig, das daraufliegende Dreieck aufruft.
		double[] relativepoint = new double[] {points[1][0]-points[0][0], points[1][1]-points[0][1]};	//Es wird zuerst der Relativvektor zwischen dem linken und rechten Basispunkt berechnet.
		relativepoint = rotateP(90, relativepoint);	//Der Relativvektor (welcher ja theoretisch im Koordinatenursprung steht) wird mithilfe der rotateP-Funktion 90 Grad nach links gedreht.
		double[][] newpoints = new double[2][2];	//Es wird ein Array zum Speichern der beiden anderen Eckpunkte, welche auch gleichzeitig die Startpunkte des neuen Dreiecks sind, angelegt.
		for (int i = 0; i < 2; i++) {	//Die neuen Eckpunkte werden durch die Summe der alten Eckpunkte und des gedrehten Relativvektors berechnet.
			newpoints[i][0] = points[i][0] + relativepoint[0];
			newpoints[i][1] = points[i][1] + relativepoint[1];
		}
		StdDraw.line(points[0][0],points[0][1],newpoints[0][0],newpoints[0][1]);	//Das Quadrat wird mithilfe der alten Eckpunkte und der neuen gezeichnet.
		StdDraw.line(newpoints[0][0],newpoints[0][1],newpoints[1][0],newpoints[1][1]);
		StdDraw.line(points[1][0],points[1][1],newpoints[1][0],newpoints[1][1]);
		n--;	//Die Zahl der noch zu zeichnenden Ebenen wird um 1 reduziert (es wurde soeben eine Ebene abgeschlossen).
		if (n>0) DrawTriangle(n, newpoints);	//Sofern noch eine Ebene gezeichnet werden soll (n>0), wird das naechste Dreieck auf den neuen Eckpunkten gezeichnet.
	}
	
	static void DrawTriangle(int n, double[][] points) {	//Dreiecksfunktion, welche auf einer gegebenen Grundlinie ein zufaelliges, rechtwinkliges Dreieck erzeugt und anschliessend auf den Katheten die dazugehoerigen Quadrate aufruft.
		double[] mpoint = new double[] {(points[1][0]+points[0][0])/2, (points[1][1]+points[0][1])/2};	//Die Bestimmung des 3. Eckpunktes erfolgt ueber den Satz des Thales. Deshalb wird zuerst der Mittelpunkt aus dem Durchschnitt der beiden Basispunkte gebildet.
		double[] rpoint = new double[] {points[1][0]-mpoint[0], points[1][1]-mpoint[1]};	//Es wird der Relativvektor zwischen Mittelpunkt und rechten Eckpunkt erzeugt (Da die Drehung in mathematisch positive Richtung nach links erfolgen wird).
		double Alpha = Math.random()*30+30;	//Es wird, wie gefordert, der linke Winkel im Bereich 30-60 Grad zufaellig erzeugt.
		rpoint = rotateP(2*Alpha, rpoint);	//Mithilfe einer trigonometrischen Betrachtung wird klar, dass der Winkel zwischen Mittelpunktsvektor und der Hypothenuse 2*Alpha betragen muss.
		double[] tip = new double[] {mpoint[0]+rpoint[0], mpoint[1]+rpoint[1]};	//Der 3. Eckpunkt wird aus der Summe des gedrehten Mittelpunktsvektors und dem Mittelpunkt berechnet.
		StdDraw.line(points[0][0],points[0][1],tip[0],tip[1]);	//Die 3 Eckpunkte werden mit 2 Geraden verbunden, wobei die Grundlinie schon im Quadrat enthalten ist.
		StdDraw.line(points[1][0],points[1][1],tip[0],tip[1]);
		double[][] leftpoints = new double[][] {points[0],tip};	//Es werden die Basispunkte fuer das linke Quadrat abgespeichert.
		double[][] rightpoints = new double[][] {tip,points[1]};	//Die Zuordnung der Punkte fuer das rechte Quadrat erfolgt andersherum, damit der Aufbau des Quadrats (nach links) in die richtige Richtung erfolgt.
		DrawBox(n, leftpoints);		//Die Quadratsfunktion fuer die linke Box wird aufgerufen,
		DrawBox(n, rightpoints);	//dasselbe wird fuer die rechte Box getan.
	}
	
	static double[] rotateP(double angle, double[] point) {	//Die rotateP-Funktion, welche ich in der Isogons-Aufgabenstellung des letzten Praktikums schreiben sollte.
		double[] rotatedpoint = new double[2];	//Es wird ein Speicherplatz fuer die Koordinaten des gedrehten Punktes erstellt.
		double sina = Math.sin(Math.toRadians(angle));	//Drehung des Punktes/Ortsvektors nach der Drehmatrix
		double cosa = Math.cos(Math.toRadians(angle));
		rotatedpoint[0] = point[0]*cosa - point[1]*sina;
		rotatedpoint[1] = point[0]*sina + point[1]*cosa;
		return rotatedpoint;	//Rueckgabe des gedrehten Punktes
	}
	
}
