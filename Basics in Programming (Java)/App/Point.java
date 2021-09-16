
public class Point {
	private double x;
	private double y;
	
	public Point() {
		
	}
	
	public Point(double a, double b) {
		x = a;
		y = b;
	}
	
	public double getX() {
		return x;
	}
	
	public double getY() {
		return y;
	}
	
	public Point rotate(Angle phi) {
		double sina = Math.sin(Math.toRadians(phi.getDegree()));
		double cosa = Math.cos(Math.toRadians(phi.getDegree()));
		double newx = x*cosa - y*sina;
		double newy = x*sina + y*cosa;
		return new Point(newx, newy);
	}
	
	public Point translate(Point pos) {
		x = x + pos.getX();
		y = y + pos.getY();
		return this;
	}
}
