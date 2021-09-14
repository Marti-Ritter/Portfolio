public class Wurst extends Speise{

	public Wurst(String name, int menge) {
		super(name, menge);
	}
 
	 public boolean essen() {
		 menge -= 10;
		 return true;
	 }
	 
	 public String status() {
		 return "Wurst: " + name + ", " + menge + "g";
	 }
}
