```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    69,581.037 ns |     346.5281 ns |    307.1881 ns |  1.000 |    0.01 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   758,507.175 ns |   2,783.1667 ns |  2,467.2048 ns | 10.901 |    0.06 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,131,858.154 ns |  35,452.1510 ns | 33,161.9641 ns | 59.383 |    0.53 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.199 ns |       0.0416 ns |      0.0348 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.833 ns |       0.0325 ns |      0.0288 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.806 ns |       0.0610 ns |      0.0571 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       139.821 ns |       0.6013 ns |      0.5625 ns |  0.002 |    0.00 |    5 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        17.473 ns |       0.0947 ns |      0.0886 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    70,133.886 ns |   2,118.5563 ns |    116.1252 ns |  1.000 |    0.00 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   763,893.996 ns |  22,422.4474 ns |  1,229.0500 ns | 10.892 |    0.02 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,126,406.576 ns | 181,114.6864 ns |  9,927.5071 ns | 58.836 |    0.15 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        11.018 ns |       0.0818 ns |      0.0045 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         6.984 ns |       0.6900 ns |      0.0378 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         8.801 ns |       0.2147 ns |      0.0118 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       144.306 ns |       3.4429 ns |      0.1887 ns |  0.002 |    0.00 |    5 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        17.761 ns |       2.0497 ns |      0.1123 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
