```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   677.9 ns |     16.19 ns |  18.64 ns |  0.81 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   760.1 ns |      8.40 ns |   9.34 ns |  0.91 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   835.6 ns |     19.13 ns |  21.26 ns |  1.00 |    0.03 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   624.5 ns |     14.10 ns |  15.68 ns |  0.75 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   828.7 ns |     13.46 ns |  14.40 ns |  0.99 |    0.03 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,394.2 ns |    188.43 ns | 201.62 ns |  5.26 |    0.27 | 0.1068 | 0.1030 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,552.8 ns |    473.29 ns | 486.03 ns |  9.04 |    0.61 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,235.2 ns |     43.41 ns |  48.25 ns |  2.68 |    0.09 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,200.5 ns |    387.51 ns | 414.64 ns |  8.62 |    0.53 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,144.3 ns |    115.99 ns | 124.11 ns |  3.77 |    0.17 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,241.8 ns |    328.53 ns | 365.16 ns |  3.88 |    0.44 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   693.0 ns |    495.56 ns |  27.16 ns |  0.84 |    0.04 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   723.5 ns |    319.85 ns |  17.53 ns |  0.87 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   828.1 ns |    533.80 ns |  29.26 ns |  1.00 |    0.04 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   619.4 ns |    141.95 ns |   7.78 ns |  0.75 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   845.9 ns |    599.00 ns |  32.83 ns |  1.02 |    0.05 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,242.6 ns |  1,467.33 ns |  80.43 ns |  5.13 |    0.18 | 0.1068 | 0.1030 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,720.5 ns |  9,069.75 ns | 497.14 ns |  9.33 |    0.59 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,316.7 ns |    460.58 ns |  25.25 ns |  2.80 |    0.09 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 7,401.9 ns | 11,288.39 ns | 618.75 ns |  8.95 |    0.70 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,082.1 ns |  2,065.71 ns | 113.23 ns |  3.73 |    0.16 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,886.3 ns |    612.09 ns |  33.55 ns |  3.49 |    0.11 | 0.0610 | 0.0572 |    1072 B |        2.16 |
