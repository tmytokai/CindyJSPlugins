(function(){
'use strict';CindyJS.registerPlugin(1,"audio",function(d){var h=null,e=null,f=null,v=null,r=[null,null],l=null,y=0,t=[null,null],k=[null,null],z=[null,null],n=[null,null],p=[null,null],A=[null,null],m=[null,null],q=0,B=0,g=null,w=null,u=null,C=function(){null==e&&(e=new (window.AudioContext||window.webkitAudioContext))},G=function(){null==l&&(l=e.createScriptProcessor(2048,f.buffer.numberOfChannels,f.buffer.numberOfChannels),l.onaudioprocess=function(a){for(a=0;a<f.buffer.numberOfChannels;++a){r[a].getFloatFrequencyData(t[a]);
var b=a;if(null!=k[b])for(var c=0;256>c;++c){var d=t[b][1024*c/256];d=Math.min(-30,Math.max(-100,d));d=Math.floor((d- -100)/70*256);for(var e=255,h=255,l=0;256>l;++l){var x=4*(256*(255-l)+c);l==d&&(e=h=0);n[b].data[x+0]=e;n[b].data[x+1]=h;n[b].data[x+2]=0;n[b].data[x+3]=255}}if(null!=p[b])for(c=0;256>c;++c)d=4*(256*(255-c)+q),e=t[b][1024*c/256],e=Math.min(-30,Math.max(-100,e)),e=(g.height-1-Math.floor((e- -100)/70*(g.height-1)))*g.width*4,m[b].data[d+0]=u.data[e+0],m[b].data[d+1]=u.data[e+1],m[b].data[d+
2]=u.data[e+2],m[b].data[d+3]=255}q=(q+1)%256})},D=function(a){null==f&&(f=e.createBufferSource(),f.buffer=a,f.loop=!0,B=f.buffer.sampleRate/2,console.log("rate = "+f.buffer.sampleRate),console.log("duration = "+f.buffer.duration),console.log("channels = "+f.buffer.numberOfChannels));null==v&&(v=e.createChannelSplitter(f.buffer.numberOfChannels));for(a=0;a<f.buffer.numberOfChannels;++a)null==r[a]&&(r[a]=e.createAnalyser(),r[a].smoothingTimeConstant=0,r[a].fftSize=2048,r[a].minDecibels=-100,r[a].maxDecibels=
-30);for(a=0;2>a;++a)null==t[a]&&(t[a]=new Float32Array(1024));G();if(null==g){g=document.createElement("canvas");g.id="audiocolormap";g.style.display="none";g.width=10;g.height=256;w=g.getContext("2d");for(a=0;a<g.height;++a)w.fillStyle="hsl("+240*a/g.height+", 100%, 50%)",w.fillRect(0,a,g.width,1);u=w.getImageData(0,0,g.width,g.height)}for(a=0;2>a;++a){var b=a;null==k[b]&&(k[b]=document.createElement("canvas"),k[b].id="audiocanvassp"+b,k[b].style.display="none",k[b].width=256,k[b].height=256,z[b]=
k[b].getContext("2d"),n[b]=z[b].getImageData(0,0,256,256));null==p[b]&&(p[b]=document.createElement("canvas"),p[b].id="audiocanvasspgr"+b,p[b].style.display="none",p[b].width=256,p[b].height=256,A[b]=p[b].getContext("2d"),m[b]=A[b].getImageData(0,0,256,256));b=a;if(null!=k[b])for(var c=0;262144>c;c+=4)n[b].data[c+0]=0,n[b].data[c+1]=0,n[b].data[c+2]=0,n[b].data[c+3]=255;if(null!=p[b]){c=(g.height-1)*g.width*4;for(var d=0;262144>d;d+=4)m[b].data[d+0]=u.data[c+0],m[b].data[d+1]=u.data[c+1],m[b].data[d+
2]=u.data[c+2],m[b].data[d+3]=255}}q=0;f.connect(v);for(a=0;a<f.buffer.numberOfChannels;++a)v.connect(r[a],a);f.connect(l);l.connect(e.destination);f.connect(e.destination);y=e.currentTime;f.start(0)},H=function(a){if(null==f){console.log("id "+a);a=document.getElementById(a).files[0];console.log("loading "+a.name);var b=new FileReader;b.onload=function(){C();e.decodeAudioData(b.result,function(a){D(a)},function(a){console.log("decodeAudioData failed")})};b.readAsArrayBuffer(a)}},I=function(a){if(null==
f){console.log("loading "+a);var b=new XMLHttpRequest;b.open("GET",a,!0);b.responseType="arraybuffer";b.onerror=function(){console.log("XMLHttpRequest failed")};b.onload=function(){C();e.decodeAudioData(this.response,function(a){D(a)},function(a){console.log("decodeAudioData failed")})};b.send()}},E=function(a,b){var c=d.getInitialMatrix();return{x:c.a*a-c.b*b+c.tx,y:c.c*a-c.d*b-c.ty}},F=function(a,b){var c=d.getInitialMatrix();a-=c.tx;b+=c.ty;return{x:(c.d*a-c.b*b)/c.det,y:-(-c.c*a+c.a*b)/c.det}};
d.defineFunction("playaudio",1,function(a,b){a=d.evaluate(a[0]);if("string"!==a.ctype)return d.nada;a.value.startsWith("file://")?H(a.value.substr(7)):I(a.value);return d.nada});d.defineFunction("stopaudio",0,function(a,b){if(null!=f)return f.stop(),e.close(),v=f=e=null,r=[null,null],l=null,d.nada});d.defineFunction("audiospectrum",2,function(a,b){b=-100;var c=d.evaluate(a[0]);"number"==c.ctype&&null!=t[c.value.real]&&(a=d.evaluate(a[1]),"number"==a.ctype&&(b=t[c.value.real][a.value.real]));return{ctype:"number",
value:{real:b,imag:0}}});d.defineFunction("audiodrawspectrum",2,function(a,b){b=d.evaluate(a[1]);if("number"!=b.ctype)return d.nada;b=b.value.real;a=d.extractPoint(d.evaluateAndVal(a[0]));a=E(a.x,a.y);if(null==h&&(h=d.instance.canvas.getContext("2d"),null==h))return d.nada;if(null==k[b])return h.fillStyle="black",h.fillRect(a.x,a.y,256,256),d.nada;h.putImageData(n[b],a.x,a.y);return d.nada});d.defineFunction("audiodrawspectrogram",2,function(a,b){b=d.evaluate(a[1]);if("number"!=b.ctype)return d.nada;
b=b.value.real;a=d.extractPoint(d.evaluateAndVal(a[0]));var c=E(a.x,a.y);if(null==h&&(h=d.instance.canvas.getContext("2d"),null==h))return d.nada;if(null==p[b])return h.fillStyle="hsl(240, 100%, 50%)",h.fillRect(c.x,c.y,256,256),d.nada;h.putImageData(m[b],c.x-q,c.y,q,0,256-q,256);0<q&&h.putImageData(m[b],c.x+256-q,c.y,0,0,q,256);b="";c=0;for(var e=(B-0)/2E3,f=F(0,0).y-F(0,256).y,g=a.x-.05,l=f/e,k=0;k<=Math.floor(e);++k)b+="drawtext(["+g+","+(a.y-f+l*k-.05)+"],"+c+',align->"right",size->10);',c+=2E3;
b+="drawtext(["+g+","+(a.y+.05)+'],"Hz",align->"right",size->10);';d.instance.evalcs(b);return d.nada});d.defineFunction("audioposition",0,function(a,b){a=0;null!=f&&(a=e.currentTime-y);return{ctype:"number",value:{real:a,imag:0}}})});
}).call(this);

