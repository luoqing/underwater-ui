import * as T from "three";

// Keep a fish's undisturbed route and its short-lived escape displacement separate.
export function vesselClearance(){
  const center=new T.Vector3(),nose=new T.Vector3(),sweep=new T.Vector3(),delta=new T.Vector3(),target=new T.Vector3(),before=new T.Vector3();
  let enabled=false,halfHull=1.02,hullRadius=.95,speed=0,pass=0,contacts=0;
  const offsets=new WeakMap();
  function axisDistance(p,half,extra){
    delta.subVectors(p,center);
    delta.addScaledVector(nose,-T.MathUtils.clamp(delta.dot(nose),-half,half));
    if(extra && sweep.lengthSq()>.001)
      delta.addScaledVector(sweep,-T.MathUtils.clamp(delta.dot(sweep)/sweep.lengthSq(),0,1));
    return delta.length();
  }
  return {
    update(active,boat,dt){
      enabled=!!active;pass=0;contacts=0;
      halfHull=active==="diver"?.22:1.02;
      hullRadius=active==="diver"?.32:.95;
      speed=boat.velocity.length();
      center.copy(boat.position);boat.heading(nose);
      sweep.copy(boat.velocity).multiplyScalar(.95+dt);
    },
    // Anticipate a moving hull. An animal behind a departing boat should settle.
    threat(p,r,seed=0){
      if(!enabled || speed<.20 || p.distanceToSquared(center)>(11+r)**2)return 0;
      delta.subVectors(p,center);
      if(delta.dot(sweep)<-speed*(r+halfHull+.45))return 0;
      const distance=axisDistance(p,halfHull,true);
      const reach=r+hullRadius+(1.65+.35*Math.sin(seed))*(.55+hullRadius)+.28*speed;
      return (1-T.MathUtils.smoothstep(distance,r+hullRadius,reach))*T.MathUtils.smoothstep(speed,.2,2);
    },
    get count(){return pass},
    get corrections(){return contacts},
    avoid(key,p,r,dt,seed=0){
      let offset=offsets.get(key);
      if(!enabled){if(offset)offset.set(0,0,0);return 0}
      if(center.distanceToSquared(p)>(10+r)**2 && (!offset || offset.lengthSq()<.0001))return 0;
      if(!offset){offset=new T.Vector3();offsets.set(key,offset)}
      const distance=axisDistance(p,halfHull,true);
      const reach=2*hullRadius+r+.30*sweep.length();
      target.set(0,0,0);
      if(distance<reach){
        // Commit to the same side when a fish lies exactly on the course axis.
        const lateral=delta.x*nose.z-delta.z*nose.x;
        const side=Math.abs(lateral)>.11?Math.sign(lateral):Math.sin(seed+1)>=0?1:-1;
        target.set(nose.z*side,.13+.13*Math.sin(seed*2),-nose.x*side);
        target.addScaledVector(delta,.42).normalize().multiplyScalar(reach-distance+.32);
        pass++;
      }
      before.copy(offset);offset.lerp(target,1-Math.exp(-dt*(target.lengthSq()>.001?5.3:1.3)));
      p.add(offset);
      const gap=axisDistance(p,halfHull+.01,false),safe=hullRadius+r;
      if(gap<safe){
        if(gap<.001)delta.set(nose.z,0,-nose.x);else delta.divideScalar(gap);
        p.addScaledVector(delta,safe-gap);offset.addScaledVector(delta,safe-gap);contacts++;
      }
      before.subVectors(offset,before);
      return before.lengthSq()>.000001?Math.atan2(-before.z,before.x):NaN;
    }
  };
}
