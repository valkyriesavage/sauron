from django.db import models

class Thing(models.Model):
  name = models.CharField(max_length=200)

class Shape(models.Model):
  thing = models.OneToOneField(Thing)

class Triangle(models.Model):
  shape = models.ForeignKey(Shape)

class Point(models.Model):
  x = models.IntegerField()
  y = models.IntegerField()
  z = models.IntegerField()
  triangle = models.ForeignKey(Triangle)

class Location(models.Model):
  x = models.IntegerField()
  y = models.IntegerField()
  z = models.IntegerField()
  x_rot = models.IntegerField()
  y_rot = models.IntegerField()
  z_rot = models.IntegerField()
  shape = models.ForeignKey(Shape)

class InteractiveBit(models.Model):
  base_model_location = models.CharField(max_length=512)
  location = models.ForeignKey(Location)
  thing = models.ForeignKey(Thing)

class Button:
  button_style = models.FileField(upload_to="custom/button-%y%m%d-%H%M%S.stl")
  clickable = models.BooleanField()

class Joystick(InteractiveBit):
  knob_style = models.FileField(upload_to="custom/knob-%y%m%d-%H%M%S.stl")
  clickable = models.BooleanField()

class Slider(InteractiveBit):
  detentes = models.BooleanField()

class DirectionPad(InteractiveBit):
  num_directions = models.IntegerField()
  clickable = models.BooleanField()

class ScrollWheel(InteractiveBit):
  clickable = models.BooleanField()
  detentes = models.BooleanField()

class Dial(InteractiveBit):
  knob_style = models.FileField(upload_to="custom/knob-%y%m%d-%H%M%S.stl")
  clickable = models.BooleanField()
  detentes = models.BooleanField()
  positions = models.IntegerField()

class TrackBall(InteractiveBit):
  eep = models.CharField(max_length=1)
