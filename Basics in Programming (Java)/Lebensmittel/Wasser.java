public class Wasser extends Getraenk{

	public Wasser(String name, int menge) {
		super(name, menge);
	}
	
	public boolean trinken() {
		menge -= 200;
		return true;
	}
	
	public String status() {
		return "Wasser: " + name + ", " + menge + "ml";
	}
}
