import gdp.stdlib.StdDraw;

public class Isogons {

	public static void main(String[] args) {
		int n = Integer.parseInt(args[0]);

		double myscale = 5.0; 

		double angle_diff = 360. / n;
		double angle = 0.0;
		double[] pos = new double[] { 3., 0 };

		StdDraw.setXscale(-myscale, myscale);
		StdDraw.setYscale(-myscale, myscale);

		for (int i = 0; i < n; i++, angle += angle_diff, pos = rotateP(angle_diff, pos)) {
			    drawIsogon(3 + i, angle, pos);
		}
		StdDraw.show();
	}

	static double[] rotateP(double angle, double[] point) {
		double[] rotatedpoint = new double[2];
		double sina = Math.sin(Math.toRadians(angle));
		double cosa = Math.cos(Math.toRadians(angle));
		rotatedpoint[0] = point[0]*cosa - point[1]*sina;
		rotatedpoint[1] = point[0]*sina + point[1]*cosa;
		return rotatedpoint;
	}

	static double[][] translatePoints2D(double[] shift, double[][] points) {
		for (int n = 0; n < points.length; n++) {
			for (int m = 0; m < 2; m++) {
				points[n][m] += shift[m];
			}
		}
		return points;
	}

	static double[][] rotatePoints2D(double angle, double[][] points) {
		for (int n = 0; n < points.length; n++) {
			points[n] = rotateP(angle, points[n]);
		}
		return points;
	}

	static double[][] unit_isogon_points(int n) {
		double[][] points = new double[n+1][2];
		double angle_diff = 360. / n;
		double angle = 0.0;
		points[0][0] = 1.;
		points[0][1] = 0;
		for (int m = 1; m <= n; m++) {
			angle += angle_diff;
			points[m] = rotateP(angle, points[0]);
		}
		return points;
		}
    
	static double[][] isogon_points(int n, double angle, double[] shift) {
		double[][] points = unit_isogon_points(n);
		points = rotatePoints2D(angle, points);
		points = translatePoints2D(shift, points);
		return points;
	}

	static double[] getCoordinates(double[][] points, int index) {
		double[] Coordinates = new double[points.length];
		for (int n = 0; n < points.length; n++) {
			Coordinates[n] = points[n][index];
		}
		return Coordinates;
	}
    
	static void drawIsogon(int n, double angle, double[] shift) {
		double[][] points = isogon_points(n, angle, shift);
		StdDraw.polygon(getCoordinates(points, 0),getCoordinates(points, 1));	
	}
}