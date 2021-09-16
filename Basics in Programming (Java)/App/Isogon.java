import gdp.stdlib.StdDraw;

public class Isogon {
	private Point[] Corners;
	
	public Isogon() {	
	}
	
	public Isogon(int n) {
		Corners = new Point[n];
		Angle delta = new Angle(360./n);
		Corners[0] = new Point(1.,0.);
		for (int i = 1; i < n; i++) {
			Corners[i] = Corners[i-1].rotate(delta);
		}
	}
	
	public Isogon rotate(Angle phi) {
		for (int i = 0; i < Corners.length; i++) {
			Corners[i] = Corners[i].rotate(phi);
		}
		return this;
	}
	
	public Isogon translate(Point pos) {
		for (int i = 0; i < Corners.length; i++) {
			Corners[i] = Corners[i].translate(pos);
		}
		return this;
	}
	
	public void draw() {
		for (int i = 1; i < Corners.length; i++) {
			StdDraw.line(Corners[i-1].getX(), Corners[i-1].getY(), Corners[i].getX(), Corners[i].getY());
		}
		StdDraw.line(Corners[0].getX(), Corners[0].getY(), Corners[Corners.length-1].getX(), Corners[Corners.length-1].getY());
	}
}
