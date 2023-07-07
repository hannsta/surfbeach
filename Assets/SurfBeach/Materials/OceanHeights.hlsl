#ifndef OCEANHEIGHT_INCLUDED
#define OCEANHEIGHT_INCLUDED


void OceanHeight_float(
        float3 position, 
        float time, 
        float speed, 
        float amplitude, 
        float frequency, 
        float maxDepth, 
        float3 waveDirection,
        UnityTexture2D resistances, 
        UnityTexture2D corners, 
        UnityTexture2D depths, 
        UnityTexture2D slopes,
        out float3 Out,
        out float ur)
{
    float scale = 400;
    waveDirection = normalize(waveDirection);
    float2 textureCoord = float2(position.x/scale, position.z/scale);

    float4 resistanceColor = tex2Dlod(resistances, float4(textureCoord, 0, 0));
    
    float coordResistance = 1-  resistanceColor.r;

    



    float4 depthColor = tex2Dlod(depths, float4(textureCoord, 0, 0));

    float depthResistance = 1;
    if (depthColor.g > 0){
        depthResistance = (.8f + .2f * depthColor.g);
    }else{
        depthResistance = .05f;
    }    

    float relativeX = (position.x * frequency  * waveDirection.x + (time * speed)) + (position.z * frequency * waveDirection.z + (time * speed));
    float totalHeight = 1 + coordResistance * depthResistance * (sin(relativeX) + .3 *abs(sin(relativeX))) * amplitude;
    float waveHeight = coordResistance * depthResistance * amplitude;

    float4 cornerColor = tex2Dlod(corners, float4(textureCoord, 0, 0));
    if (cornerColor.a == 0){
        Out = position;
        Out.y = totalHeight;
    }else {
        float3 cornerDirection = cornerColor.rgb - float3(position.x/scale, 0, position.z/scale);
        float cornerDistance = distance(float2(position.x/scale, position.z/scale), cornerColor.rb);

        float directionDot = dot(normalize(cornerDirection), normalize(waveDirection));
        

        float angle = degrees(acos(directionDot));
        if (angle > 0 && angle < 70){
            float waveletX = (cornerDistance * scale * frequency - (time  * speed * 2));                                        
            // totalHeight = cornerDistance * 50;
            totalHeight +=  depthResistance * (1 - angle / 70) * (sin(waveletX) + .3 * abs(sin(waveletX))) * amplitude;                                   
            waveHeight += depthResistance * (1 - angle / 70) * amplitude;
        }

        Out = position;
        Out.y = totalHeight;
        if (angle < 3){
            float boundaryBlend = smoothstep(0.0, 1.0, .7);
            Out.y = lerp(position.y, totalHeight, boundaryBlend);
        }
    }
    
    float4 landSlope = tex2Dlod(slopes, float4(textureCoord, 0, 0));
    float slope = radians(landSlope.g * 360);
    float waveLength = speed / frequency;
    float depth = depthColor.g * maxDepth;

    float steepness = waveHeight / depth;//(waveHeight * 2 / (waveLength * sqrt(9.8f * waveLength))) * (speed / (sqrt(depth) * sin(slope)));
    if (position.x-126 > -1 && position.x-126 < 1){
        if (position.z-72 > -1 && position.z-72 < 1){
            steepness = 0;
        }
    }
    bool isSurfable = false;
    bool isBreaking = false;
    bool isBroken = false;
    if (steepness > .14){
        steepness= .3;
        isBroken = true;
    }else if (steepness > .1){
        steepness= .5;
        isBreaking = true;
    }else if (steepness > .07){
        steepness= 1;
        isSurfable = true;
    }else if (steepness > .05){
        steepness= .3;
    }else{
        steepness = 0;
    }
    if (waveHeight < .3f){
        steepness = steepness * waveHeight;
    }
    ur = steepness;
}

#endif //OCEANHEIGHT_INCLUDED