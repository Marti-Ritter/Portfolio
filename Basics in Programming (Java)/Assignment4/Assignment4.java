import java.io.BufferedReader;
import java.io.FileReader;
import java.util.Scanner;


public class Assignment4 {
	public static void main(String[] args) {
//		long Starttime = System.currentTimeMillis();
		String[][] Lists = getLists(args[0]);
		String Matrixindex = "";
		int[][] Matrix = new int[1][1];
		try {BufferedReader br = new BufferedReader(new FileReader(args[1]));
			Matrixindex = getIndex(br);
			Scanner sc = new Scanner(br);
			Matrix = getMatrix(sc, Matrixindex.length());
			sc.close();
			br.close();
		}
		catch(Exception E) {System.err.println(E);}
		
		int[][] Similarity = getSimilarity(Lists, Matrixindex, Matrix);

		String[] oldIndex = new String[Similarity.length];
		for(int i = 0; i<oldIndex.length; i++) {
			oldIndex[i] = Integer.toString(i+1);
		}
	
		String Startnode = "";
		String[] Nodes = new String[(oldIndex.length*2)+1];
		int i=0;
		
		boolean Start = true;
		while(Similarity.length>1) {
			if(!Start) System.out.print(", ");
			Start = false;
			int[] Maximum = getMaximum(Similarity);
			System.out.print("("+oldIndex[Maximum[0]]+","+oldIndex[Maximum[1]]+")");
			
			Startnode = oldIndex[Maximum[0]] + oldIndex[Maximum[1]];
			Nodes[i++] = oldIndex[Maximum[0]];
			Nodes[i++] = oldIndex[Maximum[1]];
			
			oldIndex = getnewIndex(oldIndex, Maximum);
			Similarity = getReducedSimilarity(Similarity, Maximum);
		}
		
		System.out.println();
		System.out.println();
		System.out.println(Startnode);
		Treeprinter(Startnode, Nodes, 0);
//		System.out.println(System.currentTimeMillis() - Starttime);
	}
	
	
	
