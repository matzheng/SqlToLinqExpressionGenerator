//Author: Sergey Lavrinenko
//Date:   28Nov2010

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequestEngine
{
    /// <summary>
    /// This is very simple, but not finished class representing segment of the List<>. Similar to ArraySegment<>, but more useful
    /// </summary>
    public class ListSegment<T>
    {
        List<T> list;
        int offset;
        int count;

        public ListSegment(List<T> list) : this(list, 0, list.Count) {}

        public ListSegment(List<T> list, int offset, int count)
        {
            this.list = list;
            this.offset = offset;
            this.count = count;
        }

        public T this[int index]
        {
            get
            {
                if (index >= count) throw new IndexOutOfRangeException();
                return list[offset + index];
            }
        }

        public int Count
        {
            get { return count; }
        }

        public ListSegment<T> GetSegment(int offset, int count)
        {
            if (offset>=this.count || offset+count>this.count)
                throw new IndexOutOfRangeException();
            return new ListSegment<T>(list, this.offset + offset, count);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(item, Comparer<T>.Default);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return list.BinarySearch(offset, count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index >= this.count || index + count > this.count)
                throw new IndexOutOfRangeException();

            return list.BinarySearch(offset + index, count, item, comparer);
        }

        public bool Contains(T item)
        {
            for (int i = offset; i < offset + count; ++i)
                if (list[i].Equals(item)) return true;
            return false;
        }

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }

        public int FindIndex(Predicate<T> match)
        {
            for (int i = offset; i < offset + count; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            if (startIndex >= count) throw new IndexOutOfRangeException();

            for (int i = offset + startIndex; i < offset + count; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if (startIndex >= this.count || startIndex + count > this.count)
                throw new IndexOutOfRangeException();

            for (int i = offset + startIndex; i < offset + startIndex + count; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public int FindLastIndex(Predicate<T> match)
        {
            for (int i = offset + count; --i >= offset; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            if (startIndex >= count) throw new IndexOutOfRangeException();

            for (int i = offset + count; --i >= offset + startIndex; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (startIndex >= this.count || startIndex + count > this.count)
                throw new IndexOutOfRangeException();

            for (int i = offset + count; --i >= offset + startIndex; ++i)
                if (match(list[i])) return i - offset;
            return -1;
        }

        public T Find(Predicate<T> match)
        {
            for (int i = offset; i < offset + count; ++i)
                if (match(list[i])) return list[i];
            return default(T);
        }

        public T FindLast(Predicate<T> match)
        {
            for (int i = offset + count; --i >= offset; )
                if (match(list[i])) return list[i];
            return default(T);
        }

        public void ForEach(Action<T> action)
        {
            for (int i = offset; i < count; ++i)
                action(list[i]);
        }

        public bool TrueForAll(Predicate<T> match)
        {
            for (int i = offset; i < count; ++i)
                if (!match(list[i])) return false;
            return true;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            List<T> result = new List<T>();
            for (int i = offset; i < offset + count; ++i)
                if (match(list[i])) result.Add(list[i]);
            return result;
        }
    }
}
