import SimpleOpenNI.*;
SimpleOpenNI kinect;

int closestValue;
int closestX;
int closestY;

int previousX;
int previousY;

void setup()
{
size(640,480);
kinect = new SimpleOpenNI(this);
kinect.enableDepth();
  colorMode(HSB, 360, 100, 100);
  background(0);
}

void draw()
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
void drawpaint(int x,int y){
    pushMatrix();
      translate(x, y);
      float hue = random(360);
      fill(hue, 100, 100);
      noStroke();
      int dropNum = int(map(random(1), 0, 1, 700, 1000));
      for(int i = 0; i < dropNum; i++){
        float diameter = pow(random(1), 20);
        float distance = sq((1.0 - pow(diameter, 2)) * random(1));
        float scaledDiameter = map(diameter, 0, 1, 1, 30);
        float scaledDistance = map(distance, 0, 1, 0, 300);
        float radian = random(TWO_PI);
        ellipse(scaledDistance * cos(radian), scaledDistance * sin(radian), scaledDiameter, scaledDiameter);
      }
    popMatrix();
}
