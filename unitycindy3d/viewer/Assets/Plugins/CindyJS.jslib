mergeInto( LibraryManager.library,
{
  InitBufferCS: function( offset, length ) {
    uc3dBuffer = new Float32Array( buffer, offset, length );
  },

  StartCS: function() {
    startCS();
  },

  CollisionEnterCS: function( objid1, objclass1, objid2, objclass2 ) {
    console.log( "OnCollisionEnterCindyJS " +objclass1 + ":" +objid1 + " " +objclass2 + ":" + objid2 );
    collisionEnterCS( objid1, objclass1, objid2, objclass2 );
  },
}
);
