mergeInto( LibraryManager.library,
{
  InitBufferCS: function( offset, length ) {
    uc3dBuffer = new Float32Array( buffer, offset, length );
  },

  StartCS: function() {
    startCS();
  },

  OnDestroyCS: function( id ) {
    onDestroyCS( id );
  },

  OnCollisionEnterCS: function( id1, id2 ) {
    onCollisionEnterCS( id1, id2 );
  },
}
);
