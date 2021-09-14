public class Brot extends Speise{
	protected int typ;

	public Brot(int typ, int menge) {
		super("Brot", menge);
		this.typ = typ;
	}
	
	public boolean essen() {
		menge -= 50;
		return true;
	}
	
	public String status() {
		String Art;
		switch (typ) {
		case 0: Art = "Weissbrot"; break;
		case 1: Art = "Schwarzbrot"; break;
		case 2: Art = "Mischbrot"; break;
		default: Art = "Spezialbrot";
		}
		return "Brot: " + Art + ", " + menge + "g";
	}
}