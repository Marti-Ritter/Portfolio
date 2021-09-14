
public class Angle {
	private double Degree;
	
	public Angle() {
	}
	
	public Angle(double Size) {
		Degree = Size;
	}
	
	public double getDegree() {
		return Degree;
	}
	
	public Angle add(Angle Addition) {
		Degree += Addition.Degree;
		return this;
	}
}
