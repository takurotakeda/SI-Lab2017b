import SimpleOpenNI.*;
SimpleOpenNI kinect;

int closestValue;
int closestX;
int closestY;

int previousX;
int previousY;
int p = 0;
void setup()
{
size(1280,480);

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
  if (currentDepthValue>640&&currentDepthValue<2000&&currentDepthValue<closestValue){
   closestValue = currentDepthValue;
  closestX = x;
  closestY = y; 

  }
}
}
/////////////////////////////////////////////////
image(kinect.depthImage(),640,0);


fill(2,100,100);
ellipse(closestX+640,closestY,20,20);
fill(255);
rect(640,0,700,40);
fill(0);
textSize(40);
text("point"+p, 688, 35);

//noFill();
//rect(640,0,1280,640);
if(closestValue>640&&closestValue<650){
 p = p+1;
drawpaint(closestX,closestY);
}
if( closestX>600&&closestY<40){
  fill(0);
   p = 0;
rect(0,0,640,480);
}
if (p>=100){///////////////////////////////////////////////////////////////////////////////////////////////////////////
  fill(0);
rect(0,0,640,480);
//background(0);
fill(255);
textSize(100);
text("game clear", 30, 290);
}

/////////////////////////////////////////////////
}
void drawpaint(int x,int y){
  
    pushMatrix();
      translate(x, y);
      float hue = random(360);
      fill(hue, 100, 100);
      noStroke();
      int dropNum = int(map(random(1), 0, 1, 700, 1000));
      for(int i = 0; i < dropNum; i++){
        float diameter = pow(random(1), 50);
        float distance = sq((1.0 - pow(diameter, 2)) * random(1));
        float scaledDiameter = map(diameter, 0, 1, 1, 30);
        float scaledDistance = map(distance, 0, 1, 0, 300);
        float radian = random(TWO_PI);
        ellipse(scaledDistance * cos(radian), scaledDistance * sin(radian), scaledDiameter, scaledDiameter);

    }
    popMatrix();
}
void keyPressed() {
  if ( key == 'p' ) {
    save( "hoge.png" );
  }
}
