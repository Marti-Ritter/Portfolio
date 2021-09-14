import java.io.BufferedReader;
import java.io.FileReader;

public class Fasta {

	public static void main(String[] args) {
		String Filename = args[0];
		try (BufferedReader br = new BufferedReader(new FileReader(Filename))) {
			String line;
			long Counter=0;
			while ((line=br.readLine()) != null) {
				String[] Test = line.split(">");
				if (Test.length > 1) {
					if (Counter != 0) {
						System.out.println(Counter);
						Counter = 0;
					}
					System.out.print(Test[1] + "\t");
					continue;
				}
				Counter+=line.length();
			}
			System.out.println(Counter);
		}
		catch (Exception E) {
			System.out.println("an error occured");
		}
	}
}
