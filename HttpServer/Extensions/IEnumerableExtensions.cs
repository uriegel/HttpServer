using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Extensions
{
    /// <summary>
    /// Erweiterungsklasse für <see cref="System.Collections.Generic.IEnumerable{T}"/>
    /// </summary>
    static class IEnumerableExtensions
    {
        /// <summary>
        /// Hiermit kann eine Enumerable eindeutig gemacht werden. Das Kriterium ist das Ergebnis eines Lambdaausdrucks, der
        /// als Parameter übergeben wird
        /// </summary>
        /// <typeparam name="TSource">Typ, aus dem das Enumerable gebildet wird</typeparam>
        /// <typeparam name="TKey">Typ des Kriteriums</typeparam>
        /// <param name="source">Enumerable, die eindeutig gemacht werden soll, bitte als Erweiterungmethode aufrufen (source.DistinctBy)!</param>
        /// <param name="keySelector">Lamdaausdruck, der das Kriteríum erzeugt</param>
        /// <returns>Die eindeutig gemachte Enumerable</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Ausführen einer Aktion auf alle Elemente einer Enumerable. Die Aktion kann ge- oder misslingen. Wenn eine Aktion erfolgreich war, gibt Perform true zurück
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="sourceList"></param>
        /// <param name="perform"></param>
        /// <param name="trueOnAll"></param>
        /// <returns></returns>
        public static bool Perform<TSource>(this IEnumerable<TSource> sourceList, Func<TSource, bool> perform, bool trueOnAll = false)
        {
            var result = trueOnAll;
            foreach (var source in sourceList)
            {
                var ok = perform(source);
                if (trueOnAll && !ok)
                    result = false;
                else if (!trueOnAll && ok)
                    result = true;
            }
            return result;
        }

        public static IEnumerable<T> ConcatMany<T>(this IEnumerable<IEnumerable<T>> ts)
            => ts.Where(n => n != null).Aggregate(Enumerable.Empty<T>(), (p, c) => p.Concat(c));

        /// <summary>
        /// Spezielles Mapping eines Enumerables in ein Enumerable eines anderen Typs
        /// </summary>
        /// <typeparam name="T">Typ des Eingangs-Enumerable</typeparam>
        /// <typeparam name="TResult">Typ des resultierenden Enumerables</typeparam>
        /// <param name="seq">Das Eingangs-Enumerable</param>
        /// <param name="ctor">Konstruktor, der aus einem Eingangstyp einen Ausganstyp erzeugt</param>
        /// <returns></returns>
        public static IEnumerable<TResult> Map<T, TResult>(this IEnumerable<T> seq, Func<T, TResult> ctor)
            => seq != null ? seq.Select(n => ctor(n)) : Enumerable.Empty<TResult>();

        /// <summary>
        /// Asynchrones Select, allerdings ohne ExpressionTree, d.h. das Ergebnis wird direkt asynchron ermittelt
        /// </summary>
        /// <typeparam name="T">Der Typ der Elemente, die umgewandelt werden sollen</typeparam>
        /// <typeparam name="TResult">Der Typ der Elemente, die im Ergebnis-Array zurückkommen</typeparam>
        /// <param name="elements">Elemente, die umgewandelt werden sollen</param>
        /// <param name="selectFunction">Umwandlung eines Elements vom Type T nach Typ TResult</param>
        /// <returns>Ein Array vom Typ TResult</returns>
        public static async Task<TResult[]> SelectAsync<T, TResult>(this IEnumerable<T> elements, Func<T, Task<TResult>> selectFunction)
        {
            var elementList = new List<TResult>();
            foreach (var element in elements)
                elementList.Add(await selectFunction(element));
            return elementList.ToArray();
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, bool throwOnError, Func<TSource, bool> predicate)
            => throwOnError ? source.First(predicate) : source.FirstOrDefault(predicate);
    }
}