	public static String[][] getLists(String Filename) {
		StringBuilder NameLine = new StringBuilder();
		StringBuilder SequenceLine = new StringBuilder();
		try {
			BufferedReader br = new BufferedReader(new FileReader(Filename));
			String line;
			boolean Start = true;
			while ((line=br.readLine()) != null) {
				String[] Test = line.split(">");
				if (Test.length > 1) {
					if(!Start) SequenceLine.append(",");
					NameLine.append(Test[1] + ",");
					Start = false;
					continue;
				}
				SequenceLine.append(line);
			}
			br.close();
		}
		catch(Exception E) {System.err.println(E);}
		
		String[] Namesplit = (NameLine.toString()).split(",");
		String[] Sequencesplit = (SequenceLine.toString()).split(",");
		String[][] Lists = new String[Namesplit.length][2];
		for(int i = 0; i < Namesplit.length; i++) {
			Lists[i][0] = Namesplit[i];
			Lists[i][1] = Sequencesplit[i];
		}

		for(int i=0; i<Lists.length; i++) {
			System.out.println(i+1 + "\t" + Lists[i][0]);
		}
		System.out.println();
/*
		for(int i=0; i<Lists.length; i++) {
			System.out.println(Lists[i][1] + "      ");
		}
		System.out.println();
*/
		return Lists;
	}
	
	
	public static String getIndex(BufferedReader br) {
		String Matrixindex = "";
		try {
			String line;	
			while ((line=br.readLine()) != null) {
				if(line.charAt(0) == '#') {
					continue;
				}
				for(int i=0; i<line.length();i++) {
					if("ABCDEFGHIJKLMNOPQRSTUVWXYZ*".contains("" + line.charAt(i))) {
						Matrixindex += line.charAt(i);
					}
				}
				break;
			}
		}
		catch(Exception E) {System.err.println(E);}
		
//		System.out.println(Matrixindex);
		
		return Matrixindex;
	}
	
	
	public static int[][] getMatrix(Scanner sc, int length) {
		int[][] Matrix = new int[length][length];
		try {	
			int x=0, y=0;
			sc.next();
			while(sc.hasNextInt()) {
				Matrix[x++][y] = sc.nextInt();
				if(x==Matrix.length) {
					y++;
					x=0;
					if (y < Matrix.length) sc.next();
				}
			}
		}
		
		catch (Exception E) {System.err.println(E);}
/*		
		for(int y=0;y<length;y++) {
			for(int x=0;x<length;x++) {
				if (Matrix[x][y]<0) System.out.print(Matrix[x][y] + " ");
				else System.out.print(" " + Matrix[x][y] + " ");
				}
			System.out.println();
		}
*/
		return Matrix;
	}
	
	
	public static int[][] getAlignment(String Sequence1, String Sequence2, String Matrixindex, int[][] Matrix) {
		int[][] AlignmentMatrix = new int[Sequence1.length()+1][Sequence2.length()+1];
		int Deletion1, Deletion2, Swap;
		
		for(int i = 1; i<Sequence1.length()+1; i++) {
			AlignmentMatrix[i][0] = AlignmentMatrix[i-1][0] + Matrix[Matrixindex.indexOf(Sequence1.charAt(i-1))][Matrix.length-1];
		}

		for(int j = 1; j<Sequence2.length()+1; j++) {
			AlignmentMatrix[0][j] = AlignmentMatrix[0][j-1] + Matrix[Matrix.length-1][Matrixindex.indexOf(Sequence2.charAt(j-1))];
		}
		
		for(int j = 1; j<Sequence2.length()+1; j++) {
			for(int i = 1; i<Sequence1.length()+1; i++) {
				Deletion1 = AlignmentMatrix[i-1][j] + Matrix[Matrixindex.indexOf(Sequence1.charAt(i-1))][Matrix.length-1];
				Deletion2 = AlignmentMatrix[i][j-1] + Matrix[Matrix.length-1][Matrixindex.indexOf(Sequence2.charAt(j-1))];
				Swap = AlignmentMatrix[i-1][j-1] + Matrix[Matrixindex.indexOf(Sequence1.charAt(i-1))][Matrixindex.indexOf(Sequence2.charAt(j-1))];
				AlignmentMatrix[i][j] = Math.max(Deletion1, Math.max(Deletion2, Swap));
/*
				System.out.println(i + " " + j);
				System.out.println(Sequence1.charAt(i-1) + " " + Sequence2.charAt(j-1));
				System.out.println(Deletion1 + " " + Deletion2 + " " + Swap);
				System.out.println(AlignmentMatrix[i][j]);
*/
			}
		}	
/*
		for(int y=0;y<Sequence2.length();y++) {
			for(int x=0;x<Sequence1.length();x++) {
				if(AlignmentMatrix[x][y] >= 100) System.out.print(AlignmentMatrix[x][y] + " ");
				else if(AlignmentMatrix[x][y] >= 10) System.out.print(AlignmentMatrix[x][y] + "  ");
				else if(AlignmentMatrix[x][y] >= 0)  System.out.print(AlignmentMatrix[x][y] + "   ");
				else System.out.print(AlignmentMatrix[x][y] + "  ");
				}
			System.out.println();
		}
*/
		return AlignmentMatrix;
	}

	
	public static int[][] getSimilarity(String[][] Lists, String Matrixindex, int[][] Matrix) {
		int[][] Similarity = new int[Lists.length][Lists.length];
		for(int y=0; y<Lists.length;y++) {
			for(int x=y+1; x<Lists.length;x++) {
				int[][] AlignmentMatrix = getAlignment(Lists[x][1], Lists[y][1], Matrixindex, Matrix);
				Similarity[x][y] = AlignmentMatrix[Lists[x][1].length()][Lists[y][1].length()];
				Similarity[y][x] = Similarity[x][y];
			}
		}
		
		System.out.print("\t");
		for(int x=0; x<Lists.length;x++) {
			System.out.print(x+1 + "\t");
		}
		System.out.println();
		for(int y=0; y<Lists.length;y++) {
			System.out.print(y+1 + "\t");
			for(int x=0; x<Lists.length;x++) {
				if(Similarity[x][y] == 0) System.out.print("\t");
				else System.out.print(Similarity[x][y] + "\t");
			}
			System.out.println();
		}
		System.out.println();
		
		return Similarity;
	}
	
	
	public static int[] getMaximum(int[][] Similarity) {
		int[] Position = new int[2];
		int Maximum = -Integer.MIN_VALUE;
		for(int j = 0; j<Similarity.length; j++) {
			for(int i = j+1; i<Similarity[j].length; i++) {
				if(Similarity[i][j] > Maximum) {
					Maximum = Similarity[i][j];
					Position[0] = i;
					Position[1] = j;
//					System.out.println(Maximum);
				}
			}
		}
//		System.out.println(Maximum);
		return Position;
	}
	
	
	public static String[] getnewIndex(String[] oldIndex, int[] Maximum) {
		String[] newIndex=new String[oldIndex.length-1];
		int j = 0;
		for(int i=0; i<oldIndex.length;i++) {
			if(i == Maximum[0] || i == Maximum[1]) continue;
			newIndex[j] = oldIndex[i];
			j++;
		}
		newIndex[newIndex.length-1] = oldIndex[Maximum[0]] +  oldIndex[Maximum[1]];
		return newIndex;
	}
	
	
	public static int[][] getReducedSimilarity(int[][] oldSimilarity, int[] Maximum) {
		int[][] newSimilarity = new int[oldSimilarity.length-1][oldSimilarity.length-1];
		
		int j = 0;
		for(int y=0; y<oldSimilarity.length;y++) {
			if(y == Maximum[0] || y == Maximum[1]) continue;
			int i = 0;
			for(int x=0; x<oldSimilarity[y].length;x++) {
				if(y<=x) continue;
				if(x == Maximum[0] || x == Maximum[1]) continue;
				newSimilarity[i][j] = oldSimilarity[x][y];
				newSimilarity[j][i] = newSimilarity[i][j];
				i++;
			}
			newSimilarity[newSimilarity.length-1][j] = (oldSimilarity[Maximum[0]][y]+oldSimilarity[Maximum[1]][y])/2;
			newSimilarity[j][newSimilarity.length-1] = newSimilarity[newSimilarity.length-1][j];
			j++;
		}	
/*
		for(int y=0; y<newSimilarity.length;y++) {
			for(int x=0; x<newSimilarity[y].length;x++) {
				System.out.print(newSimilarity[x][y] + " ");
			}
			System.out.println();
		}
*/
		return newSimilarity;
	}
	
	
	public static void Treeprinter(String Startnode, String[] Nodes, int Level) {
		if(Startnode.length() == 1) return;
		for(int x = 0; x<Nodes.length; x++) {
			for(int y = x+1; y<Nodes.length;y++) {
				if((Nodes[x] + Nodes[y]).equals(Startnode)) {
/*					
					for(int i = 0; i<Level+1;i++) {
						System.out.print("|  ");
					}
					System.out.println();
*/					
					for(int i = 0; i<Level;i++) {
						System.out.print("|  ");
					}					
					System.out.println("|--" + Nodes[y]);
					Treeprinter(Nodes[y], Nodes, Level+1);
/*					
					for(int i = 0; i<Level+1;i++) {
						System.out.print("|  ");
					}
					System.out.println();
*/					
					for(int i = 0; i<Level;i++) {
						System.out.print("|  ");
					}
					System.out.println(Nodes[x]);
					Treeprinter(Nodes[x], Nodes, Level);
				}
			}
		}
	}
}