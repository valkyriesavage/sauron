include <screw_thread.scad>

t_d1 = 4;  // Diameter of coils in the helix
t_d2 = 25; // Diameter of central thread (add d1 for max diameter )
t_h1 = 3; // Height between coils (0 means coils have no separation)
t_h2 = 30; // Height of total coil (should be non-zero)
spring( t_d1, t_d2, t_h1, t_h2 );
