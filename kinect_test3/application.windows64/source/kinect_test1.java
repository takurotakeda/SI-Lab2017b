import processing.core.*; 
import processing.data.*; 
import processing.event.*; 
import processing.opengl.*; 

import SimpleOpenNI.*; 

import java.util.HashMap; 
import java.util.ArrayList; 
import java.io.File; 
import java.io.BufferedReader; 
import java.io.PrintWriter; 
import java.io.InputStream; 
import java.io.OutputStream; 
import java.io.IOException; 

public class kinect_test1 extends PApplet {


SimpleOpenNI kinect;

int closestValue;
int closestX;
int closestY;

int previousX;
int previousY;

public void setup()
{
size(640,480);
kinect = new SimpleOpenNI(this);
kinect.enableDepth();
  colorMode(HSB, 360, 100, 100);
  background(0);
}

public void draw()
{
closestValue = 8000;
kinect.update();
int[] depthValues = kinect.depthMap();
for(int y=0;y<480;y++){
for(int x=0;x<640;x++){
  int i=x+y*640;
  int currentDepthValue = depthValues[i];
  if (currentDepthValue>640&&currentDepthValue<1000&&currentDepthValue<closestValue){
   closestValue = currentDepthValue;
  closestX = x;
  closestY = y; 

  }
}
}
//image(kinect.depthImage(),0,0);
if(closestValue>640&&closestValue<700){
drawpaint(closestX,closestY);
}


}
public void drawpaint(int x,int y){
    pushMatrix();
      translate(x, y);
      float hue = random(360);
      fill(hue, 100, 100);
      noStroke();
      int dropNum = PApplet.parseInt(map(random(1), 0, 1, 700, 1000));
      for(int i = 0; i < dropNum; i++){
        float diameter = pow(random(1), 20);
        float distance = sq((1.0f - pow(diameter, 2)) * random(1));
        float scaledDiameter = map(diameter, 0, 1, 1, 30);
        float scaledDistance = map(distance, 0, 1, 0, 300);
        float radian = random(TWO_PI);
        ellipse(scaledDistance * cos(radian), scaledDistance * sin(radian), scaledDiameter, scaledDiameter);
      }
    popMatrix();
}
  static public void main(String[] passedArgs) {
    String[] appletArgs = new String[] { "--full-screen", "--bgcolor=#666666", "--hide-stop", "kinect_test1" };
    if (passedArgs != null) {
      PApplet.main(concat(appletArgs, passedArgs));
    } else {
      PApplet.main(appletArgs);
    }
  }
}
