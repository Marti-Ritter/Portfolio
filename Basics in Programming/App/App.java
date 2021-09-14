import gdp.stdlib.*;

public class App {

public static void main(String[] args) {
		if (args.length == 0) System.exit(-1);
		int n = Integer.parseInt(args[0]); 

		double myscale = 5.0; 

		Angle delta = new Angle(360. / n);
		Angle phi   = new Angle(0.0);

		Point pos = new Point(3., 0); 

		StdDraw.setXscale(-myscale, myscale);
		StdDraw.setYscale(-myscale, myscale);

		for (int i = 3; i < 3+n; i++) {

			new Isogon(i).rotate(phi).translate(pos.rotate(phi)).draw();
			phi = phi.add(delta);
		}

		StdDraw.show();
	}
}