﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace fx.Definitions
{
    // We use delegates to get the default values, and they are really "global".
    // That is, instead of being per generic type, they can be configured for
    // all the data-types at once.
    public static class RefList
    {
        public delegate int GetBlockSizeDelegate( Type itemType );

        private static GetBlockSizeDelegate _getDefaultFirstBlockSize = ( itemType ) => 32;
        public static GetBlockSizeDelegate GetDefaultFirstBlockSize
        {
            get
            {
                return _getDefaultFirstBlockSize;
            }
            set
            {
                if ( value == null )
                    _getDefaultFirstBlockSize = ( itemType ) => 32;
                else
                    _getDefaultFirstBlockSize = value;
            }
        }

        private static GetBlockSizeDelegate _getDefaultMaximumBlockSize = ( itemType ) => 1000000;
        public static GetBlockSizeDelegate GetDefaultMaximumBlockSize
        {
            get
            {
                return _getDefaultMaximumBlockSize;
            }
            set
            {
                if ( value == null )
                    _getDefaultMaximumBlockSize = ( itemType ) => 1000000;
                else
                    _getDefaultMaximumBlockSize = value;
            }
        }
    }

    // These classes do not implement any interface on purpose.
    // The LINQ methods only use the Count property or the indexer/ElementAt
    // when the type implement the modifiable interfaces, not the read-only ones.
    // Also the normal collections have a 32-bit size limit while these collections
    // are 64-bit. So, the easiest thing to do was to avoid using any interfaces
    // as, if needed, you can always create your own adapters.
    // Even the IEnumerable was avoided as some of the LINQ methods will actually
    // create a copy of the items (with a 32-bit limit) and the foreach works 
    // in classes that have a valid GetEnumerator even if it doesn't implement
    // the IEnumerable interface.
    public sealed class RefList<T> : IEnumerable, IEnumerable<T>, IList<T> 
    {
        // OK. We may not want to put tests in our delegates for specific types, yet
        // we may want to configure specific types with different block sizes.
        // So, we can do it by using the delegates in here. These affect only
        // the actual T, not the others.
        private static Func<int> _getDefaultFirstBlockSize = ( ) => RefList.GetDefaultFirstBlockSize( typeof( T ) );
        public static Func<int> GetDefaultFirstBlockSize
        {
            get
            {
                return _getDefaultFirstBlockSize;
            }
            set
            {
                if ( value == null )
                    _getDefaultFirstBlockSize = () => RefList.GetDefaultFirstBlockSize( typeof( T ) );
                else
                    _getDefaultFirstBlockSize = value;
            }
        }

        public static T NotFound = default( T );

        private static Func<int> _getDefaultMaximumBlockSize = ( ) => RefList.GetDefaultMaximumBlockSize( typeof( T ) );
        public static Func<int> GetDefaultMaximumBlockSize
        {
            get
            {
                return _getDefaultMaximumBlockSize;
            }
            set
            {
                if ( value == null )
                    _getDefaultMaximumBlockSize = () => RefList.GetDefaultMaximumBlockSize( typeof( T ) );
                else
                    _getDefaultMaximumBlockSize = value;
            }
        }

        internal sealed class _Node
        {
            internal _Node( long size )
            {
                _array = new T[ size ];
            }

            internal readonly T[ ] _array;
            internal _Node _nextNode;
        }

        private T[ ] _actualArray;
        private _Node _firstNode;
        private _Node _actualNode;
        private long _size;
        private readonly Func<int> _getMaximumBlockSize;
        private int _countInActualArray;

        public RefList() : this( FinancialHelper.CalculateStorageSize( TimeSpan.FromMinutes( 5 ) ) )
        {
        }

        private TimeSpan _period;
        public RefList( TimeSpan period )
        {
            _period = period;

            CreateStorage( FinancialHelper.CalculateStorageSize( _period ) );
        }

        public void CreateStorage( long capacity )
        {
            _firstNode = new _Node( capacity );
            _actualNode = _firstNode;
            _actualArray = _firstNode._array;


        }

        public RefList( long capacity, Func<int> getMaximumBlockSize = null )
        {
            Debug.Assert( capacity > 0 );

            CreateStorage( capacity );

            if ( getMaximumBlockSize == null )
            {
                // we actually run the getDefaultMaximumBlock size instead of
                // simply storing it so, if it changes, we will use the changed
                // version.
                getMaximumBlockSize = () => _getDefaultMaximumBlockSize();
            }

            _getMaximumBlockSize = getMaximumBlockSize;
        }

        public long Count
        {
            get
            {
                return _size;
            }
        }        

        public bool IsReadOnly => false;

        int ICollection<T>.Count => (int) Count;

        public T this[ int index ] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Clear()
        {
            _firstNode = new _Node( _firstNode._array.Length );
            _actualArray = _firstNode._array;
            _actualNode = _firstNode;
            _size = 0;
            _countInActualArray = 0;
        }
        public void Add( T item )
        {
            if ( _countInActualArray == _actualArray.Length )
            {
                long count = _size;
                int maximumSize = _getMaximumBlockSize( );

                if ( maximumSize < 1 )
                    throw new InvalidOperationException( "The GetMaximumBlockSize delegate returned an invalid value." );

                if ( count > maximumSize )
                    count = maximumSize;

                var newNode = new _Node( count );
                _actualNode._nextNode = newNode;
                _actualNode = newNode;
                _actualArray = newNode._array;
                _countInActualArray = 0;
            }

            _actualArray[ _countInActualArray ] = item;
            _size++;
            _countInActualArray++;
        }

        public int IndexOf( T item )
        {
            int index = -1;

            checked
            {
                foreach ( var otherItem in this )
                {
                    index++;
                    if ( item.Equals( otherItem ) )
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        public bool Contains( T item )
        {
            return IndexOf( item ) != -1;
        }

        public int FindIndex( Predicate<T> match )
        {
            return FindIndex( 0, ( int ) Count, match );
        }

        public int FindIndex( int startIndex, Predicate<T> match )
        {
            return FindIndex( startIndex, (int)Count - startIndex, match );
        }

        public int FindIndex( int startIndex, int count, Predicate<T> match )
        {
            if ( ( uint ) startIndex > ( uint ) _size )
            {
                throw new InvalidOperationException( "StartIndex out of Range" );
                //ThrowHelper.ThrowArgumentOutOfRangeException( ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index );
            }

            if ( count < 0 || startIndex > _size - count )
            {
                throw new InvalidOperationException( "Count out of Range" );                
            }

            if ( match == null )
            {
                throw new InvalidOperationException( "match is null" );                
            }
            
            int endIndex = startIndex + count;
            for ( int i = startIndex; i < endIndex; i++ )
            {
                if ( match( ElementAt( i ) ) ) return i;
            }
            return -1;
        }


        public void Update( long index, T item )
        {
            Debug.Assert( index >= 0 && index < _size );

            var node = _firstNode;
            while ( true )
            {
                var array = node._array;
                if ( index < array.Length )
                {
                    array[ index ] = item;
                    return;
                }


                index -= array.Length;
                node = node._nextNode;
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator( );
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
        

        public RefListForEach<T>.Enumerator GetEnumerator()
        {
            return new RefListForEach<T>.Enumerator( _firstNode, _actualArray, _countInActualArray );
        }
        public RefListForEach<T> AsImmutable()
        {
            return new RefListForEach<T>( _firstNode, _actualArray, _countInActualArray, _size );
        }
        public T[ ] ToArray()
        {
            return AsImmutable().ToArray();
        }
        public void CopyTo( T[ ] array, long arrayIndex )
        {
            AsImmutable().CopyTo( array, arrayIndex );
        }

        // This method is here for completeness, but it is slower for the
        // latest items as many blocks may need to be navigated.
        // Yet this ElementAt is much faster than the LINQ one, as here
        // we only need to navigate blocks, not element by element.
        public ref T ElementAt( long index )
        {
            if ( index >= 0 && index < _size )
            {
                var node = _firstNode;
                while ( true )
                {
                    var array = node._array;
                    if ( index < array.Length )
                        return ref array[ index ];

                    index -= array.Length;
                    node = node._nextNode;
                }
            }

            return ref NotFound;
        }

        public void Insert( int index, T item )
        {
            throw new NotImplementedException();
        }

        public void RemoveAt( int index )
        {
            throw new NotImplementedException();
        }

        public void CopyTo( T[ ] array, int arrayIndex )
        {
            throw new NotImplementedException();
        }

        public bool Remove( T item )
        {
            throw new NotImplementedException();
        }

        
    }

    public sealed class RefListForEach<T> 
    {
        private readonly RefList<T>._Node _firstNode;
        private readonly T[ ] _lastArray;
        private readonly int _countInLastArray;
        private readonly long _count;

        internal RefListForEach( RefList<T>._Node firstNode, T[ ] lastArray, int countInLastArray, long count )
        {
            _firstNode = firstNode;
            _lastArray = lastArray;
            _countInLastArray = countInLastArray;
            _count = count;
        }

        public long Count
        {
            get
            {
                return _count;
            }
        }

        // This method is here for completeness, but it is slower for the
        // latest items as many blocks may need to be navigated.
        // Yet this ElementAt is much faster than the LINQ one, as here
        // we only need to navigate blocks, not element by element.
        public ref T ElementAt( long index )
        {
            Debug.Assert( index >= 0 && index < _count );

            var node = _firstNode;
            while ( true )
            {
                var array = node._array;
                if ( index < array.Length )
                    return ref array[ index ];

                index -= array.Length;
                node = node._nextNode;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator( _firstNode, _lastArray, _countInLastArray );
        }

        public sealed class Enumerator :
            IEnumerator<T>
        {
            private T[ ] _array;
            private RefList<T>._Node _node;
            private long _positionInArray;
            private long _countInArray;
            private T[ ] _lastArray;
            private int _countInLastArray;


            internal Enumerator( RefList<T>._Node firstNode, T[ ] lastArray, int countInLastArray )
            {
                _node = firstNode;
                _array = _node._array;
                _positionInArray = -1;

                _lastArray = lastArray;
                if ( _array == lastArray )
                    _countInArray = countInLastArray;
                else
                    _countInArray = _array.Length;

                _countInLastArray = countInLastArray;
            }

            public T Current
            {
                get
                {
                    return _array[ _positionInArray ];
                }
            }

            public void Dispose()
            {
                _array = null;
                _node = null;
                _lastArray = null;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                if ( _array == null )
                    return false;

                _positionInArray++;
                if ( _positionInArray >= _countInArray )
                {
                    _node = _node._nextNode;

                    if ( _node == null )
                    {
                        _array = null;
                        return false;
                    }

                    _array = _node._array;
                    _positionInArray = 0;

                    if ( _array == _lastArray )
                        _countInArray = _countInLastArray;
                    else
                        _countInArray = _array.Length;
                }

                return true;
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }

        public T[ ] ToArray()
        {
            var result = new T[ _count ];

            CopyTo( result, 0 );

            return result;
        }
        public void CopyTo( T[ ] array, long arrayIndex )
        {
            Debug.Assert( array != null );

            Debug.Assert( arrayIndex >= 0 && arrayIndex <= ( array.Length - _count ) );

            var node = _firstNode;
            while ( true )
            {
                var nodeArray = node._array;

                if ( nodeArray == _lastArray )
                {
                    Array.Copy( nodeArray, 0, array, arrayIndex, _countInLastArray );
                    return;
                }

                nodeArray.CopyTo( array, arrayIndex );
                arrayIndex += nodeArray.Length;

                node = node._nextNode;
            }
        }
    }
}



