public abstract class Speise extends Lebensmittel{

	public Speise(String name, int menge) {
		super(name, menge);
	}
	
	public boolean trinken() {
		return false;
	}
	
	public abstract boolean essen();
	public abstract String status();
}
