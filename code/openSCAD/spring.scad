include <screw_thread.scad>

coil_diameter = 4;
core_diameter = 25;
btwn_coil_height = 3;
total_coil_height = 50;

rotate(a=[90,0,0]) {
	spring( coil_diameter, core_diameter, btwn_coil_height, total_coil_height );
}
