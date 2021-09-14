	public abstract class Getraenk extends Lebensmittel{

	public Getraenk(String name, int menge) {
		super(name, menge);
	}
	
	public boolean essen() {
		return false;
	}
	
	public abstract boolean trinken();
	public abstract String status();
}