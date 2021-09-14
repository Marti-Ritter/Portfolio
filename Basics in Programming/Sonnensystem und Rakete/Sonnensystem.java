/**
 * @author Marti
 *
 */
import gdp.stdlib.*;
public class Sonnensystem {
	public static void main(String[] args) {
		int timestep = 2500;
		int N = StdIn.readInt();
		int EarthNumber = 1;
		double R = StdIn.readDouble();
		String[] PlanetNames = new String[N];
		double[][] PlanetCoordinates_xy = new double[N][2];
		double[][] PlanetVelocity_xy = new double[N][2];
		double[] PlanetMass = new double[N];
		for (int i = 0; i < N; i++) {
			for (int n = 0; n < 2; n++) {PlanetCoordinates_xy[i][n] = StdIn.readDouble();}
			for (int n = 0; n < 2; n++) {PlanetVelocity_xy[i][n] = StdIn.readDouble();}
			PlanetMass[i] = StdIn.readDouble();
			PlanetNames[i] = StdIn.readString();
			if (PlanetNames[i] == "earth.gif") EarthNumber = i;
		}
		double[] RocketStats = {PlanetCoordinates_xy[EarthNumber][0],PlanetCoordinates_xy[EarthNumber][1],PlanetVelocity_xy[EarthNumber][0], PlanetVelocity_xy[EarthNumber][1],2.97E6, 34.02E6};
		StdDraw.setXscale(-R, R);
		StdDraw.setYscale(-R, R);
		StdAudio.play("2001.mid");
		while (true) {
			StdDraw.clear();
			StdDraw.picture(0.0, 0.0, "starfield.jpg");
			StdDraw.picture(RocketStats[0], RocketStats[1], "acorn3.gif");
			for (int n = 0; n < N; n++) {
				StdDraw.picture(PlanetCoordinates_xy[n][0],PlanetCoordinates_xy[n][1],(String) PlanetNames[n]);
			}
			StdDraw.show(10);
			double[] ForceX = new double[N];
			double[] ForceY = new double[N];
			double RocketForceX = 0;
			double RocketForceY = 0;
			for (int n = 0; n < N; n++) {
				for (int m = 0; m < N; m++) {
					if (m == n ) continue;
					double DistanceX = PlanetCoordinates_xy[m][0] - PlanetCoordinates_xy[n][0];
					double DistanceY = PlanetCoordinates_xy[m][1] - PlanetCoordinates_xy[n][1];
					double Distance = Math.sqrt(DistanceX*DistanceX + DistanceY*DistanceY);
					ForceX[n] += (6.67E-11*PlanetMass[n]*PlanetMass[m])/(Distance*Distance)*(DistanceX/Distance);
					ForceY[n] += (6.67E-11*PlanetMass[n]*PlanetMass[m])/(Distance*Distance)*(DistanceY/Distance);
				}
			}
			for (int n = 0; n < N; n++) {
				PlanetVelocity_xy[n][0] += ForceX[n] * timestep / PlanetMass[n];
				PlanetVelocity_xy[n][1] += ForceY[n] * timestep / PlanetMass[n];
				PlanetCoordinates_xy[n][0] += PlanetVelocity_xy[n][0] * timestep;
				PlanetCoordinates_xy[n][1] += PlanetVelocity_xy[n][1] * timestep;
				
				double DistanceX = PlanetCoordinates_xy[n][0] - RocketStats[0];
				double DistanceY = PlanetCoordinates_xy[n][1] - RocketStats[1];
				double Distance = Math.sqrt(DistanceX*DistanceX + DistanceY*DistanceY);
				RocketForceX += (6.67E-11*PlanetMass[n]*RocketStats[4])/(Distance*Distance)*(DistanceX/Distance);
				RocketForceY += (6.67E-11*PlanetMass[n]*RocketStats[4])/(Distance*Distance)*(DistanceY/Distance);
				if (PlanetNames[n] == "mars.gif") {
					RocketForceX += RocketStats[5]*(DistanceX/Distance);
					RocketForceY += RocketStats[5]*(DistanceY/Distance);
				}
				RocketStats[2] += RocketForceX * timestep / RocketStats[4];
				RocketStats[3] += RocketForceY * timestep / RocketStats[4];
			}
			if (StdDraw.mousePressed()) {
				double DistanceX = StdDraw.mouseX() - RocketStats[0];
				double DistanceY = StdDraw.mouseY() - RocketStats[1];
				double Distance = Math.sqrt(DistanceX*DistanceX + DistanceY*DistanceY);
				double ThrustX = RocketStats[5] * DistanceX/Distance;
				double ThrustY = RocketStats[5] * DistanceY/Distance;
				RocketStats[2] += ThrustX * timestep / RocketStats[4];
				RocketStats[3] += ThrustY * timestep / RocketStats[4];
			}
			RocketStats[0] += RocketStats[2] * timestep;
			RocketStats[1] += RocketStats[3] * timestep;
			if (RocketStats[0]>R||RocketStats[0]<-R) RocketStats[2] = 0;
			if (RocketStats[1]>R||RocketStats[1]<-R) RocketStats[3] = 0;
		}
	}

}
