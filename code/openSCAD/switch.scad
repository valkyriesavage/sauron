fudge_factor = .8;

switch_width = 6.1 + fudge_factor;
switch_length = 6.1 + fudge_factor;
switch_height = 3.6 + fudge_factor;

module switch() {
  color("OliveDrab")
    cube(size=[switch_width,switch_length,switch_height]);
}
