import java.io.BufferedReader;
import java.io.FileReader;
import java.lang.StringBuilder;
import java.util.Arrays;


public class BoyerMoore {

	public static void main(String[] args) {
		try {
			BufferedReader br = new BufferedReader(new FileReader(args[1]));
			String sequenceline;
			StringBuilder Sequence = new StringBuilder();
			while ((sequenceline=br.readLine()) != null) {
				String[] Test = sequenceline.split(">");
				if (Test.length > 1) {
					continue;
				}
				Sequence.append(sequenceline);
			}
			br.close();
			br = new BufferedReader(new FileReader(args[0]));
			boolean Start = true;
			String line;
			StringBuilder Pattern = new StringBuilder();
			while ((line=br.readLine()) != null) {
				String[] Test = line.split(">");
				if (Test.length > 1) {
					if (Start == false) {
						boyermoore(Sequence, Pattern);
						Pattern = new StringBuilder();
					}
					Start=false;
					continue;
				}
				Pattern.append(line);
			}
			boyermoore(Sequence, Pattern);
		}
		catch (Exception E) {
		System.err.println(E);
		}
	}
	
	public static void boyermoore(StringBuilder Sequence, StringBuilder Pattern) {
		String P = Pattern.toString();
		char[] ch  = P.toCharArray();
		int[] BCRTable = new int[26];
		Arrays.fill(BCRTable, ch.length);
		for(int x = 0; x<ch.length; x++) {
			char c = ch[x];
			int temp = (int)c;
			if (ch.length-1-x == 0) continue;
			BCRTable[temp-97] = ch.length-1-x;
		}
		
/*		for(int y = 0; y<BCRTable.length; y++) {
			System.out.print(BCRTable[y]);
		} */
		
		//int Counter = 0;
		int PL = Pattern.length()-1;
		for(int i=0; i<Sequence.length(); i++) {
			for(int j=0; j <= PL;j++) {
				if(Sequence.charAt(i+PL-j) != Pattern.charAt(PL-j)) {
					char RightChar = Sequence.charAt(i+PL);
					int BCR = BCRTable[(int)RightChar-97];
					i+=BCR;
					if(i+Pattern.length() >= Sequence.length()) {
		//				System.out.print(">"+Pattern.toString()+" "+Counter+"; ");
						return;
					}
					j=-1;
				}
		//		else System.out.println(Sequence.charAt(i+PL-j) + " = " + Pattern.charAt(PL-j));
			}
			System.out.println(i + ", " + Pattern);
		//	Counter++;
		}
	}
	
}