/**
 *
 */

/**
 * @author Marti
 *
 */

public class MonteHall {
	
	/**
	 * @param args
	 */
	
	public static void main(String[] args) {
		int N = Integer.parseInt(args[0]);
		int SuccessChangedDoor = 0;
		int SuccessNotChangedDoor = 0;
		for (int Counter = 0; Counter < N; Counter++){
			double r = Math.random();
			int rightdoor = (int) (r*3);
			r = Math.random();
			int chosendoor = (int) (r*3);
			if (chosendoor == rightdoor) {
				SuccessNotChangedDoor++;
				}
			}
		for (int Counter = 0; Counter < N; Counter++){
			double r = Math.random();
			int rightdoor = (int) (r*3);
			r = Math.random();
			int chosendoor = (int) (r*3);
			if (chosendoor != rightdoor) {
				SuccessChangedDoor++;
			}
		}
		double RateChangedDoor =  1.0 * SuccessChangedDoor / N;
		double RateNotChangedDoor = 1.0 * SuccessNotChangedDoor / N;
		System.out.println("Wechseln: "+RateChangedDoor);
		System.out.println("Nichtwechseln: "+RateNotChangedDoor);
	}
}
