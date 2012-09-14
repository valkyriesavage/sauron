translate([0,0,1]) {
	union() {
		translate([0,0,25])
			sphere(25);
		cube(size=[50,50,2], center=true);
	}
}