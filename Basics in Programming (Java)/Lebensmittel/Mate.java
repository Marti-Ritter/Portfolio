public class Mate extends Getraenk{

	public Mate(String name) {
		super(name, 500);
	}
	
	public boolean trinken() {
		menge -= 100;
		return true;
	}
	
	public String status() {
		return "Mate: " + name + ", " + menge + "ml";
	}
}
