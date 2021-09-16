
public class GGT {
	public static void main(String args[]) {
		int a=Integer.parseInt(args[0]);
		int b=Integer.parseInt(args[1]);
		if (a<=0||b<=0) {
			System.out.println("nur positive ganze Zahlen als Argumente erlaubt");
			System.exit(-1);
		}
/*1.*/	int m=a;
		int n=b;
		int r=1;
/*5.*/	while (r!=0) {
/*2.*/		if (m<n) {
				int t=m;
				m=n;
				n=t;
			}
/*3.*/		r=m-n;
/*4.*/		m=n;
			n=r;
		}
		System.out.println("ggT("+a+", "+b+") = "+m);
	}
}
