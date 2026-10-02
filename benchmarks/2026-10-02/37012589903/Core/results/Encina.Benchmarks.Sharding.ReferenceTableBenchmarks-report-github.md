```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|--------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    78,998.564 ns |    344.0999 ns |   321.8713 ns |  1.000 |    0.01 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   839,478.737 ns |  1,241.9503 ns | 1,100.9566 ns | 10.627 |    0.04 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,500,747.353 ns |  8,335.8669 ns | 7,797.3751 ns | 56.973 |    0.24 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         9.357 ns |      0.1098 ns |     0.1027 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         6.936 ns |      0.0758 ns |     0.0709 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         7.897 ns |      0.0164 ns |     0.0145 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       130.183 ns |      0.1697 ns |     0.1504 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        14.084 ns |      0.0233 ns |     0.0218 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    80,186.888 ns |  4,406.7074 ns |   241.5465 ns |  1.000 |    0.00 |    5 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   813,028.706 ns | 60,783.9089 ns | 3,331.7712 ns | 10.139 |    0.04 |    6 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,399,348.773 ns | 27,181.8058 ns | 1,489.9265 ns | 54.864 |    0.14 |    7 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.959 ns |      0.6554 ns |     0.0359 ns |  0.000 |    0.00 |    2 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         6.281 ns |      0.1162 ns |     0.0064 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         7.852 ns |      0.9846 ns |     0.0540 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       146.458 ns |     47.9341 ns |     2.6274 ns |  0.002 |    0.00 |    4 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        14.480 ns |      0.4157 ns |     0.0228 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
